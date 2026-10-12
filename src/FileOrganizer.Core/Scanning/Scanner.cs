using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Scanning;

/// <summary>
/// Сканирует дерево каталогов без рекурсии (стек), не следуя по символическим ссылкам/junction.
/// Ошибки доступа к отдельным файлам и папкам собираются в список и не прерывают сканирование.
/// Работает в фоне; поддерживает отмену и отчёт о прогрессе.
/// </summary>
public sealed class Scanner
{
    private readonly FileClassifier _classifier;
    private readonly Logger _log;

    public Scanner(FileClassifier? classifier = null, Logger? log = null)
    {
        _classifier = classifier ?? new FileClassifier();
        _log = log ?? Logger.Null;
    }

    public Task<ScanResult> ScanAsync(string rootPath, ScanOptions options, IProgress<ScanProgress>? progress = null, CancellationToken ct = default)
        => Task.Run(() => Scan(rootPath, options, progress, ct), CancellationToken.None);

    public ScanResult Scan(string rootPath, ScanOptions options, IProgress<ScanProgress>? progress = null, CancellationToken ct = default)
    {
        var root = PathUtil.Normalize(rootPath);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Папка не найдена: {root}");

        var rootEntry = new FolderEntry { FullPath = root, Name = Path.GetFileName(root) is { Length: > 0 } n ? n : root, ParentPath = null, Depth = 0 };
        var result = new ScanResult { RootPath = root, Root = rootEntry, StartedUtc = DateTime.UtcNow };
        result.Folders.Add(rootEntry);

        var matcher = new ExclusionMatcher(options.Exclusions);
        var stack = new Stack<FolderEntry>();
        stack.Push(rootEntry);
        long bytes = 0;
        int sinceReport = 0;

        var enumOptions = new EnumerationOptions
        {
            RecurseSubdirectories = false,
            IgnoreInaccessible = false,
            AttributesToSkip = 0,
            ReturnSpecialDirectories = false,
        };

        while (stack.Count > 0)
        {
            if (ct.IsCancellationRequested) { result.Cancelled = true; break; }
            var folder = stack.Pop();
            IEnumerable<FileSystemInfo> entries;
            List<FileSystemInfo> list;
            try
            {
                entries = new DirectoryInfo(folder.FullPath).EnumerateFileSystemInfos("*", enumOptions);
                list = new List<FileSystemInfo>();
                using var e = entries.GetEnumerator();
                while (true)
                {
                    try
                    {
                        if (!e.MoveNext()) break;
                        list.Add(e.Current);
                    }
                    catch (Exception ex) when (ex is UnauthorizedAccessException or IOException)
                    {
                        AddError(result, folder.FullPath, ex);
                        break;
                    }
                }
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
            {
                AddError(result, folder.FullPath, ex);
                continue;
            }

            var subfolders = new List<FolderEntry>();
            foreach (var info in list)
            {
                try
                {
                    var attrs = info.Attributes;
                    bool isReparse = (attrs & FileAttributes.ReparsePoint) != 0;
                    bool isHidden = (attrs & FileAttributes.Hidden) != 0;
                    bool isSystem = (attrs & FileAttributes.System) != 0;

                    if (info is DirectoryInfo di)
                    {
                        if (isReparse && !options.FollowReparsePoints) { result.SkippedPaths.Add(di.FullName); continue; }
                        if (options.SkipSystemFolders && (isSystem || SystemPathGuard.IsProtectedFolder(di.FullName))) { result.SkippedPaths.Add(di.FullName); continue; }
                        if (isHidden && !options.IncludeHidden) { result.SkippedPaths.Add(di.FullName); continue; }
                        if (matcher.IsFolderExcluded(di.FullName, di.Name)) { result.SkippedPaths.Add(di.FullName); continue; }

                        var sub = new FolderEntry { FullPath = di.FullName, Name = di.Name, ParentPath = folder.FullPath, Depth = folder.Depth + 1 };
                        folder.SubFolders.Add(sub);
                        result.Folders.Add(sub);
                        subfolders.Add(sub);
                    }
                    else if (info is FileInfo fi)
                    {
                        if (isSystem && options.SkipSystemFolders) continue;
                        if (isHidden && !options.IncludeHidden) continue;
                        if (isReparse && !options.FollowReparsePoints) { result.SkippedPaths.Add(fi.FullName); continue; }
                        var ext = FileClassifier.GetExtension(fi.Name);
                        if (matcher.IsFileExcluded(fi.FullName, fi.Name, ext)) continue;

                        var entry = new FileEntry
                        {
                            FullPath = fi.FullName,
                            Name = fi.Name,
                            Extension = ext,
                            DirectoryPath = folder.FullPath,
                            Size = fi.Length,
                            LastWriteTimeUtc = fi.LastWriteTimeUtc,
                            CreationTimeUtc = fi.CreationTimeUtc,
                            LastAccessTimeUtc = fi.LastAccessTimeUtc,
                            Attributes = attrs,
                        };
                        entry.Category = _classifier.Classify(entry);
                        folder.Files.Add(entry);
                        result.Files.Add(entry);
                        bytes += entry.Size;

                        if (++sinceReport >= options.ProgressEvery)
                        {
                            sinceReport = 0;
                            progress?.Report(new ScanProgress(result.Files.Count, result.Folders.Count, bytes, folder.FullPath, result.Errors.Count));
                            if (ct.IsCancellationRequested) { result.Cancelled = true; break; }
                        }
                    }
                }
                catch (Exception ex) when (ex is UnauthorizedAccessException or IOException or System.Security.SecurityException)
                {
                    AddError(result, info.FullName, ex);
                }
            }
            if (result.Cancelled) break;
            // В обратном порядке, чтобы обход шёл по алфавиту.
            for (int i = subfolders.Count - 1; i >= 0; i--) stack.Push(subfolders[i]);
        }

        ComputeTotals(rootEntry);
        result.FinishedUtc = DateTime.UtcNow;
        progress?.Report(new ScanProgress(result.Files.Count, result.Folders.Count, bytes, "", result.Errors.Count));
        _log.Info("Scan", root, $"files={result.Files.Count}; folders={result.Folders.Count}; errors={result.Errors.Count}; cancelled={result.Cancelled}");
        return result;
    }

    private void AddError(ScanResult result, string path, Exception ex)
    {
        var kind = ex switch
        {
            UnauthorizedAccessException => ScanErrorKind.AccessDenied,
            System.Security.SecurityException => ScanErrorKind.AccessDenied,
            PathTooLongException => ScanErrorKind.PathTooLong,
            DirectoryNotFoundException or FileNotFoundException => ScanErrorKind.NotFound,
            IOException => ScanErrorKind.IoError,
            _ => ScanErrorKind.Other,
        };
        result.Errors.Add(new ScanError(path, kind, ex.Message));
        _log.Warn("Scan", path, ex.Message);
    }

    /// <summary>Подсчёт размеров снизу вверх без рекурсии.</summary>
    private static void ComputeTotals(FolderEntry root)
    {
        var order = new List<FolderEntry>();
        var stack = new Stack<FolderEntry>();
        stack.Push(root);
        while (stack.Count > 0)
        {
            var f = stack.Pop();
            order.Add(f);
            foreach (var s in f.SubFolders) stack.Push(s);
        }
        for (int i = order.Count - 1; i >= 0; i--)
        {
            var f = order[i];
            f.TotalSize = f.Files.Sum(x => x.Size) + f.SubFolders.Sum(s => s.TotalSize);
            f.TotalFileCount = f.Files.Count + f.SubFolders.Sum(s => s.TotalFileCount);
        }
    }
}
