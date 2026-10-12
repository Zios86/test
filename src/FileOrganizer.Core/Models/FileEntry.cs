namespace FileOrganizer.Core.Models;

/// <summary>Один файл, найденный при сканировании. Содержимое файла не хранится.</summary>
public sealed class FileEntry
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    /// <summary>Расширение в нижнем регистре без точки ("" — если расширения нет).</summary>
    public required string Extension { get; init; }
    public required string DirectoryPath { get; init; }
    public long Size { get; init; }
    public DateTime LastWriteTimeUtc { get; init; }
    public DateTime CreationTimeUtc { get; init; }
    public DateTime LastAccessTimeUtc { get; init; }
    public FileAttributes Attributes { get; init; }
    public FileCategory Category { get; set; } = FileCategory.Other;

    public bool IsReadOnly => (Attributes & FileAttributes.ReadOnly) != 0;
    public bool IsHidden => (Attributes & FileAttributes.Hidden) != 0;
    public string NameWithoutExtension => Extension.Length == 0 ? Name : Name[..^(Extension.Length + 1)];

    public override string ToString() => FullPath;
}

/// <summary>Папка, найденная при сканировании.</summary>
public sealed class FolderEntry
{
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public string? ParentPath { get; init; }
    public int Depth { get; init; }
    public List<FolderEntry> SubFolders { get; } = new();
    public List<FileEntry> Files { get; } = new();

    /// <summary>Суммарный размер файлов в папке и всех вложенных.</summary>
    public long TotalSize { get; set; }
    /// <summary>Количество файлов в папке и всех вложенных.</summary>
    public int TotalFileCount { get; set; }

    public override string ToString() => FullPath;
}
