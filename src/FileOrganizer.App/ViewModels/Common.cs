using System.Collections.ObjectModel;
using FileOrganizer.App.Mvvm;
using FileOrganizer.Core.Models;

namespace FileOrganizer.App.ViewModels;

/// <summary>Базовый класс страницы.</summary>
public abstract class PageViewModel : ObservableObject
{
    protected PageViewModel(MainViewModel shell) { Shell = shell; }
    public MainViewModel Shell { get; }
    public abstract string Title { get; }
    public abstract string Icon { get; }
    public virtual string Subtitle => "";
    /// <summary>Вызывается при новом результате анализа.</summary>
    public virtual void OnReportChanged() { }
    /// <summary>Вызывается при открытии страницы.</summary>
    public virtual void OnNavigatedTo() { }
}

/// <summary>Строка-файл с флажком выбора.</summary>
public class FileRow : ObservableObject
{
    public FileRow(FileEntry file, string? note = null, bool isChecked = false)
    {
        File = file;
        Note = note;
        _isChecked = isChecked;
    }

    public FileEntry File { get; }
    public string Name => File.Name;
    public string FullPath => File.FullPath;
    public string Folder => File.DirectoryPath;
    public long Size => File.Size;
    public DateTime Modified => File.LastWriteTimeUtc;
    public string Category => File.Category.DisplayName();
    public string? Note { get; }

    private bool _isChecked;
    public bool IsChecked { get => _isChecked; set => Set(ref _isChecked, value); }
}

public sealed class Option<T>
{
    public Option(T value, string text) { Value = value; Text = text; }
    public T Value { get; }
    public string Text { get; }
    public override string ToString() => Text;
}

public static class CollectionExtensions
{
    public static void ReplaceWith<T>(this ObservableCollection<T> c, IEnumerable<T> items)
    {
        c.Clear();
        foreach (var i in items) c.Add(i);
    }
}
