using System.Collections.ObjectModel;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

/// <summary>«Структура»: текущее дерево, предлагаемое дерево и изменения с цветовыми обозначениями.</summary>
public sealed class StructureViewModel : PageViewModel
{
    private bool _dirty = true;

    public StructureViewModel(MainViewModel shell) : base(shell)
    {
        shell.PlanChanged += (_, _) => { _dirty = true; if (shell.CurrentPage == this) Rebuild(); };
    }

    public override string Title => "Структура";
    public override string Icon => "";

    public ObservableCollection<PreviewNode> Current { get; } = new();
    public ObservableCollection<PreviewNode> Proposed { get; } = new();
    public bool HasPlan => Shell.Builder != null && Shell.Builder.Plan.Operations.Any(o => o.Selected);

    public override void OnReportChanged() { _dirty = true; }

    public override void OnNavigatedTo()
    {
        if (_dirty) Rebuild();
    }

    private void Rebuild()
    {
        _dirty = false;
        Current.Clear();
        Proposed.Clear();
        var r = Shell.Report;
        if (r == null) return;
        var plan = Shell.Builder?.Plan;
        Current.Add(PreviewTreeBuilder.BuildCurrent(r.Scan, plan));
        if (plan != null) Proposed.Add(PreviewTreeBuilder.BuildProposed(r.Scan, plan));
        OnPropertyChanged(nameof(HasPlan));
    }
}
