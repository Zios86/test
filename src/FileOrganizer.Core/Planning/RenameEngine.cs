using System.Globalization;
using System.Text.RegularExpressions;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Planning;

public sealed record RenamePreview(FileEntry File, string OldName, string NewName, bool Changed, string? Problem);

/// <summary>
/// Массовое переименование по шаблону. Перед применением всегда строится предпросмотр «Было → Станет».
/// Подстановки: {date} (ГГГГ-ММ-ДД), {year}, {month}, {name} (исходное имя), {clean} (имя без пометок копий/версий),
/// {parent} (папка), {type} (категория), {project}, {client}, {doc} (тип документа), {counter} / {counter:000}.
/// </summary>
public sealed partial class RenameEngine
{
    public static readonly (string Title, string Template)[] Presets =
    {
        ("Дата_Название", "{date}_{clean}"),
        ("Проект_Тип_Дата", "{project}_{doc}_{date}"),
        ("Клиент_Документ_Год", "{client}_{doc}_{year}"),
        ("Папка_Номер", "{parent}_{counter:000}"),
    };

    public string Template { get; set; } = "{date}_{clean}";
    public string Project { get; set; } = "";
    public string Client { get; set; } = "";
    public string DocType { get; set; } = "";
    public int CounterStart { get; set; } = 1;
    public string? Find { get; set; }
    public string? Replace { get; set; }

    [GeneratedRegex(@"\{counter(:(?<fmt>0+))?\}")]
    private static partial Regex CounterToken();

    public List<RenamePreview> Preview(IReadOnlyList<FileEntry> files)
    {
        var result = new List<RenamePreview>();
        var taken = new Dictionary<string, HashSet<string>>(PathUtil.Comparer);
        int counter = CounterStart;
        foreach (var f in files)
        {
            var newBase = Apply(f, counter++);
            string? problem = null;
            if (string.IsNullOrWhiteSpace(newBase)) { newBase = f.NameWithoutExtension; problem = "шаблон дал пустое имя"; }
            var ext = f.Extension.Length > 0 ? "." + Path.GetExtension(f.Name).TrimStart('.') : "";
            var newName = PathUtil.SanitizeFileName(newBase) + ext;

            if (!taken.TryGetValue(f.DirectoryPath, out var set)) taken[f.DirectoryPath] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.Equals(newName, f.Name, StringComparison.Ordinal))
            {
                var candidate = newName;
                var full = Path.Combine(f.DirectoryPath, candidate);
                if (set.Contains(candidate) || (File.Exists(full) && !PathUtil.SamePath(full, f.FullPath)))
                {
                    candidate = Path.GetFileName(PathUtil.MakeUnique(full, p => set.Contains(Path.GetFileName(p))));
                    problem = "имя уже занято — будет добавлен номер";
                }
                newName = candidate;
            }
            set.Add(newName);
            result.Add(new RenamePreview(f, f.Name, newName, !string.Equals(newName, f.Name, StringComparison.Ordinal), problem));
        }
        return result;
    }

    private string Apply(FileEntry f, int counter)
    {
        var local = f.LastWriteTimeUtc.ToLocalTime();
        var name = f.NameWithoutExtension;
        if (!string.IsNullOrEmpty(Find)) name = name.Replace(Find, Replace ?? "", StringComparison.OrdinalIgnoreCase);
        var clean = Clean(name);
        var s = Template
            .Replace("{date}", local.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture))
            .Replace("{year}", local.Year.ToString(CultureInfo.InvariantCulture))
            .Replace("{month}", local.ToString("MM", CultureInfo.InvariantCulture))
            .Replace("{name}", name)
            .Replace("{clean}", clean)
            .Replace("{parent}", Path.GetFileName(f.DirectoryPath))
            .Replace("{type}", f.Category.DisplayName())
            .Replace("{project}", Project)
            .Replace("{client}", Client)
            .Replace("{doc}", DocType);
        s = CounterToken().Replace(s, m => counter.ToString(m.Groups["fmt"].Success ? m.Groups["fmt"].Value : "0", CultureInfo.InvariantCulture));
        // Убираем двойные/висячие разделители, если какие-то поля пустые.
        s = Regex.Replace(s, @"[_\- ]{2,}", "_").Trim('_', '-', ' ');
        return s.Replace(' ', '_');
    }

    [GeneratedRegex(@"(\s*\(\d+\)|[_\-\s]*(?<![\p{L}\p{N}])(final|финал\w*|new|новый|новая|copy|копия|draft|черновик)\d*(?!\p{L}))", RegexOptions.IgnoreCase)]
    private static partial Regex Markers();

    /// <summary>Убирает пометки копий/версий: «Отчёт_final_final2 (1)» → «Отчёт».</summary>
    public static string Clean(string name)
    {
        var s = Markers().Replace(name, "");
        s = Regex.Replace(s, @"[_\-\s]{2,}", "_").Trim('_', '-', ' ', '.');
        return s.Length == 0 ? name : s;
    }
}
