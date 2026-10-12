using System.Globalization;
using System.Text.RegularExpressions;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Выявляет бессмысленные и хаотичные названия («Новый документ (17).docx», «123.pdf», «IMG_5678.jpg»,
/// «final_final2_new.docx», «копия копия.xlsx») и предлагает понятное имя. Имена не меняются автоматически.
/// </summary>
public static partial class NameAnalyzer
{
    private static readonly (Regex Pattern, string Problem)[] Patterns =
    {
        (new Regex(@"^(новый|новая|new)\s*(документ|текстовый документ|таблица|презентация|папка|document|text document|microsoft \w+ \w+|folder)(\s*\(\d+\))?$", RegexOptions.IgnoreCase), "стандартное имя нового файла"),
        (new Regex(@"^(документ|document|doc|книга|book|лист|sheet|презентация|presentation)\s*\d*(\s*\(\d+\))?$", RegexOptions.IgnoreCase), "стандартное имя без смысла"),
        (new Regex(@"^(безымянный|без имени|untitled|unnamed|noname|image|изображение|рисунок|file|файл)(\s*[-_]?\s*\d*)(\s*\(\d+\))?$", RegexOptions.IgnoreCase), "безымянный файл"),
        (new Regex(@"^[\d\s_\-\.()]+$"), "имя состоит только из цифр"),
        (new Regex(@"^(img|dsc|dscn|dcim|pxl|mvimg|vid|mov|photo|scan|scr|screenshot)[_\- ]?\d+", RegexOptions.IgnoreCase), "имя, присвоенное камерой или устройством"),
        (new Regex(@"(final.*final|финал.*финал|new.*new|копия.*копия|copy.*copy|(final|финал)\w*[_\- ]*(new|новый|\d)|(_|\s)(new|old)\d*$)", RegexOptions.IgnoreCase), "несколько пометок версии в имени"),
        (new Regex(@"^(копия|copy of|копия\s+копия)\b|(\s|_|-)(копия|copy)(\s*\(\d+\))?$", RegexOptions.IgnoreCase), "имя копии"),
        (new Regex(@"^[a-z0-9]{20,}$", RegexOptions.IgnoreCase), "случайный набор символов"),
        (new Regex(@"^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$", RegexOptions.IgnoreCase), "имя-идентификатор (GUID)"),
    };

    [GeneratedRegex(@"(\(\d+\)|(?<![\p{L}\p{N}])(final|финал\w*|new|новый|новая|old|copy of|copy|копия|draft|v\d+|ver\d+)(?!\p{L})|\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex Noise();

    private static readonly HashSet<string> Generic = new(StringComparer.OrdinalIgnoreCase)
    {
        "новый", "новая", "документ", "текстовый", "document", "doc", "untitled", "unnamed", "noname", "безымянный", "без", "имени",
        "image", "изображение", "рисунок", "file", "файл", "img", "dsc", "dscn", "pxl", "mvimg", "photo", "scan", "scr", "screenshot", "vid", "mov",
        "microsoft", "word", "excel", "powerpoint", "книга", "book", "лист", "sheet", "таблица", "презентация", "presentation", "text", "of",
    };

    public static string? DetectProblem(FileEntry f)
    {
        var name = f.NameWithoutExtension.Trim();
        if (name.Length == 0) return "пустое имя";
        foreach (var (pattern, problem) in Patterns)
            if (pattern.IsMatch(name)) return problem;
        return null;
    }

    /// <summary>
    /// Предлагает имя: очищенное от «шумовых» пометок, а если смысла не осталось —
    /// «ГГГГ-ММ-ДД_Тема_или_Папка». Расширение сохраняется.
    /// </summary>
    public static string SuggestName(FileEntry f, string? topic = null)
    {
        var date = f.LastWriteTimeUtc.ToLocalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var cleaned = Noise().Replace(f.NameWithoutExtension, " ");
        var words = TextNormalizer.Tokens(cleaned).Where(w => !Generic.Contains(w)).ToList();
        string core;
        if (words.Count > 0)
        {
            // Сохраняем исходный регистр слов.
            var original = Regex.Split(cleaned, @"[\s_\-\.]+").Where(w => w.Length > 0 && !Generic.Contains(w.ToLowerInvariant())).ToList();
            core = string.Join("_", original.Count > 0 ? original : words);
        }
        else
        {
            var parent = Path.GetFileName(f.DirectoryPath);
            core = topic ?? (string.IsNullOrWhiteSpace(parent) ? f.Category.DisplayName() : parent);
            core = Regex.Replace(core.Trim(), @"\s+", "_");
        }
        var ext = f.Extension.Length > 0 ? "." + f.Extension : "";
        return PathUtil.SanitizeFileName($"{date}_{core}") + ext;
    }

    public static List<BadNameInfo> Find(IEnumerable<FileEntry> files)
    {
        var result = new List<BadNameInfo>();
        foreach (var f in files)
        {
            var problem = DetectProblem(f);
            if (problem == null) continue;
            var suggested = SuggestName(f);
            if (string.Equals(suggested, f.Name, StringComparison.OrdinalIgnoreCase)) continue;
            result.Add(new BadNameInfo { File = f, Problem = problem, SuggestedName = suggested });
        }
        return result;
    }
}
