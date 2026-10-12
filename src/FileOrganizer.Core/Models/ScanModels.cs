namespace FileOrganizer.Core.Models;

public sealed class ScanOptions
{
    public bool IncludeHidden { get; set; }
    public bool FollowReparsePoints { get; set; }
    /// <summary>Пропускать системные каталоги (Windows, Program Files, AppData...). Отключать не рекомендуется.</summary>
    public bool SkipSystemFolders { get; set; } = true;
    public ExclusionSet Exclusions { get; set; } = new();
    /// <summary>Как часто (в файлах) сообщать о прогрессе.</summary>
    public int ProgressEvery { get; set; } = 250;
}

public sealed record ScanProgress(int FilesScanned, int FoldersScanned, long BytesScanned, string CurrentFolder, int ErrorCount);

public enum ScanErrorKind { AccessDenied, PathTooLong, NotFound, IoError, Other }

public sealed record ScanError(string Path, ScanErrorKind Kind, string Message);

public sealed class ScanResult
{
    public required string RootPath { get; init; }
    public required FolderEntry Root { get; init; }
    public List<FileEntry> Files { get; } = new();
    public List<FolderEntry> Folders { get; } = new();
    public List<ScanError> Errors { get; } = new();
    public List<string> SkippedPaths { get; } = new();
    public DateTime StartedUtc { get; init; }
    public DateTime FinishedUtc { get; set; }
    public bool Cancelled { get; set; }

    public long TotalSize => Files.Sum(f => f.Size);

    public FolderEntry? FindFolder(string path)
    {
        _folderIndex ??= Folders.ToDictionary(f => f.FullPath, StringComparer.OrdinalIgnoreCase);
        return _folderIndex.TryGetValue(path, out var f) ? f : null;
    }

    private Dictionary<string, FolderEntry>? _folderIndex;
}
