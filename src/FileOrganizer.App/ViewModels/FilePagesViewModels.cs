using System.Collections.ObjectModel;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

/// <summary>Базовая страница со списком файлов, флажками и действиями «в план».</summary>
public abstract class FileListPageViewModel : PageViewModel
{
    protected FileListPageViewModel(MainViewModel shell) : base(shell)
    {
        OpenCommand = new RelayCommand(p => { if (p is FileRow r) AppServices.OpenInExplorer(r.FullPath); });
        OpenFileCommand = new RelayCommand(p => { if (p is FileRow r) AppServices.OpenFile(r.FullPath); });
        CheckAllCommand = new RelayCommand(() => { foreach (var r in Rows) r.IsChecked = true; RefreshSelection(); });
        UncheckAllCommand = new RelayCommand(() => { foreach (var r in Rows) r.IsChecked = false; RefreshSelection(); });
    }

    public ObservableCollection<FileRow> Rows { get; } = new();
    public ICommand OpenCommand { get; }
    public ICommand OpenFileCommand { get; }
    public ICommand CheckAllCommand { get; }
    public ICommand UncheckAllCommand { get; }

    public IEnumerable<FileRow> CheckedRows => Rows.Where(r => r.IsChecked);
    public string SelectionText => $"Отмечено: {Rows.Count(r => r.IsChecked)} из {Rows.Count} ({Format.Size(Rows.Where(r => r.IsChecked).Sum(r => r.Size))})";
    public void RefreshSelection() => OnPropertyChanged(nameof(SelectionText));

    protected void Fill(IEnumerable<FileRow> rows)
    {
        foreach (var r in Rows) r.PropertyChanged -= RowChanged;
        Rows.ReplaceWith(rows);
        foreach (var r in Rows) r.PropertyChanged += RowChanged;
        RefreshSelection();
    }

    private void RowChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FileRow.IsChecked)) RefreshSelection();
    }

    protected bool RequireChecked()
    {
        if (CheckedRows.Any()) return true;
        AppServices.Info("Отметьте файлы флажками.");
        return false;
    }
}

/// <summary>«Что занимает место»: топ-100, корзины по размеру, распределение по типам.</summary>
public sealed class SpaceViewModel : FileListPageViewModel
{
    public SpaceViewModel(MainViewModel shell) : base(shell)
    {
        ToRecoveryCommand = new RelayCommand(() =>
        {
            if (!RequireChecked()) return;
            Shell.AddToPlan(b => { foreach (var r in CheckedRows) b.AddMoveToRecovery(r.File, "Крупный файл — отмечен вами для удаления", "Что занимает место"); });
        });
        ArchiveCommand = new RelayCommand(() =>
        {
            if (!RequireChecked()) return;
            Shell.AddToPlan(b => OperationPlanner.PlanArchiveOld(b, CheckedRows.Select(r => r.File)));
        });
    }

    public override string Title => "Что занимает место";
    public override string Icon => "";

    public ObservableCollection<SizeBucket> Buckets { get; } = new();
    public ObservableCollection<CategoryBar> Categories { get; } = new();
    public ObservableCollection<CategoryBar> Folders { get; } = new();
    public ICommand ToRecoveryCommand { get; }
    public ICommand ArchiveCommand { get; }

    public override void OnReportChanged()
    {
        var r = Shell.Report;
        Buckets.ReplaceWith(r?.SizeBuckets ?? new List<SizeBucket>());
        Fill(r?.LargeFiles.Select(f => new FileRow(f, f.Category.DisplayName())) ?? Enumerable.Empty<FileRow>());
        Categories.Clear();
        Folders.Clear();
        if (r == null) return;
        long max = r.CategoryUsage.Count > 0 ? Math.Max(1, r.CategoryUsage.Max(c => c.TotalBytes)) : 1;
        foreach (var c in r.CategoryUsage) Categories.Add(new CategoryBar(c.Category.DisplayName(), Format.Size(c.TotalBytes), c.Count, (double)c.TotalBytes / max));
        var folders = SpaceAnalyzer.LargestFolders(r.Scan, 15);
        long fmax = folders.Count > 0 ? Math.Max(1, folders.Max(f => f.TotalSize)) : 1;
        foreach (var f in folders) Folders.Add(new CategoryBar(f.Name, Format.Size(f.TotalSize), f.TotalFileCount, (double)f.TotalSize / fmax));
    }
}

/// <summary>«Давно не использовались»: фильтр по возрасту; старый файл не считается мусором.</summary>
public sealed class OldFilesViewModel : FileListPageViewModel
{
    public OldFilesViewModel(MainViewModel shell) : base(shell)
    {
        Presets = OldFileFinder.Presets.Select(p => new Option<int>(p.Days, p.Label)).Append(new Option<int>(-1, "Свой период…")).ToList();
        _selectedPreset = Presets[3];
        ArchiveCommand = new RelayCommand(() =>
        {
            if (!RequireChecked()) return;
            Shell.AddToPlan(b => OperationPlanner.PlanArchiveOld(b, CheckedRows.Select(r => r.File), ArchiveFolder));
        });
        ApplyFilterCommand = new RelayCommand(Refresh);
    }

    public override string Title => "Давно не использовались";
    public override string Icon => "";

    public List<Option<int>> Presets { get; }
    private Option<int> _selectedPreset;
    public Option<int> SelectedPreset
    {
        get => _selectedPreset;
        set { if (Set(ref _selectedPreset, value)) { OnPropertyChanged(nameof(IsCustom)); if (value.Value > 0) Refresh(); } }
    }
    public bool IsCustom => SelectedPreset.Value < 0;
    private string _customDays = "90";
    public string CustomDays { get => _customDays; set => Set(ref _customDays, value); }
    private string _archiveFolder = "99_Архив";
    public string ArchiveFolder { get => _archiveFolder; set => Set(ref _archiveFolder, value); }
    private string _summary = "";
    public string Summary { get => _summary; set => Set(ref _summary, value); }
    public ICommand ArchiveCommand { get; }
    public ICommand ApplyFilterCommand { get; }

    public override void OnReportChanged() => Refresh();

    private void Refresh()
    {
        var r = Shell.Report;
        if (r == null) { Fill(Enumerable.Empty<FileRow>()); Summary = ""; return; }
        int days = SelectedPreset.Value > 0 ? SelectedPreset.Value : int.TryParse(CustomDays, out var d) && d > 0 ? d : 90;
        var list = OldFileFinder.Find(r.Scan.Files, TimeSpan.FromDays(days));
        Fill(list.Take(5000).Select(f => new FileRow(f, $"не изменялся с {f.LastWriteTimeUtc.ToLocalTime():dd.MM.yyyy}")));
        Summary = $"{Format.Number(list.Count)} файлов, {Format.Size(list.Sum(f => f.Size))}." + (list.Count > 5000 ? " Показаны первые 5 000." : "");
    }
}

/// <summary>Пустые папки (в т. ч. только с системными файлами и цепочки).</summary>
public sealed class EmptyFoldersViewModel : PageViewModel
{
    public EmptyFoldersViewModel(MainViewModel shell) : base(shell)
    {
        AddCommand = new RelayCommand(() =>
        {
            var chosen = Items.Where(i => i.IsChecked).Select(i => i.Info).ToList();
            if (chosen.Count == 0) { AppServices.Info("Отметьте папки флажками."); return; }
            Shell.AddToPlan(b => OperationPlanner.PlanEmptyFolders(b, chosen));
        });
        OpenCommand = new RelayCommand(p => { if (p is EmptyFolderRow r) AppServices.OpenInExplorer(r.Info.Path); });
    }

    public override string Title => "Пустые папки";
    public override string Icon => "";
    public ObservableCollection<EmptyFolderRow> Items { get; } = new();
    public ICommand AddCommand { get; }
    public ICommand OpenCommand { get; }

    public override void OnReportChanged() =>
        Items.ReplaceWith(Shell.Report?.EmptyFolders.Select(e => new EmptyFolderRow(e, Shell.Report.Scan.RootPath)) ?? Enumerable.Empty<EmptyFolderRow>());
}

public sealed class EmptyFolderRow : ObservableObject
{
    public EmptyFolderRow(EmptyFolderInfo info, string root)
    {
        Info = info;
        RelativePath = System.IO.Path.GetRelativePath(root, info.Path);
        Note = info.OnlySystemFiles ? $"только служебные файлы ({string.Join(", ", info.JunkFiles.Select(System.IO.Path.GetFileName).Distinct())})"
            : info.NestedEmptyCount > 0 ? $"цепочка из {info.NestedEmptyCount + 1} пустых папок" : "пустая";
    }
    public EmptyFolderInfo Info { get; }
    public string RelativePath { get; }
    public string Note { get; }
    private bool _isChecked = true;
    public bool IsChecked { get => _isChecked; set => Set(ref _isChecked, value); }
}

/// <summary>«Возможно лежат не там».</summary>
public sealed class MisplacedViewModel : PageViewModel
{
    public MisplacedViewModel(MainViewModel shell) : base(shell)
    {
        AddCommand = new RelayCommand(() =>
        {
            var chosen = Items.Where(i => i.IsChecked).Select(i => i.Info).ToList();
            if (chosen.Count == 0) { AppServices.Info("Отметьте файлы флажками."); return; }
            Shell.AddToPlan(b => OperationPlanner.PlanMisplaced(b, chosen));
        });
        OpenCommand = new RelayCommand(p => { if (p is MisplacedRow r) AppServices.OpenInExplorer(r.Info.File.FullPath); });
    }

    public override string Title => "Возможно лежат не там";
    public override string Icon => "";
    public ObservableCollection<MisplacedRow> Items { get; } = new();
    public ICommand AddCommand { get; }
    public ICommand OpenCommand { get; }

    public override void OnReportChanged() =>
        Items.ReplaceWith(Shell.Report?.Misplaced.Select(m => new MisplacedRow(m)) ?? Enumerable.Empty<MisplacedRow>());
}

public sealed class MisplacedRow : ObservableObject
{
    public MisplacedRow(MisplacedFileInfo info) { Info = info; }
    public MisplacedFileInfo Info { get; }
    public string Name => Info.File.Name;
    public string Folder => Info.File.DirectoryPath;
    public string Reason => Info.Reason;
    public string Suggested => Info.SuggestedFolder ?? "";
    public double Confidence => Info.Confidence;
    private bool _isChecked;
    public bool IsChecked { get => _isChecked; set => Set(ref _isChecked, value); }
}

/// <summary>Временные и остаточные файлы — никогда не удаляются автоматически.</summary>
public sealed class TempFilesViewModel : FileListPageViewModel
{
    public TempFilesViewModel(MainViewModel shell) : base(shell)
    {
        AddCommand = new RelayCommand(() =>
        {
            if (!RequireChecked()) return;
            var chosen = new HashSet<FileEntry>(CheckedRows.Select(r => r.File));
            Shell.AddToPlan(b => OperationPlanner.PlanTempFiles(b, Shell.Report!.TempFiles.Where(t => chosen.Contains(t.File))));
        });
    }

    public override string Title => "Временные файлы";
    public override string Icon => "";

    public override void OnReportChanged() =>
        Fill(Shell.Report?.TempFiles.Select(t => new FileRow(t.File, t.Reason)) ?? Enumerable.Empty<FileRow>());

    public ICommand AddCommand { get; }
}

/// <summary>Проектные группы: документы, таблицы, презентации одного проекта.</summary>
public sealed class ProjectsViewModel : PageViewModel
{
    public ProjectsViewModel(MainViewModel shell) : base(shell)
    {
        GroupCommand = new RelayCommand(p =>
        {
            if (p is not ProjectRow row) return;
            var root = Shell.Report!.Scan.RootPath;
            var folder = System.IO.Path.Combine(root, "Проекты", PathUtil.SanitizeFileName("Проект_" + row.Name));
            Shell.AddToPlan(b =>
            {
                foreach (var f in row.Group.Files)
                    b.AddMove(f, System.IO.Path.Combine(folder, ProjectDetector.SubfolderFor(f.Category)), $"Проектная группа «{row.Name}»: {row.Group.Reason}", 0.9, OperationPlanner.GroupProjects);
            });
        });
    }

    public override string Title => "Проекты";
    public override string Icon => "";
    public ObservableCollection<ProjectRow> Items { get; } = new();
    public ICommand GroupCommand { get; }

    public override void OnReportChanged() =>
        Items.ReplaceWith(Shell.Report?.Projects.Select(p => new ProjectRow(p)) ?? Enumerable.Empty<ProjectRow>());
}

public sealed class ProjectRow
{
    public ProjectRow(ProjectGroup g) { Group = g; Files = g.Files.Select(f => new FileRow(f, ProjectDetector.SubfolderFor(f.Category))).ToList(); }
    public ProjectGroup Group { get; }
    public string Name => Group.Name;
    public string Reason => Group.Reason;
    public List<FileRow> Files { get; }
    public string Target => $"Проекты/Проект_{Group.Name}/{{Документы, Данные, Презентации, Материалы, Архив}}";
}
