using System.Globalization;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Rules;

public enum RuleField { Extension, Category, Name, NameWithoutExtension, FolderPath, Size, Modified, Created }

public enum RuleOperator
{
    Equals, NotEquals, Contains, NotContains, StartsWith, EndsWith, Matches, InList,
    GreaterThan, LessThan, OlderThanDays, NewerThanDays, Before, After,
}

/// <summary>Условие правила: «тип файла = PDF», «имя содержит счёт», «дата старше 2024».</summary>
public sealed class RuleCondition
{
    public RuleField Field { get; set; }
    public RuleOperator Operator { get; set; }
    public string Value { get; set; } = "";

    public override string ToString() => $"{FieldName(Field)} {OperatorName(Operator)} «{Value}»";

    public static string FieldName(RuleField f) => f switch
    {
        RuleField.Extension => "расширение",
        RuleField.Category => "тип файла",
        RuleField.Name => "имя",
        RuleField.NameWithoutExtension => "имя без расширения",
        RuleField.FolderPath => "папка",
        RuleField.Size => "размер (МБ)",
        RuleField.Modified => "дата изменения",
        RuleField.Created => "дата создания",
        _ => f.ToString(),
    };

    public static string OperatorName(RuleOperator o) => o switch
    {
        RuleOperator.Equals => "=",
        RuleOperator.NotEquals => "≠",
        RuleOperator.Contains => "содержит",
        RuleOperator.NotContains => "не содержит",
        RuleOperator.StartsWith => "начинается с",
        RuleOperator.EndsWith => "заканчивается на",
        RuleOperator.Matches => "соответствует шаблону",
        RuleOperator.InList => "одно из",
        RuleOperator.GreaterThan => ">",
        RuleOperator.LessThan => "<",
        RuleOperator.OlderThanDays => "старше (дней)",
        RuleOperator.NewerThanDays => "новее (дней)",
        RuleOperator.Before => "раньше",
        RuleOperator.After => "позже",
        _ => o.ToString(),
    };
}

public enum RuleActionKind { MoveTo, Skip, Tag }

/// <summary>
/// Пользовательское правило из визуального конструктора.
/// Пример: ЕСЛИ тип файла = PDF И имя содержит «счет» ТО переместить в «Документы/Счета».
/// </summary>
public sealed class Rule
{
    public long Id { get; set; }
    public string Name { get; set; } = "Новое правило";
    public bool Enabled { get; set; } = true;
    public int Priority { get; set; }
    /// <summary>true — все условия (И), false — любое (ИЛИ).</summary>
    public bool MatchAll { get; set; } = true;
    public List<RuleCondition> Conditions { get; set; } = new();
    public RuleActionKind Action { get; set; } = RuleActionKind.MoveTo;
    /// <summary>
    /// Для MoveTo — папка относительно корня. Поддерживаются подстановки:
    /// {year}, {month}, {category}, {ext}. Для Tag — имя тега.
    /// </summary>
    public string Target { get; set; } = "";

    [JsonIgnore]
    public string Description =>
        $"Если {string.Join(MatchAll ? " и " : " или ", Conditions.Select(c => c.ToString()))} → " + Action switch
        {
            RuleActionKind.MoveTo => $"переместить в «{Target}»",
            RuleActionKind.Tag => $"присвоить тег «{Target}»",
            _ => "не трогать",
        };

    public bool IsMatch(FileEntry f, DateTime? nowUtc = null)
    {
        if (Conditions.Count == 0) return false;
        return MatchAll ? Conditions.All(c => Evaluate(c, f, nowUtc ?? DateTime.UtcNow))
                        : Conditions.Any(c => Evaluate(c, f, nowUtc ?? DateTime.UtcNow));
    }

    public string ResolveTarget(FileEntry f)
    {
        var local = f.LastWriteTimeUtc.ToLocalTime();
        return Target
            .Replace("{year}", local.Year.ToString(CultureInfo.InvariantCulture))
            .Replace("{month}", local.ToString("MM", CultureInfo.InvariantCulture))
            .Replace("{category}", f.Category.DisplayName())
            .Replace("{ext}", f.Extension.Length > 0 ? f.Extension.ToUpperInvariant() : "Без расширения");
    }

    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    internal static bool Evaluate(RuleCondition c, FileEntry f, DateTime nowUtc)
    {
        switch (c.Field)
        {
            case RuleField.Size:
                if (!double.TryParse(c.Value.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var mb)) return false;
                var bytes = (long)(mb * 1024 * 1024);
                return c.Operator switch
                {
                    RuleOperator.GreaterThan => f.Size > bytes,
                    RuleOperator.LessThan => f.Size < bytes,
                    RuleOperator.Equals => f.Size == bytes,
                    _ => false,
                };
            case RuleField.Modified:
            case RuleField.Created:
                var date = c.Field == RuleField.Modified ? f.LastWriteTimeUtc : f.CreationTimeUtc;
                switch (c.Operator)
                {
                    case RuleOperator.OlderThanDays when int.TryParse(c.Value, out var d1): return date < nowUtc.AddDays(-d1);
                    case RuleOperator.NewerThanDays when int.TryParse(c.Value, out var d2): return date >= nowUtc.AddDays(-d2);
                    case RuleOperator.Before or RuleOperator.LessThan:
                        return TryParseDate(c.Value, out var b) && date.ToLocalTime() < b;
                    case RuleOperator.After or RuleOperator.GreaterThan:
                        return TryParseDate(c.Value, out var a, endOfPeriod: true) && date.ToLocalTime() >= a;
                    default: return false;
                }
        }

        string value = c.Field switch
        {
            RuleField.Extension => f.Extension,
            RuleField.Category => f.Category.DisplayName(),
            RuleField.Name => f.Name,
            RuleField.NameWithoutExtension => f.NameWithoutExtension,
            RuleField.FolderPath => f.DirectoryPath,
            _ => "",
        };
        var expected = c.Value.Trim();
        if (c.Field == RuleField.Extension) expected = expected.TrimStart('*').TrimStart('.');
        if (c.Field == RuleField.Category && Enum.TryParse<FileCategory>(expected, true, out var cat)) expected = cat.DisplayName();

        static string N(string s) => s.ToLowerInvariant().Replace('ё', 'е');
        var v = N(value);
        var e = N(expected);
        return c.Operator switch
        {
            RuleOperator.Equals => v == e,
            RuleOperator.NotEquals => v != e,
            RuleOperator.Contains => v.Contains(e),
            RuleOperator.NotContains => !v.Contains(e),
            RuleOperator.StartsWith => v.StartsWith(e, StringComparison.Ordinal),
            RuleOperator.EndsWith => v.EndsWith(e, StringComparison.Ordinal),
            RuleOperator.InList => e.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.TrimStart('.')).Contains(v),
            RuleOperator.Matches => SafeRegex(expected, value),
            _ => false,
        };
    }

    private static bool SafeRegex(string pattern, string value)
    {
        try
        {
            // Шаблон с * и ? трактуем как маску, иначе — как регулярное выражение.
            var regex = pattern.IndexOfAny(new[] { '^', '$', '(', '[', '\\', '+', '{' }) < 0
                ? Scanning.ExclusionMatcher.WildcardToRegex(pattern)
                : new Regex(pattern, RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
            return regex.IsMatch(value);
        }
        catch (Exception ex) when (ex is ArgumentException or RegexMatchTimeoutException)
        {
            return false;
        }
    }

    /// <summary>Принимает «2024», «2024-05», «2024-05-17», «17.05.2024».</summary>
    internal static bool TryParseDate(string s, out DateTime date, bool endOfPeriod = false)
    {
        s = s.Trim();
        if (int.TryParse(s, out var year) && year is > 1900 and < 3000)
        {
            date = endOfPeriod ? new DateTime(year + 1, 1, 1) : new DateTime(year, 1, 1);
            return true;
        }
        if (DateTime.TryParseExact(s, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var ym))
        {
            date = endOfPeriod ? ym.AddMonths(1) : ym;
            return true;
        }
        if (DateTime.TryParse(s, Ru, DateTimeStyles.None, out var d) || DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out d))
        {
            date = endOfPeriod ? d.Date.AddDays(1) : d.Date;
            return true;
        }
        date = default;
        return false;
    }
}
