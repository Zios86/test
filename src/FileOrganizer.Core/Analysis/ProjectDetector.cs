using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Проектные группы: документы, таблицы, презентации, изображения и архивы с общим «проектным» словом в имени.
/// Например «ABC_ТЗ.docx», «ABC_смета.xlsx», «ABC_защита.pptx» → «Проект_ABC».
/// </summary>
public static class ProjectDetector
{
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "final", "new", "old", "copy", "draft", "img", "dsc", "scan", "screenshot", "image", "photo", "document", "doc", "file",
        "для", "по", "от", "на", "из", "при", "без", "под", "над", "это", "как", "что", "все", "или", "года", "год",
        "копия", "новый", "новая", "финал", "версия", "проект", "project", "документ", "файл", "фото", "скан", "отчет", "отчеты", "report",
        "договор", "счет", "акт", "кп", "презентация", "таблица", "протокол", "инструкция", "резюме", "часть", "part", "page", "стр",
        "январь", "февраль", "март", "апрель", "май", "июнь", "июль", "август", "сентябрь", "октябрь", "ноябрь", "декабрь",
        "whatsapp", "telegram", "microsoft", "word", "excel", "powerpoint", "pdf", "docx", "xlsx",
    };

    public static List<ProjectGroup> Find(IEnumerable<FileEntry> files, int minFiles = 3, int minCategories = 2)
    {
        var tokenFiles = new Dictionary<string, List<FileEntry>>(StringComparer.OrdinalIgnoreCase);
        foreach (var f in files)
        {
            if (f.Category is FileCategory.Temporary or FileCategory.Installers or FileCategory.Code) continue;
            var tokens = TextNormalizer.Tokens(TextNormalizer.SplitWords(f.NameWithoutExtension))
                .Where(t => t.Length >= 3 && !t.All(char.IsDigit) && !Stop.Contains(t))
                .Distinct();
            foreach (var t in tokens)
            {
                if (!tokenFiles.TryGetValue(t, out var l)) tokenFiles[t] = l = new List<FileEntry>();
                l.Add(f);
            }
        }

        var groups = tokenFiles
            .Where(kv => kv.Value.Count >= minFiles && kv.Value.Select(f => f.Category).Distinct().Count() >= minCategories)
            // Слишком частые слова — это не проект, а общее слово.
            .Where(kv => kv.Value.Count <= 500)
            .OrderByDescending(kv => kv.Value.Select(f => f.Category).Distinct().Count())
            .ThenByDescending(kv => kv.Value.Count)
            .ToList();

        var used = new HashSet<FileEntry>();
        var result = new List<ProjectGroup>();
        foreach (var (token, list) in groups)
        {
            var free = list.Where(f => !used.Contains(f)).ToList();
            if (free.Count < minFiles || free.Select(f => f.Category).Distinct().Count() < minCategories) continue;
            foreach (var f in free) used.Add(f);
            var display = free.Select(f => FindOriginalCase(f.NameWithoutExtension, token)).FirstOrDefault(s => s != null) ?? token;
            result.Add(new ProjectGroup
            {
                Name = display,
                Files = free,
                Reason = $"слово «{display}» встречается в названиях {free.Count} файлов разных типов ({string.Join(", ", free.Select(f => f.Category.DisplayName().ToLowerInvariant()).Distinct())})",
            });
        }
        return result;
    }

    private static string? FindOriginalCase(string name, string token)
    {
        var idx = TextNormalizer.SplitWords(name).IndexOf(token, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        return TextNormalizer.SplitWords(name).Substring(idx, token.Length);
    }

    /// <summary>Подпапка проекта по типу файла.</summary>
    public static string SubfolderFor(FileCategory c) => c switch
    {
        FileCategory.Documents => "Документы",
        FileCategory.Spreadsheets => "Данные",
        FileCategory.Presentations => "Презентации",
        FileCategory.Archives => "Архив",
        _ => "Материалы",
    };
}
