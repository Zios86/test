using System.Text.RegularExpressions;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Scanning;

/// <summary>Проверяет файлы и папки на соответствие пользовательским исключениям.</summary>
public sealed class ExclusionMatcher
{
    private readonly HashSet<string> _folderNames = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _folderPaths = new();
    private readonly HashSet<string> _extensions = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _files = new(PathUtil.Comparer);
    private readonly List<Regex> _masks = new();

    public ExclusionMatcher(ExclusionSet set)
    {
        foreach (var e in set.Items)
        {
            var p = e.Pattern.Trim();
            if (p.Length == 0) continue;
            switch (e.Kind)
            {
                case ExclusionKind.Folder:
                    if (Path.IsPathRooted(p)) _folderPaths.Add(PathUtil.Normalize(p));
                    else _folderNames.Add(p.Trim('\\', '/'));
                    break;
                case ExclusionKind.Extension:
                    _extensions.Add(p.TrimStart('*').TrimStart('.'));
                    break;
                case ExclusionKind.File:
                    _files.Add(PathUtil.Normalize(p));
                    break;
                case ExclusionKind.Mask:
                    _masks.Add(WildcardToRegex(p));
                    break;
            }
        }
    }

    public static Regex WildcardToRegex(string mask) =>
        new("^" + Regex.Escape(mask).Replace("\\*", ".*").Replace("\\?", ".") + "$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public bool IsFolderExcluded(string fullPath, string name)
    {
        if (_folderNames.Contains(name)) return true;
        foreach (var fp in _folderPaths)
            if (PathUtil.IsUnder(fullPath, fp)) return true;
        foreach (var m in _masks)
            if (m.IsMatch(name)) return true;
        return false;
    }

    public bool IsFileExcluded(string fullPath, string name, string extension)
    {
        if (extension.Length > 0 && _extensions.Contains(extension)) return true;
        if (_files.Count > 0 && _files.Contains(fullPath)) return true;
        foreach (var m in _masks)
            if (m.IsMatch(name)) return true;
        return false;
    }
}
