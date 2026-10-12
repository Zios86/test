using System.Collections.ObjectModel;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

public sealed class SimilarFolderGroupViewModel : ObservableObject
{
    public SimilarFolderGroupViewModel(SimilarFolderGroup g, string root)
    {
        Group = g;
        Folders = g.Folders.Select(f => new Option<FolderEntry>(f, $"{System.IO.Path.GetRelativePath(root, f.FullPath)}  ({f.TotalFileCount} файлов)")).ToList();
        _target = Folders.First(o => o.Value == g.Target);
    }
    public SimilarFolderGroup Group { get; }
    public List<Option<FolderEntry>> Folders { get; }
    private Option<FolderEntry> _target;
    public Option<FolderEntry> Target { get => _target; set => Set(ref _target, value); }
    public string Header => string.Join(" · ", Group.Folders.Select(f => f.Name));
    public string Conflicts => Group.ConflictingNames.Count == 0
        ? "Конфликтов имён нет."
        : $"Одинаковые имена файлов ({Group.ConflictingNames.Count}): {string.Join(", ", Group.ConflictingNames.Take(8))}{(Group.ConflictingNames.Count > 8 ? "…" : "")}. При объединении будут сохранены оба файла (с номером) — или выберите другое на экране «Будет выполнено».";
    public bool HasConflicts => Group.ConflictingNames.Count > 0;
}

/// <summary>Похожие папки и их объединение (с проверкой конфликтов).</summary>
public sealed class SimilarFoldersViewModel : PageViewModel
{
    public SimilarFoldersViewModel(MainViewModel shell) : base(shell)
    {
        MergeCommand = new RelayCommand(p =>
        {
            if (p is SimilarFolderGroupViewModel g)
                Shell.AddToPlan(b => OperationPlanner.PlanMergeFolders(b, g.Group, g.Target.Value));
        });
        OpenCommand = new RelayCommand(p => { if (p is Option<FolderEntry> o) AppServices.OpenInExplorer(o.Value.FullPath); });
    }

    public override string Title => "Похожие папки";
    public override string Icon => "";
    public ObservableCollection<SimilarFolderGroupViewModel> Groups { get; } = new();
    public ICommand MergeCommand { get; }
    public ICommand OpenCommand { get; }

    public override void OnReportChanged() =>
        Groups.ReplaceWith(Shell.Report?.SimilarFolders.Select(g => new SimilarFolderGroupViewModel(g, Shell.Report.Scan.RootPath)) ?? Enumerable.Empty<SimilarFolderGroupViewModel>());
}
