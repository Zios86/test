namespace FileOrganizer.Core.Infrastructure;

public static class PathUtil
{
    public static readonly StringComparer Comparer =
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    public static StringComparison Comparison =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    public static string Normalize(string path) =>
        Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

    /// <summary>true, если <paramref name="path"/> совпадает с <paramref name="root"/> или лежит внутри него.</summary>
    public static bool IsUnder(string path, string root)
    {
        var p = Normalize(path);
        var r = Normalize(root);
        if (string.Equals(p, r, Comparison)) return true;
        return p.StartsWith(r + Path.DirectorySeparatorChar, Comparison)
            || (r.EndsWith(Path.DirectorySeparatorChar) && p.StartsWith(r, Comparison));
    }

    public static bool SamePath(string a, string b) => string.Equals(Normalize(a), Normalize(b), Comparison);

    /// <summary>
    /// Возвращает свободный путь: «имя.ext», «имя (2).ext», «имя (3).ext»…
    /// <paramref name="isTaken"/> позволяет учесть ещё и пути, которые займут другие запланированные операции.
    /// </summary>
    public static string MakeUnique(string desiredPath, Func<string, bool>? isTaken = null)
    {
        bool Taken(string p) => File.Exists(p) || Directory.Exists(p) || (isTaken?.Invoke(p) ?? false);
        if (!Taken(desiredPath)) return desiredPath;
        var dir = Path.GetDirectoryName(desiredPath) ?? "";
        var ext = Path.GetExtension(desiredPath);
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        for (int i = 2; i < 100000; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i}){ext}");
            if (!Taken(candidate)) return candidate;
        }
        throw new IOException($"Не удалось подобрать свободное имя для {desiredPath}");
    }

    private static readonly char[] Invalid = Path.GetInvalidFileNameChars()
        .Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).Distinct().ToArray();

    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    /// <summary>Делает строку допустимым именем файла/папки в Windows.</summary>
    public static string SanitizeFileName(string name, string fallback = "Без_имени")
    {
        var chars = name.Select(c => Invalid.Contains(c) || char.IsControl(c) ? '_' : c).ToArray();
        var s = new string(chars).Trim().TrimEnd('.', ' ');
        if (s.Length == 0) s = fallback;
        if (ReservedWindowsNames.Contains(Path.GetFileNameWithoutExtension(s))) s = "_" + s;
        if (s.Length > 150) s = s[..150].TrimEnd('.', ' ');
        return s;
    }

    public static string RelativeTo(string root, string path) => Path.GetRelativePath(root, path);
}
