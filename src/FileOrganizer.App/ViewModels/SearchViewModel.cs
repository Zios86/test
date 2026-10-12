using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Search;

namespace FileOrganizer.App.ViewModels;

/// <summary>Поиск: имя, расширение, тип, размер, дата, папка, содержимое, теги. Виртуальные теги не меняют расположение файлов.</summary>
public sealed class SearchViewModel : FileListPageViewModel
{
    public SearchViewModel(MainViewModel shell) : base(shell)
    {
        SearchCommand = new AsyncCommand(SearchAsync, () => Shell.HasReport);
        AddTagCommand = new RelayCommand(AddTag);
        RemoveTagCommand = new RelayCommand(RemoveTag);
        Categories = new List<Option<FileCategory?>> { new(null, "Любой тип") }
            .Concat(Enum.GetValues<FileCategory>().Select(c => new Option<FileCategory?>(c, c.DisplayName()))).ToList();
        _category = Categories[0];
        ReloadTags();
    }

    public override string Title => "Поиск";
    public override string Icon => "";

    public List<Option<FileCategory?>> Categories { get; }
    private Option<FileCategory?> _category;
    public Option<FileCategory?> Category { get => _category; set => Set(ref _category, value); }
    public string Name { get; set; } = "";
    public string Extension { get; set; } = "";
    public string MinSizeMb { get; set; } = "";
    public string MaxSizeMb { get; set; } = "";
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public string FolderPart { get; set; } = "";
    public string Content { get; set; } = "";
    public string Tag { get; set; } = "";
    private string _newTag = "";
    public string NewTag { get => _newTag; set => Set(ref _newTag, value); }
    private string _status = "";
    public string Status { get => _status; set => Set(ref _status, value); }
    public ObservableCollection<string> AllTags { get; } = new();

    public ICommand SearchCommand { get; }
    public ICommand AddTagCommand { get; }
    public ICommand RemoveTagCommand { get; }

    private void ReloadTags()
    {
        try { AllTags.ReplaceWith(AppServices.Db.GetTags().Select(t => t.Name)); }
        catch (Exception ex) { AppServices.Log.Error("Tags", null, ex); }
    }

    private static long? Mb(string s) =>
        double.TryParse(s.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out var v) ? (long)(v * 1024 * 1024) : null;

    private async Task SearchAsync()
    {
        var r = Shell.Report;
        if (r == null) return;
        var q = new SearchQuery
        {
            Name = Name, Extension = Extension, Category = Category.Value, MinSize = Mb(MinSizeMb), MaxSize = Mb(MaxSizeMb),
            ModifiedFrom = From, ModifiedTo = To?.AddDays(1), Folder = FolderPart, Content = Content, Tag = Tag,
        };
        var tags = AppServices.Db.GetFileTags();
        var ct = Shell.BeginBusy("Поиск");
        Shell.ProgressText = string.IsNullOrWhiteSpace(Content) ? "Поиск…" : "Поиск по содержимому документов…";
        try
        {
            var found = await Task.Run(() => SearchService.Search(r.Scan.Files, q, tags, ct), ct);
            Fill(found.Select(f => new FileRow(f, tags.TryGetValue(f.FullPath, out var t) ? "Теги: " + string.Join(", ", t) : null)));
            Status = $"Найдено: {Format.Number(found.Count)}" + (found.Count >= q.Limit ? $" (показаны первые {q.Limit})" : "");
        }
        finally
        {
            Shell.EndBusy();
        }
    }

    private void AddTag()
    {
        if (string.IsNullOrWhiteSpace(NewTag)) { AppServices.Info("Введите название тега."); return; }
        if (!RequireChecked()) return;
        foreach (var row in CheckedRows) AppServices.Db.TagFile(row.FullPath, NewTag.Trim());
        Status = $"Тег «{NewTag.Trim()}» присвоен {CheckedRows.Count()} файлам. Расположение файлов не изменилось.";
        ReloadTags();
    }

    private void RemoveTag()
    {
        if (string.IsNullOrWhiteSpace(NewTag) || !RequireChecked()) return;
        foreach (var row in CheckedRows) AppServices.Db.UntagFile(row.FullPath, NewTag.Trim());
        Status = $"Тег «{NewTag.Trim()}» снят.";
    }
}
