using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Classification;

/// <summary>Классификация по расширению. Таблица типов настраивается пользователем.</summary>
public sealed class FileClassifier
{
    private readonly Dictionary<string, FileCategory> _map = new(StringComparer.OrdinalIgnoreCase);

    public static IReadOnlyDictionary<FileCategory, string[]> DefaultMap { get; } = new Dictionary<FileCategory, string[]>
    {
        [FileCategory.Documents] = new[] { "doc", "docx", "pdf", "txt", "rtf", "odt", "md", "djvu", "epub", "fb2", "xps" },
        [FileCategory.Spreadsheets] = new[] { "xls", "xlsx", "xlsm", "csv", "ods", "tsv" },
        [FileCategory.Presentations] = new[] { "ppt", "pptx", "odp", "key" },
        [FileCategory.Images] = new[] { "jpg", "jpeg", "png", "gif", "webp", "heic", "heif", "tif", "tiff", "bmp", "svg", "raw", "cr2", "nef", "ico" },
        [FileCategory.Video] = new[] { "mp4", "mkv", "avi", "mov", "webm", "wmv", "m4v", "flv", "3gp", "mpg", "mpeg" },
        [FileCategory.Audio] = new[] { "mp3", "wav", "flac", "aac", "ogg", "m4a", "wma", "opus" },
        [FileCategory.Archives] = new[] { "zip", "rar", "7z", "tar", "gz", "tgz", "bz2", "xz", "iso" },
        [FileCategory.Installers] = new[] { "exe", "msi", "msix", "appx", "dmg", "pkg", "deb", "rpm", "apk" },
        [FileCategory.Code] = new[] { "py", "js", "ts", "tsx", "jsx", "html", "htm", "css", "scss", "sql", "ps1", "bat", "cmd", "sh", "cs", "java", "cpp", "c", "h", "go", "rs", "php", "rb", "json", "xml", "yaml", "yml", "ipynb" },
        [FileCategory.Temporary] = new[] { "tmp", "temp", "crdownload", "part", "partial", "download", "bak", "old", "~" },
    };

    public FileClassifier() : this(DefaultMap) { }

    public FileClassifier(IReadOnlyDictionary<FileCategory, string[]> map)
    {
        foreach (var (cat, exts) in map)
            foreach (var e in exts)
                _map[e.TrimStart('.')] = cat;
    }

    public IReadOnlyDictionary<string, FileCategory> Map => _map;

    public FileCategory Classify(FileEntry f) => Classify(f.Extension);

    public FileCategory Classify(string extension) =>
        _map.TryGetValue(extension.TrimStart('.'), out var c) ? c : FileCategory.Other;

    /// <summary>Расширение в нижнем регистре без точки. «.bashrc» и «README» — без расширения.</summary>
    public static string GetExtension(string fileName)
    {
        var idx = fileName.LastIndexOf('.');
        if (idx <= 0 || idx == fileName.Length - 1) return "";
        var ext = fileName[(idx + 1)..];
        // «Отчёт v1.2 final» — не расширение.
        if (ext.Length > 10 || ext.Contains(' ')) return "";
        return ext.ToLowerInvariant();
    }
}
