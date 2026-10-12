using System.Diagnostics;
using System.IO;
using System.Windows;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using Microsoft.Win32;

namespace FileOrganizer.App.Services;

/// <summary>Общие службы приложения: база, журнал, настройки, тема, Корзина, оболочка Windows.</summary>
public static class AppServices
{
    public static Database Db { get; private set; } = null!;
    public static Logger Log { get; private set; } = Logger.Null;
    public static AppSettings Settings { get; private set; } = new();
    public static IRecycleBin RecycleBin { get; } = new WindowsRecycleBin();

    public static void Initialize()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        Log = new Logger(AppPaths.LogDirectory);
        Db = new Database(AppPaths.DatabasePath);
        Settings = Db.LoadSettings();
    }

    public static void SaveSettings() => Db.SaveSettings(Settings);

    public static FileClassifier CreateClassifier()
    {
        if (Settings.CustomCategories is not { Count: > 0 } custom) return new FileClassifier();
        var map = new Dictionary<FileCategory, string[]>();
        foreach (var (key, exts) in custom)
            if (Enum.TryParse<FileCategory>(key, out var cat)) map[cat] = exts;
        foreach (var (cat, exts) in FileClassifier.DefaultMap)
            if (!map.ContainsKey(cat)) map[cat] = exts;
        return new FileClassifier(map);
    }

    /// <summary>ИИ-классификатор согласно настройкам. Внешний (не локальный) сервис — только с явного согласия.</summary>
    public static IAIClassifier? CreateAIClassifier(out string? warning)
    {
        warning = null;
        if (!Settings.AIEnabled) return null;
        if (!Uri.TryCreate(Settings.AIEndpoint, UriKind.Absolute, out var uri))
        {
            warning = "Адрес ИИ-сервиса указан неверно — ИИ не используется.";
            return null;
        }
        if (!uri.IsLoopback && !Settings.AllowRemoteAI)
        {
            warning = "ИИ-сервис находится не на этом компьютере, а согласие на передачу фрагментов документов не дано — ИИ не используется.";
            return null;
        }
        var http = new System.Net.Http.HttpClient { BaseAddress = uri, Timeout = TimeSpan.FromSeconds(60) };
        return new OllamaClassifier(http, Settings.AIModel);
    }

    public static void OpenInExplorer(string path)
    {
        try
        {
            if (File.Exists(path)) Process.Start("explorer.exe", $"/select,\"{path}\"");
            else if (Directory.Exists(path)) Process.Start("explorer.exe", $"\"{path}\"");
            else MessageBox.Show("Файл или папка больше не существует:\n" + path, "Открыть", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            Log.Error("OpenInExplorer", path, ex);
        }
    }

    public static void OpenFile(string path)
    {
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error("OpenFile", path, ex);
            MessageBox.Show("Не удалось открыть файл: " + ex.Message, "Открыть", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public static string? PickFolder(string? initial = null)
    {
        var dlg = new OpenFolderDialog { Title = "Выберите папку", Multiselect = false };
        if (!string.IsNullOrEmpty(initial) && Directory.Exists(initial)) dlg.InitialDirectory = initial;
        return dlg.ShowDialog() == true ? dlg.FolderName : null;
    }

    public static bool Confirm(string text, string title = "Подтверждение", MessageBoxImage icon = MessageBoxImage.Question) =>
        MessageBox.Show(text, title, MessageBoxButton.YesNo, icon, MessageBoxResult.No) == MessageBoxResult.Yes;

    public static void Info(string text, string title = "Умный файловый организатор") =>
        MessageBox.Show(text, title, MessageBoxButton.OK, MessageBoxImage.Information);
}

/// <summary>Отправка в Корзину Windows через Microsoft.VisualBasic (SHFileOperation).</summary>
public sealed class WindowsRecycleBin : IRecycleBin
{
    public bool IsSupported => OperatingSystem.IsWindows();

    public void SendToRecycleBin(string path) =>
        Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path,
            Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
            Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
}

/// <summary>Тёмная/светлая/системная тема.</summary>
public static class ThemeManager
{
    public static void Apply(AppTheme theme)
    {
        bool dark = theme switch
        {
            AppTheme.Dark => true,
            AppTheme.Light => false,
            _ => IsSystemDark(),
        };
        var dicts = Application.Current.Resources.MergedDictionaries;
        var uri = new Uri(dark ? "Themes/Dark.xaml" : "Themes/Light.xaml", UriKind.Relative);
        var newDict = new ResourceDictionary { Source = uri };
        var old = dicts.FirstOrDefault(d => d.Source != null && (d.Source.OriginalString.EndsWith("Light.xaml") || d.Source.OriginalString.EndsWith("Dark.xaml")));
        if (old != null) dicts[dicts.IndexOf(old)] = newDict;
        else dicts.Insert(0, newDict);
    }

    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 0;
        }
        catch
        {
            return false;
        }
    }
}
