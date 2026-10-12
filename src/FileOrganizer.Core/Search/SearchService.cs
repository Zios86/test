using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Scanning;

namespace FileOrganizer.Core.Search;

public sealed class SearchQuery
{
    /// <summary>Часть имени или маска (*.pdf, отчет*).</summary>
    public string? Name { get; set; }
    public string? Extension { get; set; }
    public FileCategory? Category { get; set; }
    public long? MinSize { get; set; }
    public long? MaxSize { get; set; }
    public DateTime? ModifiedFrom { get; set; }
    public DateTime? ModifiedTo { get; set; }
    /// <summary>Часть пути к папке.</summary>
    public string? Folder { get; set; }
    /// <summary>Текст внутри документа (читается только для подходящих по остальным условиям файлов).</summary>
    public string? Content { get; set; }
    public string? Tag { get; set; }
    public int Limit { get; set; } = 1000;
}

/// <summary>Поиск по результатам последнего сканирования: имя, расширение, размер, дата, папка, содержимое, теги.</summary>
public static class SearchService
{
    public static List<FileEntry> Search(IEnumerable<FileEntry> files, SearchQuery q, IReadOnlyDictionary<string, List<string>>? tags = null, CancellationToken ct = default)
    {
        System.Text.RegularExpressions.Regex? mask = null;
        string? nameNorm = null;
        if (!string.IsNullOrWhiteSpace(q.Name))
        {
            if (q.Name.Contains('*') || q.Name.Contains('?')) mask = ExclusionMatcher.WildcardToRegex(q.Name.Trim());
            else nameNorm = TextNormalizer.Normalize(q.Name);
        }
        var ext = q.Extension?.Trim().TrimStart('*').TrimStart('.').ToLowerInvariant();
        var folder = q.Folder?.Trim();
        var content = string.IsNullOrWhiteSpace(q.Content) ? null : TextNormalizer.Normalize(q.Content);
        var result = new List<FileEntry>();

        foreach (var f in files)
        {
            if (ct.IsCancellationRequested) break;
            if (mask != null && !mask.IsMatch(f.Name)) continue;
            if (nameNorm != null && !TextNormalizer.Normalize(f.Name).Contains(nameNorm, StringComparison.Ordinal)) continue;
            if (!string.IsNullOrEmpty(ext) && f.Extension != ext) continue;
            if (q.Category != null && f.Category != q.Category) continue;
            if (q.MinSize != null && f.Size < q.MinSize) continue;
            if (q.MaxSize != null && f.Size > q.MaxSize) continue;
            if (q.ModifiedFrom != null && f.LastWriteTimeUtc < q.ModifiedFrom.Value.ToUniversalTime()) continue;
            if (q.ModifiedTo != null && f.LastWriteTimeUtc > q.ModifiedTo.Value.ToUniversalTime()) continue;
            if (!string.IsNullOrEmpty(folder) && f.DirectoryPath.IndexOf(folder, StringComparison.OrdinalIgnoreCase) < 0) continue;
            if (!string.IsNullOrWhiteSpace(q.Tag))
            {
                if (tags == null || !tags.TryGetValue(f.FullPath, out var t) || !t.Contains(q.Tag, StringComparer.OrdinalIgnoreCase)) continue;
            }
            if (content != null)
            {
                if (!ContentExtractor.CanExtract(f.Extension)) continue;
                var text = ContentExtractor.TryExtract(f.FullPath, 200_000);
                if (text == null || !TextNormalizer.Normalize(text).Contains(content, StringComparison.Ordinal)) continue;
            }
            result.Add(f);
            if (result.Count >= q.Limit) break;
        }
        return result;
    }
}
