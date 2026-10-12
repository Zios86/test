using System.Text;
using System.Text.RegularExpressions;

namespace FileOrganizer.Core.Classification;

public static partial class TextNormalizer
{
    /// <summary>Нижний регистр, ё→е, разделители → пробел.</summary>
    public static string Normalize(string s)
    {
        var sb = new StringBuilder(s.Length);
        foreach (var ch in s.ToLowerInvariant())
        {
            var c = ch == 'ё' ? 'е' : ch;
            sb.Append(char.IsLetterOrDigit(c) ? c : ' ');
        }
        return MultiSpace().Replace(sb.ToString(), " ").Trim();
    }

    public static IEnumerable<string> Tokens(string s) =>
        Normalize(s).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    /// <summary>Разбивает CamelCase и смешанные латиница/кириллица/цифры: «KPRostelecom2026» → «KP Rostelecom 2026».</summary>
    public static string SplitWords(string s)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            var c = s[i];
            if (i > 0)
            {
                var p = s[i - 1];
                bool boundary = (char.IsLower(p) && char.IsUpper(c))
                                || (char.IsLetter(p) && char.IsDigit(c))
                                || (char.IsDigit(p) && char.IsLetter(c));
                if (boundary) sb.Append(' ');
            }
            sb.Append(c);
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex MultiSpace();
}
