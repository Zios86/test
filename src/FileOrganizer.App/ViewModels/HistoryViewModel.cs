using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Models;

namespace FileOrganizer.App.ViewModels;

/// <summary>История операций: просмотр, отмена пакета, отмена последнего действия, восстановление на момент времени.</summary>
public sealed class HistoryViewModel : PageViewModel
{
    public HistoryViewModel(MainViewModel shell) : base(shell)
    {
        RollbackCommand = new AsyncCommand(p => RollbackAsync(p as BatchInfo), p => p is BatchInfo b && RollbackManager.CanRollback(b) && !Shell.IsBusy);
        RestoreToCommand = new AsyncCommand(RestoreToAsync, () => !Shell.IsBusy);
        PurgeCommand = new RelayCommand(p => Purge(p as BatchInfo), p => p is BatchInfo);
        OpenRootCommand = new RelayCommand(p => { if (p is BatchInfo b) AppServices.OpenInExplorer(b.RootPath); });
        OpenRecoveryCommand = new RelayCommand(p => { if (p is BatchInfo b) AppServices.OpenInExplorer(b.RecoveryFolder); });
        RestoreDate = DateTime.Today;
        Reload();
    }

    public override string Title => "История";
    public override string Icon => "";

    public ObservableCollection<BatchInfo> Batches { get; } = new();
    public ObservableCollection<OperationRecord> Operations { get; } = new();

    private BatchInfo? _selected;
    public BatchInfo? Selected
    {
        get => _selected;
        set
        {
            if (Set(ref _selected, value))
                Operations.ReplaceWith(value == null ? Enumerable.Empty<OperationRecord>() : AppServices.Db.GetOperations(value.Id));
        }
    }

    public DateTime? RestoreDate { get; set; }
    public string RestoreTime { get; set; } = "00:00";

    public ICommand RollbackCommand { get; }
    public ICommand RestoreToCommand { get; }
    public ICommand PurgeCommand { get; }
    public ICommand OpenRootCommand { get; }
    public ICommand OpenRecoveryCommand { get; }

    public override void OnNavigatedTo() => Reload();

    public void Reload()
    {
        var id = Selected?.Id;
        Batches.ReplaceWith(AppServices.Db.GetBatches());
        Selected = Batches.FirstOrDefault(b => b.Id == id) ?? Batches.FirstOrDefault();
    }

    public async Task UndoLastAsync()
    {
        var last = AppServices.Db.GetBatches().FirstOrDefault(RollbackManager.CanRollback);
        if (last == null) { AppServices.Info("Нет действий, которые можно отменить."); return; }
        await RollbackAsync(last);
    }

    private async Task RollbackAsync(BatchInfo? b)
    {
        if (b == null) return;
        if (!AppServices.Confirm($"Отменить «{b.Title}» от {b.CreatedUtc.ToLocalTime():dd.MM.yyyy HH:mm}?\n\nФайлы вернутся на исходные места ({b.DoneCount} операций). Существующие файлы не перезаписываются.", "Отмена изменений")) return;
        var ct = Shell.BeginBusy("Отмена изменений");
        Shell.ProgressText = $"Откат: {b.Title}";
        RollbackResult r;
        try { r = await new RollbackManager(AppServices.Db, AppServices.Log).RollbackBatchAsync(b.Id, ct); }
        finally { Shell.EndBusy(); }
        Report(r);
        Reload();
        Shell.ResetPlan();
    }

    private async Task RestoreToAsync()
    {
        if (RestoreDate == null || !TimeSpan.TryParse(RestoreTime, CultureInfo.InvariantCulture, out var time)) { AppServices.Info("Укажите дату и время в формате ЧЧ:ММ."); return; }
        var point = (RestoreDate.Value.Date + time).ToUniversalTime();
        var affected = AppServices.Db.GetBatches(int.MaxValue).Where(b => b.CreatedUtc >= point && RollbackManager.CanRollback(b)).ToList();
        if (affected.Count == 0) { AppServices.Info("После указанного момента изменений не было."); return; }
        if (!AppServices.Confirm($"Восстановить состояние до {point.ToLocalTime():dd.MM.yyyy HH:mm}?\n\nБудут отменены {affected.Count} пакет(ов) изменений, начиная с самого нового.", "Восстановление")) return;
        Shell.BeginBusy("Восстановление состояния");
        RollbackResult r;
        try { r = await Task.Run(() => new RollbackManager(AppServices.Db, AppServices.Log).RestoreTo(point)); }
        finally { Shell.EndBusy(); }
        Report(r);
        Reload();
        Shell.ResetPlan();
    }

    private static void Report(RollbackResult r)
    {
        var msg = $"Восстановлено: {r.Restored}\nНе удалось: {r.Failed}";
        if (r.Messages.Count > 0) msg += "\n\n" + string.Join("\n", r.Messages.Take(15)) + (r.Messages.Count > 15 ? "\n…" : "");
        AppServices.Info(msg, "Результат отката");
    }

    /// <summary>«Удалить окончательно» — только отдельной командой и после двух предупреждений.</summary>
    private void Purge(BatchInfo? b)
    {
        if (b == null) return;
        var count = AppServices.Db.GetOperations(b.Id).Count(o => o.Kind == OperationKind.MoveToRecovery && o.Status == OperationStatus.Done);
        if (count == 0) { AppServices.Info("В этом пакете нет файлов в папке восстановления."); return; }
        if (!AppServices.Confirm($"ВНИМАНИЕ: {count} файл(ов) из папки восстановления будут удалены БЕЗВОЗВРАТНО.\n\nПосле этого их нельзя будет вернуть ни программой, ни из Корзины.\n\nПродолжить?",
                "Удалить окончательно", System.Windows.MessageBoxImage.Warning)) return;
        if (!AppServices.Confirm("Вы точно уверены? Это действие нельзя отменить.", "Удалить окончательно", System.Windows.MessageBoxImage.Warning)) return;
        var n = new RollbackManager(AppServices.Db, AppServices.Log).PurgeRecovery(b.Id);
        AppServices.Info($"Удалено окончательно: {n}");
        Reload();
    }
}
