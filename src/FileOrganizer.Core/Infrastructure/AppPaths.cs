namespace FileOrganizer.Core.Infrastructure;

/// <summary>Расположение данных программы: %LOCALAPPDATA%\FileOrganizer.</summary>
public static class AppPaths
{
    public static string DataDirectory
    {
        get
        {
            var overridePath = Environment.GetEnvironmentVariable("FILEORGANIZER_DATA");
            if (!string.IsNullOrWhiteSpace(overridePath)) return overridePath;
            var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(baseDir)) baseDir = Path.Combine(Path.GetTempPath(), "FileOrganizerData");
            return Path.Combine(baseDir, "FileOrganizer");
        }
    }

    public static string DatabasePath => Path.Combine(DataDirectory, "fileorganizer.db");
    public static string LogDirectory => Path.Combine(DataDirectory, "logs");

    public const string RecoveryFolderName = "FileOrganizer_Recovery";
}
