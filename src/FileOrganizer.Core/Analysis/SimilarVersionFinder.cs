using System.Text.RegularExpressions;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Похожие версии документов: «Отчёт.docx», «Отчёт (1).docx», «Отчёт_final.docx», «Отчёт_final2.docx».
/// Это НЕ дубликаты — программа только показывает их рядом, чтобы пользователь сравнил.
/// </summary>
public static partial class SimilarVersionFinder
{
    [GeneratedRegex(@"(\(\d+\)|\b(?:(?:final|финал|финальн\w*|new|новый|новая|old|старый|copy|копия|draft|черновик|v|ver|версия\s*|ред|исправлен\w*|fix(?:ed)?)\d*|\d+)\b)", RegexOptions.IgnoreCase)]
    private static partial Regex VersionMarkers();

    public static string BaseName(string nameWithoutExt)
    {
        var s = TextNormalizer.Normalize(nameWithoutExt);
        string prev;
        do
        {
            prev = s;
            s = VersionMarkers().Replace(s, " ");
            s = TextNormalizer.Normalize(s);
        } while (s != prev);
        return s;
    }

    public static List<SimilarVersionGroup> Find(IEnumerable<FileEntry> files, ISet<string>? exactDuplicatePaths = null)
    {
        var docCats = new[] { FileCategory.Documents, FileCategory.Spreadsheets, FileCategory.Presentations };
        return files.Where(f => docCats.Contains(f.Category))
            .Select(f => (f, key: BaseName(f.NameWithoutExtension)))
            .Where(x => x.key.Length >= 3)
            .GroupBy(x => (x.key, x.f.Category))
            .Where(g => g.Count() > 1)
            // Группа интересна, только если файлы не являются точными копиями друг друга.
            .Where(g => exactDuplicatePaths == null || g.Count(x => !exactDuplicatePaths.Contains(x.f.FullPath)) > 1)
            .Where(g => g.Select(x => x.f.Name).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1)
            .Select(g => new SimilarVersionGroup { BaseName = g.Key.key, Files = g.Select(x => x.f).OrderByDescending(f => f.LastWriteTimeUtc).ToList() })
            .OrderByDescending(g => g.Files.Count)
            .ToList();
    }
}
