using System.Collections.ObjectModel;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

public sealed class RenameRow : ObservableObject
{
    public RenameRow(FileEntry file, string problem, string suggested, bool isChecked)
    {
        File = file;
        Problem = problem;
        _newName = suggested;
        _isChecked = isChecked;
    }
    public FileEntry File { get; }
    public string OldName => File.Name;
    public string Folder => File.DirectoryPath;
    public string Problem { get; }
    private string _newName;
    /// <summary>Предложенное имя можно отредактировать вручную.</summary>
    public string NewName { get => _newName; set => Set(ref _newName, value); }
    private bool _isChecked;
    public bool IsChecked { get => _isChecked; set => Set(ref _isChecked, value); }
}

/// <summary>Анализ названий и массовое переименование по шаблону (с предпросмотром «Было → Станет»).</summary>
public sealed class NamesViewModel : PageViewModel
{
    public NamesViewModel(MainViewModel shell) : base(shell)
    {
        AddBadNamesCommand = new RelayCommand(() => AddRenames(BadNames));
        PreviewCommand = new RelayCommand(BuildPreview, () => Shell.HasReport);
        AddPreviewCommand = new RelayCommand(() => AddRenames(Preview));
        OpenCommand = new RelayCommand(p => { if (p is RenameRow r) AppServices.OpenInExplorer(r.File.FullPath); });
        Templates = RenameEngine.Presets.Select(p => new Option<string>(p.Template, $"{p.Title}   ({p.Template})")).ToList();
        _selectedTemplate = Templates[0];
        _template = Templates[0].Value;
    }

    public override string Title => "Названия";
    public override string Icon => "";

    public ObservableCollection<RenameRow> BadNames { get; } = new();
    public ObservableCollection<RenameRow> Preview { get; } = new();
    public ObservableCollection<string> Folders { get; } = new();

    public List<Option<string>> Templates { get; }
    private Option<string> _selectedTemplate;
    public Option<string> SelectedTemplate { get => _selectedTemplate; set { if (Set(ref _selectedTemplate, value)) Template = value.Value; } }
    private string _template;
    public string Template { get => _template; set => Set(ref _template, value); }
    private string? _folder;
    public string? Folder { get => _folder; set => Set(ref _folder, value); }
    private string _extensionFilter = "";
    public string ExtensionFilter { get => _extensionFilter; set => Set(ref _extensionFilter, value); }
    private string _project = "";
    public string Project { get => _project; set => Set(ref _project, value); }
    private string _client = "";
    public string Client { get => _client; set => Set(ref _client, value); }
    private string _docType = "";
    public string DocType { get => _docType; set => Set(ref _docType, value); }
    private string _find = "";
    public string Find { get => _find; set => Set(ref _find, value); }
    private string _replace = "";
    public string Replace { get => _replace; set => Set(ref _replace, value); }

    public ICommand AddBadNamesCommand { get; }
    public ICommand PreviewCommand { get; }
    public ICommand AddPreviewCommand { get; }
    public ICommand OpenCommand { get; }

    public override void OnReportChanged()
    {
        var r = Shell.Report;
        BadNames.ReplaceWith(r?.BadNames.Select(b => new RenameRow(b.File, b.Problem, b.SuggestedName, false)) ?? Enumerable.Empty<RenameRow>());
        Folders.ReplaceWith(r?.Scan.Folders.Where(f => f.Files.Count > 0).Select(f => f.FullPath) ?? Enumerable.Empty<string>());
        Folder = Folders.FirstOrDefault();
        Preview.Clear();
    }

    private void BuildPreview()
    {
        var r = Shell.Report;
        if (r == null || Folder == null) return;
        var folder = r.Scan.FindFolder(Folder);
        if (folder == null) return;
        var exts = ExtensionFilter.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim('.', '*').ToLowerInvariant()).ToHashSet();
        var files = folder.Files.Where(f => exts.Count == 0 || exts.Contains(f.Extension)).OrderBy(f => f.LastWriteTimeUtc).ThenBy(f => f.Name).ToList();
        var engine = new RenameEngine
        {
            Template = Template, Project = Project, Client = Client, DocType = DocType,
            Find = string.IsNullOrEmpty(Find) ? null : Find, Replace = Replace,
        };
        Preview.ReplaceWith(engine.Preview(files).Select(p => new RenameRow(p.File, p.Problem ?? (p.Changed ? "" : "без изменений"), p.NewName, p.Changed)));
    }

    private void AddRenames(IEnumerable<RenameRow> rows)
    {
        var chosen = rows.Where(r => r.IsChecked && !string.Equals(r.NewName, r.OldName, StringComparison.Ordinal)).ToList();
        if (chosen.Count == 0) { AppServices.Info("Отметьте файлы, которые нужно переименовать."); return; }
        Shell.AddToPlan(b => OperationPlanner.PlanRenames(b, chosen.Select(r => (r.File, PathUtil.SanitizeFileName(r.NewName),
            string.IsNullOrEmpty(r.Problem) ? $"Переименование по шаблону: {r.OldName} → {r.NewName}" : $"{r.Problem}: {r.OldName} → {r.NewName}"))));
    }
}
