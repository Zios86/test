namespace FileOrganizer.Core.Models;

public sealed class DuplicateGroup
{
    public required string Hash { get; init; }
    public long Size { get; init; }
    public required List<FileEntry> Files { get; init; }
    /// <summary>Рекомендуемый для сохранения экземпляр. Окончательное решение — за пользователем.</summary>
    public FileEntry? Recommended { get; set; }
    public string RecommendationReason { get; set; } = "";
    /// <summary>Сколько места занимают лишние копии.</summary>
    public long RedundantBytes => Size * (Files.Count - 1);
}

/// <summary>Похожие версии документа (Отчёт.docx, Отчёт (1).docx, Отчёт_final.docx). Не считаются дубликатами.</summary>
public sealed class SimilarVersionGroup
{
    public required string BaseName { get; init; }
    public required List<FileEntry> Files { get; init; }
}

public sealed class EmptyFolderInfo
{
    public required string Path { get; init; }
    /// <summary>Количество вложенных пустых каталогов (длина цепочки).</summary>
    public int NestedEmptyCount { get; init; }
    /// <summary>Содержит только служебные файлы (Thumbs.db, desktop.ini, .DS_Store).</summary>
    public bool OnlySystemFiles { get; init; }
    public List<string> JunkFiles { get; init; } = new();
}

public sealed class BadNameInfo
{
    public required FileEntry File { get; init; }
    public required string Problem { get; init; }
    public required string SuggestedName { get; init; }
}

public sealed class SimilarFolderGroup
{
    public required string NormalizedName { get; init; }
    public required List<FolderEntry> Folders { get; init; }
    /// <summary>Предлагаемая папка-приёмник (где больше всего файлов).</summary>
    public required FolderEntry Target { get; init; }
    /// <summary>Имена файлов, которые встречаются в нескольких папках группы — конфликт при объединении.</summary>
    public List<string> ConflictingNames { get; init; } = new();
}

public sealed class MisplacedFileInfo
{
    public required FileEntry File { get; init; }
    public required string Reason { get; init; }
    public string? SuggestedFolder { get; init; }
    public double Confidence { get; init; }
}

public enum TempFileKind { TempFile, PartialDownload, OldInstaller, LeftoverArchive, RepeatedDownload, OfficeLockFile, Backup }

public sealed class TempFileInfo
{
    public required FileEntry File { get; init; }
    public TempFileKind Kind { get; init; }
    public required string Reason { get; init; }
}

public sealed record SizeBucket(string Label, long MinBytes, int Count, long TotalBytes);

public sealed record CategoryUsage(FileCategory Category, int Count, long TotalBytes);

public sealed class ProjectGroup
{
    public required string Name { get; init; }
    public required List<FileEntry> Files { get; init; }
    public string Reason { get; init; } = "";
}

/// <summary>Сводный результат анализа. Показывается на дашборде.</summary>
public sealed class AnalysisReport
{
    public required ScanResult Scan { get; init; }
    public int FileCount => Scan.Files.Count;
    public int FolderCount => Scan.Folders.Count;
    public long TotalSize => Scan.TotalSize;
    public int FileTypeCount { get; set; }

    public List<DuplicateGroup> Duplicates { get; set; } = new();
    public List<SimilarVersionGroup> SimilarVersions { get; set; } = new();
    public List<EmptyFolderInfo> EmptyFolders { get; set; } = new();
    public List<FileEntry> LargeFiles { get; set; } = new();
    public List<SizeBucket> SizeBuckets { get; set; } = new();
    public List<CategoryUsage> CategoryUsage { get; set; } = new();
    public List<FileEntry> OldFiles { get; set; } = new();
    public List<BadNameInfo> BadNames { get; set; } = new();
    public List<SimilarFolderGroup> SimilarFolders { get; set; } = new();
    public List<MisplacedFileInfo> Misplaced { get; set; } = new();
    public List<TempFileInfo> TempFiles { get; set; } = new();
    public List<ProjectGroup> Projects { get; set; } = new();

    public int DuplicateFileCount => Duplicates.Sum(g => g.Files.Count - 1);
    public long DuplicateBytes => Duplicates.Sum(g => g.RedundantBytes);
    public long OldFilesBytes => OldFiles.Sum(f => f.Size);
    public long TempFilesBytes => TempFiles.Sum(t => t.File.Size);
    /// <summary>
    /// Потенциально избыточные данные: точные копии + временные файлы (без двойного учёта).
    /// Это не «можно освободить» — решение принимает пользователь.
    /// </summary>
    public long PotentiallyRedundantBytes { get; set; }
    public int LargeFileThresholdCount { get; set; }
    public TimeSpan OldThreshold { get; set; }
}
