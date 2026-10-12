using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

public sealed class DuplicateFileRow : FileRow
{
    public DuplicateFileRow(FileEntry f, bool recommended) : base(f, null, recommended) { IsRecommended = recommended; }
    /// <summary>Рекомендуемый экземпляр (оставить). IsChecked = «оставить этот файл».</summary>
    public bool IsRecommended { get; }
}

public sealed class DuplicateGroupViewModel : ObservableObject
{
    public DuplicateGroupViewModel(DuplicateGroup g)
    {
        Group = g;
        Files = new ObservableCollection<DuplicateFileRow>(g.Files.Select(f => new DuplicateFileRow(f, f == g.Recommended)));
    }
    public DuplicateGroup Group { get; }
    public ObservableCollection<DuplicateFileRow> Files { get; }
    public string Header => $"{Format.Size(Group.Size)} × {Group.Files.Count} копии — лишнее: {Format.Size(Group.RedundantBytes)}";
    public string Recommendation => $"Рекомендуется оставить «{Group.Recommended?.Name}»: {Group.RecommendationReason}. Окончательное решение — за вами.";
    public string Name => Group.Files[0].Name;

    private string _status = "";
    public string Status { get => _status; set => Set(ref _status, value); }
}

public sealed class SimilarVersionViewModel
{
    public SimilarVersionViewModel(SimilarVersionGroup g) { Group = g; Files = g.Files.Select(f => new FileRow(f)).ToList(); }
    public SimilarVersionGroup Group { get; }
    public List<FileRow> Files { get; }
    public string Header => $"«{Group.BaseName}» — {Group.Files.Count} версии";
}

/// <summary>Дубликаты: группы точных копий (SHA-256) и похожие версии документов.</summary>
public sealed class DuplicatesViewModel : PageViewModel
{
    public DuplicatesViewModel(MainViewModel shell) : base(shell)
    {
        KeepBothCommand = new RelayCommand(p => Decide(p, DuplicateDecision.KeepAll));
        KeepNewestCommand = new RelayCommand(p => Decide(p, DuplicateDecision.KeepNewest));
        KeepOldestCommand = new RelayCommand(p => Decide(p, DuplicateDecision.KeepOldest));
        KeepCheckedCommand = new RelayCommand(p => Decide(p, DuplicateDecision.KeepSelected));
        MoveToDuplicatesCommand = new RelayCommand(p => Decide(p, DuplicateDecision.MoveCopiesToDuplicatesFolder));
        ApplyRecommendationToAllCommand = new RelayCommand(ApplyAll, () => Groups.Count > 0);
        OpenCommand = new RelayCommand(p => { if (p is FileRow r) AppServices.OpenInExplorer(r.FullPath); });
        CompareCommand = new RelayCommand(p =>
        {
            if (p is SimilarVersionViewModel v && v.Files.Count >= 2)
                new Views.CompareWindow(v.Files[0].FullPath, v.Files[1].FullPath) { Owner = Application.Current.MainWindow }.ShowDialog();
        });
    }

    public override string Title => "Дубликаты";
    public override string Icon => "";

    public ObservableCollection<DuplicateGroupViewModel> Groups { get; } = new();
    public ObservableCollection<SimilarVersionViewModel> Versions { get; } = new();
    public string Summary => Shell.Report == null ? "" :
        $"Найдено групп: {Groups.Count}, лишних копий: {Format.Number(Shell.Report.DuplicateFileCount)} ({Format.Size(Shell.Report.DuplicateBytes)}). " +
        "Точные копии определены по размеру и хешу SHA-256.";

    public ICommand KeepBothCommand { get; }
    public ICommand KeepNewestCommand { get; }
    public ICommand KeepOldestCommand { get; }
    public ICommand KeepCheckedCommand { get; }
    public ICommand MoveToDuplicatesCommand { get; }
    public ICommand ApplyRecommendationToAllCommand { get; }
    public ICommand OpenCommand { get; }
    public ICommand CompareCommand { get; }

    public override void OnReportChanged()
    {
        Groups.ReplaceWith(Shell.Report?.Duplicates.Select(g => new DuplicateGroupViewModel(g)) ?? Enumerable.Empty<DuplicateGroupViewModel>());
        Versions.ReplaceWith(Shell.Report?.SimilarVersions.Select(g => new SimilarVersionViewModel(g)) ?? Enumerable.Empty<SimilarVersionViewModel>());
        OnPropertyChanged(nameof(Summary));
    }

    private void Decide(object? p, DuplicateDecision decision)
    {
        if (p is not DuplicateGroupViewModel g) return;
        var b = Shell.EnsureBuilder();
        // Убираем прежнее решение по этой группе.
        var paths = new HashSet<string>(g.Group.Files.Select(f => f.FullPath), PathUtil.Comparer);
        foreach (var o in b.Plan.Operations.Where(o => o.Group == OperationPlanner.GroupDuplicates && paths.Contains(o.Source)).ToList())
            b.Remove(o);

        if (decision == DuplicateDecision.KeepAll)
        {
            foreach (var f in g.Files) f.IsChecked = true;
            g.Status = "Оставлены все копии.";
            Shell.NotifyPlanChanged();
            return;
        }
        var keep = decision == DuplicateDecision.KeepSelected ? g.Files.Where(f => f.IsChecked).Select(f => f.FullPath).ToList() : null;
        if (decision == DuplicateDecision.KeepSelected && keep!.Count == 0)
        {
            AppServices.Info("Отметьте хотя бы один файл, который нужно оставить. Удалить все копии нельзя.");
            return;
        }
        var before = b.Plan.Operations.Count;
        OperationPlanner.PlanDuplicates(b, g.Group, decision, keep);
        var added = b.Plan.Operations.Skip(before).ToList();
        foreach (var o in added) b.SetSelected(o, true);
        var moved = new HashSet<string>(added.Select(o => o.Source), PathUtil.Comparer);
        foreach (var f in g.Files) f.IsChecked = !moved.Contains(f.FullPath);
        if (added.Count(o => o.Kind != OperationKind.CreateFolder) < g.Group.Files.Count - (keep?.Count ?? 1))
            g.Status = "Часть копий уже участвует в других действиях плана (например, сортировке) — проверьте экран «Будет выполнено».";
        else
            g.Status = decision == DuplicateDecision.MoveCopiesToDuplicatesFolder
                ? "Копии будут перемещены в папку «Дубликаты» (после подтверждения на экране «Будет выполнено»)."
                : "Лишние копии будут перемещены в папку восстановления (после подтверждения).";
        Shell.NotifyPlanChanged();
    }

    private void ApplyAll()
    {
        if (!AppServices.Confirm($"Добавить в план удаление лишних копий во всех {Groups.Count} группах (останутся рекомендуемые экземпляры)?\n\nФайлы будут перемещены в папку восстановления только после подтверждения на экране «Будет выполнено».")) return;
        foreach (var g in Groups) Decide(g, DuplicateDecision.KeepRecommended);
        Shell.Navigate(Shell.Plan);
    }
}
