namespace FileOrganizer.Core.Infrastructure;

/// <summary>
/// Защита системных каталогов: Windows, Program Files, ProgramData, AppData и служебные папки.
/// Эти пути не сканируются и никогда не изменяются.
/// </summary>
public static class SystemPathGuard
{
    /// <summary>Служебные папки, которые не трогаем на любом уровне вложенности.</summary>
    private static readonly HashSet<string> AnyLevelNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "AppData", "$Recycle.Bin", "System Volume Information", "WindowsApps", AppPaths.RecoveryFolderName,
    };

    /// <summary>Системные папки в корне диска (C:\Windows, C:\Program Files...).</summary>
    private static readonly HashSet<string> DriveRootNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Windows", "Program Files", "Program Files (x86)", "ProgramData", "Recovery", "PerfLogs",
        "$WinREAgent", "Config.Msi", "MSOCache", "Boot", "Documents and Settings",
    };

    private static readonly Lazy<List<string>> ProtectedRoots = new(BuildProtectedRoots);

    private static List<string> BuildProtectedRoots()
    {
        var list = new List<string>();
        void Add(Environment.SpecialFolder f)
        {
            try
            {
                var p = Environment.GetFolderPath(f);
                if (!string.IsNullOrWhiteSpace(p)) list.Add(PathUtil.Normalize(p));
            }
            catch { /* ignore */ }
        }
        if (OperatingSystem.IsWindows())
        {
            Add(Environment.SpecialFolder.Windows);
            Add(Environment.SpecialFolder.ProgramFiles);
            Add(Environment.SpecialFolder.ProgramFilesX86);
            Add(Environment.SpecialFolder.CommonApplicationData);
            Add(Environment.SpecialFolder.ApplicationData);
            Add(Environment.SpecialFolder.LocalApplicationData);
            Add(Environment.SpecialFolder.System);
        }
        else
        {
            foreach (var p in new[] { "/bin", "/boot", "/dev", "/etc", "/lib", "/lib64", "/proc", "/sbin", "/sys", "/usr", "/var", "/run" })
                list.Add(p);
        }
        return list;
    }

    /// <summary>Каталог является системным/служебным по имени.</summary>
    /// <param name="parentIsDriveRoot">Папка лежит непосредственно в корне диска.</param>
    public static bool IsProtectedFolderName(string folderName, bool parentIsDriveRoot = false) =>
        AnyLevelNames.Contains(folderName) || (parentIsDriveRoot && DriveRootNames.Contains(folderName));

    /// <summary>Каталог является системным: по имени или по расположению.</summary>
    public static bool IsProtectedFolder(string fullPath)
    {
        var name = Path.GetFileName(Path.TrimEndingDirectorySeparator(fullPath));
        var parent = Path.GetDirectoryName(Path.TrimEndingDirectorySeparator(fullPath));
        var parentIsRoot = parent != null && Path.GetPathRoot(parent) is { } r && PathUtil.SamePath(r, parent);
        return IsProtectedFolderName(name, parentIsRoot) || IsProtectedPath(fullPath);
    }

    /// <summary>Путь лежит внутри системного каталога.</summary>
    public static bool IsProtectedPath(string path)
    {
        string full;
        try { full = PathUtil.Normalize(path); } catch { return true; }
        foreach (var root in ProtectedRoots.Value)
            if (PathUtil.IsUnder(full, root)) return true;
        // Корень диска целиком тоже не трогаем: операции только внутри выбранной папки.
        var r = Path.GetPathRoot(full);
        if (r != null && PathUtil.SamePath(r, full)) return true;
        return false;
    }

    /// <summary>
    /// Проверка перед изменением файла: путь не системный, ни один сегмент пути не является системной папкой,
    /// файл не помечен атрибутом System.
    /// </summary>
    public static bool IsSafeToModify(string path)
    {
        if (IsProtectedPath(path)) return false;
        var full = PathUtil.Normalize(path);
        var segments = full.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        // segments[0] — диск ("C:") в Windows; segments[1] — папка в корне диска.
        var rootChildIndex = OperatingSystem.IsWindows() ? 1 : 0;
        for (int i = 0; i < segments.Length; i++)
        {
            if (segments[i].Equals(AppPaths.RecoveryFolderName, StringComparison.OrdinalIgnoreCase)) continue; // проверяется отдельно
            if (IsProtectedFolderName(segments[i], i == rootChildIndex)) return false;
        }
        try
        {
            if (File.Exists(full) && (File.GetAttributes(full) & FileAttributes.System) != 0) return false;
        }
        catch { return false; }
        return true;
    }
}
