using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.ViewModels;

/// <summary>Строка плана: флажок, что/куда, причина, уверенность, разрешение конфликта.</summary>
public sealed class PlanItemViewModel : ObservableObject
{
    private readonly PlanViewModel _owner;
    public PlanItemViewModel(PlannedOperation op, PlanViewModel owner) { Op = op; _owner = owner; }

    public PlannedOperation Op { get; }
    public OperationKind Kind => Op.Kind;
    public string Group => Op.Group;
    public string Reason => Op.Reason;
    public double Confidence => Op.Confidence;
    public ConfidenceLevel Level => Op.Level;
    public string ConfidenceText => Op.Level switch
    {
        ConfidenceLevel.High => $"Высокая уверенность — {Format.Percent(Op.Confidence)}",
        ConfidenceLevel.Medium => $"Средняя — {Format.Percent(Op.Confidence)}",
        _ => $"Низкая — {Format.Percent(Op.Confidence)}, нужен ваш выбор",
    };
    public bool HasConflict => Op.HasConflict;
    public string SourceName => System.IO.Path.GetFileName(Op.Source);
    public string Source => Rel(Op.Source);
    public string Destination => Op.Kind switch
    {
        OperationKind.DeleteEmptyFolder => "будет удалена",
        OperationKind.CreateFolder => "будет создана",
        _ => Rel(Op.Destination),
    };
    public string SizeText => Op.Size > 0 ? Format.Size(Op.Size) : "";

    private string Rel(string p) => PathUtil.IsUnder(p, _owner.Root) ? System.IO.Path.GetRelativePath(_owner.Root, p) : p;

    public bool IsSelected
    {
        get => Op.Selected;
        set
        {
            if (Op.Selected == value) return;
            _owner.SetSelected(this, value);
        }
    }

    public static IReadOnlyList<Option<ConflictResolution>> Resolutions { get; } = new[]
    {
        new Option<ConflictResolution>(ConflictResolution.KeepBoth, "Оставить оба (добавить номер)"),
        new Option<ConflictResolution>(ConflictResolution.Skip, "Пропустить"),
        new Option<ConflictResolution>(ConflictResolution.Replace, "Заменить (старый — в папку восстановления)"),
    };

    public ConflictResolution Resolution
    {
        get => Op.Resolution;
        set
        {
            if (Op.Resolution == value) return;
            _owner.SetResolution(this, value);
        }
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(IsSelected));
        OnPropertyChanged(nameof(Destination));
        OnPropertyChanged(nameof(Resolution));
    }
}

/// <summary>Экран «Будет выполнено»: сводка, раскрываемые разделы, флажки, конфликты, применение.</summary>
public sealed class PlanViewModel : PageViewModel
{
    public PlanViewModel(MainViewModel shell) : base(shell)
    {
        shell.PlanChanged += (_, _) => Reload();
        ApplyCommand = new AsyncCommand(ApplyAsync, () => Items.Any(i => i.IsSelected) && !Shell.IsBusy);
        ClearCommand = new RelayCommand(() => { if (AppServices.Confirm("Очистить план? Файлы не изменятся.")) Shell.ResetPlan(); }, () => Items.Count > 0);
        SelectAllCommand = new RelayCommand(p => SetGroup(p as string, true));
        SelectNoneCommand = new RelayCommand(p => SetGroup(p as string, false));
        SortByTypeCommand = new RelayCommand(() => { Shell.BuildSortPlan(SortStrategy.ByType); }, () => Shell.HasReport);
        SmartSortCommand = new RelayCommand(() => { Shell.BuildSortPlan(SortStrategy.Smart); }, () => Shell.HasReport);
        RulesSortCommand = new RelayCommand(() => { Shell.BuildSortPlan(SortStrategy.RulesOnly); }, () => Shell.HasReport);
        ShowStructureCommand = new RelayCommand(() => Shell.Navigate(Shell.Structure));
        CompareCommand = new RelayCommand(p => { if (p is PlanItemViewModel i) Compare(i); });
        OpenCommand = new RelayCommand(p => { if (p is PlanItemViewModel i) AppServices.OpenInExplorer(i.Op.Source); });
        View = CollectionViewSource.GetDefaultView(Items);
        View.GroupDescriptions.Add(new PropertyGroupDescription(nameof(PlanItemViewModel.Group)));
    }

    public override string Title => "Будет выполнено";
    public override string Icon => "";
    public string Root => Shell.Builder?.Plan.RootPath ?? Shell.Folder;

    public ObservableCollection<PlanItemViewModel> Items { get; } = new();
    public ICollectionView View { get; }

    private PlanSummary _summary = new(0, 0, 0, 0, 0, 0, 0, 0);
    public PlanSummary Summary { get => _summary; private set => Set(ref _summary, value); }
    public bool IsEmpty => Items.Count == 0;

    public ICommand ApplyCommand { get; }
    public ICommand ClearCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand SortByTypeCommand { get; }
    public ICommand SmartSortCommand { get; }
    public ICommand RulesSortCommand { get; }
    public ICommand ShowStructureCommand { get; }
    public ICommand CompareCommand { get; }
    public ICommand OpenCommand { get; }

    private void Reload()
    {
        Items.Clear();
        if (Shell.Builder != null)
        {
            // Папки показываются вместе с действиями: пользователь видит и может отключить «Создать папку».
            foreach (var op in Shell.Builder.Plan.Operations) Items.Add(new PlanItemViewModel(op, this));
        }
        UpdateSummary();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(Root));
    }

    private void UpdateSummary()
    {
        Summary = Shell.Builder?.Plan.Summary() ?? new PlanSummary(0, 0, 0, 0, 0, 0, 0, 0);
        Shell.NotifyPlanCountChanged();
    }

    internal void SetSelected(PlanItemViewModel item, bool value)
    {
        Shell.Builder?.SetSelected(item.Op, value);
        foreach (var i in Items) i.Refresh();
        UpdateSummary();
    }

    internal void SetResolution(PlanItemViewModel item, ConflictResolution r)
    {
        Shell.Builder?.SetResolution(item.Op, r);
        item.Refresh();
        UpdateSummary();
    }

    private void SetGroup(string? group, bool value)
    {
        if (Shell.Builder == null) return;
        foreach (var i in Items.Where(i => group == null || i.Group == group))
            Shell.Builder.SetSelected(i.Op, value);
        foreach (var i in Items) i.Refresh();
        UpdateSummary();
    }

    private void Compare(PlanItemViewModel item)
    {
        var existing = item.Op.OriginalDestination ?? item.Op.Destination;
        if (!System.IO.File.Exists(existing)) { AppServices.Info("Файла с таким именем больше нет — конфликт исчез."); return; }
        new Views.CompareWindow(item.Op.Source, existing) { Owner = Application.Current.MainWindow }.ShowDialog();
    }

    private async Task ApplyAsync()
    {
        var b = Shell.Builder;
        if (b == null) return;
        b.PruneUnusedFolders();
        UpdateSummary();
        var s = b.Plan.Summary();
        if (AppServices.Settings.SimulationMode)
        {
            AppServices.Info("Сейчас включён режим «Только посмотреть» — программа не изменяет файлы.\n\n" +
                             "Чтобы применить план, отключите этот режим (переключатель вверху окна или в «Настройках»).", "Режим «Только посмотреть»");
            return;
        }
        var deleteNote = AppServices.Settings.DeleteMode == Core.Data.DeleteMode.RecycleBin
            ? "Файлы «на удаление» будут отправлены в Корзину Windows."
            : "Файлы «на удаление» будут перемещены в папку FileOrganizer_Recovery — их можно вернуть.";
        var text =
            $"Будет выполнено:\n\n" +
            $"  Перемещено: {s.Moved}\n  Переименовано: {s.Renamed}\n  Создано папок: {s.FoldersCreated}\n" +
            $"  Удалено пустых каталогов: {s.EmptyFoldersRemoved}\n  Отправлено в папку восстановления: {s.SentToRecovery}\n" +
            (s.Conflicts > 0 ? $"  Конфликтов имён (разрешены по вашему выбору): {s.Conflicts}\n" : "") +
            $"\n{deleteNote}\nСуществующие файлы не перезаписываются. Все действия можно отменить в «Истории».\n\nПрименить?";
        if (!AppServices.Confirm(text, "Подтверждение изменений")) return;

        var result = await Shell.ExecutePlanAsync(b.Plan);
        if (result == null) return;
        var msg = $"Готово.\n\nУспешно: {result.Succeeded}\nПропущено: {result.Skipped}\nОшибок: {result.Failed}" +
                  (result.Cancelled ? "\n\nВыполнение было остановлено — уже сделанные изменения записаны в журнал и могут быть отменены." : "");
        if (result.Errors.Count > 0)
            msg += "\n\nОшибки:\n" + string.Join("\n", result.Errors.Take(10).Select(e => $"• {System.IO.Path.GetFileName(e.Op.Source)}: {e.Error}")) +
                   (result.Errors.Count > 10 ? $"\n… и ещё {result.Errors.Count - 10} (см. «История»)" : "");
        AppServices.Info(msg, "Результат");
        await Shell.AfterExecutionAsync();
    }
}
