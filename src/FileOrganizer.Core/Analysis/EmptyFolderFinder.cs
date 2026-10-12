using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Пустые папки: совсем пустые, содержащие только служебные файлы (Thumbs.db, desktop.ini, .DS_Store),
/// и цепочки вложенных пустых каталогов. Возвращаются только «верхние» пустые папки.
/// </summary>
public static class EmptyFolderFinder
{
    public static readonly HashSet<string> JunkFileNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "thumbs.db", "desktop.ini", ".ds_store", "ehthumbs.db", "._.ds_store", "icon\r",
    };

    /// <summary>
    /// Ищет пустые папки. Папка считается пустой, только если она реально пуста на диске
    /// (в том числе нет скрытых и исключённых из анализа файлов).
    /// </summary>
    public static List<EmptyFolderInfo> Find(ScanResult scan, bool includeRoot = false)
    {
        var emptyCache = new Dictionary<FolderEntry, (bool empty, int nested, bool onlyJunk, List<string> junk)>();
        var order = new List<FolderEntry>();
        var stack = new Stack<FolderEntry>();
        stack.Push(scan.Root);
        while (stack.Count > 0)
        {
            var f = stack.Pop();
            order.Add(f);
            foreach (var s in f.SubFolders) stack.Push(s);
        }
        var skipped = new HashSet<string>(scan.SkippedPaths, StringComparer.OrdinalIgnoreCase);

        for (int i = order.Count - 1; i >= 0; i--)
        {
            var f = order[i];
            var (diskEmpty, junk) = InspectDisk(f.FullPath, skipped);
            bool subsEmpty = f.SubFolders.All(s => emptyCache[s].empty);
            bool empty = diskEmpty && subsEmpty;
            int nested = f.SubFolders.Sum(s => emptyCache[s].nested + 1);
            bool onlyJunk = junk.Count > 0 || f.SubFolders.Any(s => emptyCache[s].onlyJunk);
            emptyCache[f] = (empty, nested, onlyJunk, junk);
        }

        var result = new List<EmptyFolderInfo>();
        foreach (var f in order)
        {
            var (empty, nested, onlyJunk, junk) = emptyCache[f];
            if (!empty) continue;
            if (f == scan.Root)
            {
                if (!includeRoot) continue;
            }
            else
            {
                // Только верхний уровень цепочки пустых каталогов.
                var parent = f.ParentPath != null ? scan.FindFolder(f.ParentPath) : null;
                bool parentReported = parent != null && emptyCache[parent].empty && (parent != scan.Root || includeRoot);
                if (parentReported) continue;
            }
            result.Add(new EmptyFolderInfo { Path = f.FullPath, NestedEmptyCount = nested, OnlySystemFiles = onlyJunk, JunkFiles = CollectJunk(f, emptyCache) });
        }
        return result.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static List<string> CollectJunk(FolderEntry f, Dictionary<FolderEntry, (bool empty, int nested, bool onlyJunk, List<string> junk)> cache)
    {
        var all = new List<string>(cache[f].junk);
        foreach (var s in f.SubFolders) all.AddRange(CollectJunk(s, cache));
        return all;
    }

    /// <summary>Проверяет содержимое папки на диске: только служебные файлы и подпапки, которые есть в скане.</summary>
    private static (bool empty, List<string> junk) InspectDisk(string path, HashSet<string> skipped)
    {
        var junk = new List<string>();
        try
        {
            foreach (var e in new DirectoryInfo(path).EnumerateFileSystemInfos("*", new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false }))
            {
                if (e is DirectoryInfo)
                {
                    // Подпапки, пропущенные сканером (скрытые, системные, исключённые), делают папку непустой.
                    if (skipped.Contains(e.FullName)) return (false, junk);
                    continue;
                }
                if (JunkFileNames.Contains(e.Name)) { junk.Add(e.FullName); continue; }
                return (false, junk);
            }
            return (true, junk);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return (false, junk);
        }
    }
}
