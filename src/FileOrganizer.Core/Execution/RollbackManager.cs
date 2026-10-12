using FileOrganizer.Core.Data;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Execution;

/// <summary>
/// Откат по журналу: операции пакета отменяются в обратном порядке.
/// Файлы возвращаются на исходные места (без перезаписи), созданные пустые папки удаляются, удалённые пустые — создаются заново.
/// </summary>
public sealed class RollbackManager
{
    private readonly Database _db;
    private readonly Logger _log;

    public RollbackManager(Database db, Logger? log = null)
    {
        _db = db;
        _log = log ?? Logger.Null;
    }

    public static bool CanRollback(BatchInfo b) => b.Status is not (BatchStatus.RolledBack or BatchStatus.Running);

    /// <summary>Отменить последнее действие (последний не отменённый пакет).</summary>
    public RollbackResult UndoLast()
    {
        var last = _db.GetBatches().FirstOrDefault(CanRollback);
        if (last == null)
        {
            var r = new RollbackResult();
            r.Messages.Add("Нет действий для отмены.");
            return r;
        }
        return RollbackBatch(last.Id);
    }

    /// <summary>Восстановить состояние до указанного момента: откатываются все пакеты, выполненные после него (от новых к старым).</summary>
    public RollbackResult RestoreTo(DateTime pointUtc, CancellationToken ct = default)
    {
        var total = new RollbackResult();
        foreach (var b in _db.GetBatches(int.MaxValue).Where(b => b.CreatedUtc >= pointUtc && CanRollback(b)).OrderByDescending(b => b.Id))
        {
            if (ct.IsCancellationRequested) { total.Messages.Add("Восстановление прервано пользователем."); break; }
            var r = RollbackBatch(b.Id, ct);
            total.Restored += r.Restored;
            total.Failed += r.Failed;
            total.Messages.AddRange(r.Messages);
        }
        return total;
    }

    public Task<RollbackResult> RollbackBatchAsync(long batchId, CancellationToken ct = default) => Task.Run(() => RollbackBatch(batchId, ct), CancellationToken.None);

    public RollbackResult RollbackBatch(long batchId, CancellationToken ct = default)
    {
        var result = new RollbackResult();
        var batch = _db.GetBatch(batchId) ?? throw new ArgumentException($"Пакет {batchId} не найден");
        var ops = _db.GetOperations(batchId).Where(o => o.Status is OperationStatus.Done or OperationStatus.RollbackFailed or OperationStatus.Purged)
            .OrderByDescending(o => o.Seq).ToList();
        _log.Info("Rollback.Start", batch.RootPath, $"batch={batchId}; ops={ops.Count}");

        foreach (var op in ops)
        {
            if (ct.IsCancellationRequested) { result.Messages.Add("Откат прерван пользователем."); break; }
            try
            {
                if (op.Status == OperationStatus.Purged)
                {
                    result.Failed++;
                    result.Messages.Add($"«{op.OldName}» был удалён окончательно — восстановить нельзя.");
                    continue;
                }
                UndoOne(op, result);
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Messages.Add($"Не удалось отменить «{op.OldName}»: {OperationExecutor.Describe(ex)}");
                _db.UpdateOperation(op.Id, OperationStatus.RollbackFailed, OperationExecutor.Describe(ex));
                _log.Error("Rollback", op.Destination, ex);
            }
        }

        CleanupEmptyRecovery(batch.RecoveryFolder);
        var remaining = _db.GetOperations(batchId).Count(o => o.Status is OperationStatus.Done or OperationStatus.RollbackFailed);
        _db.SetBatchStatus(batchId, remaining == 0 ? BatchStatus.RolledBack : BatchStatus.PartiallyRolledBack);
        _log.Info("Rollback.End", batch.RootPath, $"batch={batchId}; restored={result.Restored}; failed={result.Failed}");
        return result;
    }

    private void UndoOne(OperationRecord op, RollbackResult result)
    {
        switch (op.Kind)
        {
            case OperationKind.CreateFolder:
                if (Directory.Exists(op.Source))
                {
                    if (FileMover.IsDirectoryEmpty(op.Source)) Directory.Delete(op.Source, false);
                    else result.Messages.Add($"Папка «{op.Source}» не удалена: в ней появились другие файлы.");
                }
                _db.UpdateOperation(op.Id, OperationStatus.RolledBack);
                result.Restored++;
                break;

            case OperationKind.DeleteEmptyFolder:
                Directory.CreateDirectory(op.Source);
                _db.UpdateOperation(op.Id, OperationStatus.RolledBack);
                result.Restored++;
                break;

            default:
                if (op.Destination.StartsWith(OperationExecutor.RecycleBinPrefix, StringComparison.Ordinal))
                {
                    result.Failed++;
                    result.Messages.Add($"«{op.OldName}» отправлен в Корзину Windows — восстановите его из Корзины.");
                    _db.UpdateOperation(op.Id, OperationStatus.RollbackFailed, "Файл в Корзине Windows");
                    return;
                }
                if (!File.Exists(op.Destination))
                {
                    if (File.Exists(op.Source))
                    {
                        // Уже на месте (например, повторный откат после сбоя).
                        _db.UpdateOperation(op.Id, OperationStatus.RolledBack);
                        result.Restored++;
                        return;
                    }
                    throw new FileNotFoundException($"Файл не найден по новому пути: {op.Destination}");
                }
                var target = op.Source;
                if (File.Exists(target) || Directory.Exists(target))
                {
                    target = PathUtil.MakeUnique(target);
                    result.Messages.Add($"На исходном месте уже есть файл «{op.OldName}» — восстановлен как «{Path.GetFileName(target)}».");
                }
                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                FileMover.Move(op.Destination, target);
                _db.MoveFileTags(op.Destination, target);
                _db.UpdateOperation(op.Id, OperationStatus.RolledBack);
                _log.Info("Rollback." + op.Kind, $"{op.Destination} -> {target}");
                result.Restored++;
                break;
        }
    }

    /// <summary>
    /// «Удалить окончательно»: безвозвратно удаляет файлы пакета из папки восстановления.
    /// Вызывать только после явного предупреждения пользователя.
    /// </summary>
    public int PurgeRecovery(long batchId)
    {
        var batch = _db.GetBatch(batchId) ?? throw new ArgumentException($"Пакет {batchId} не найден");
        int purged = 0;
        foreach (var op in _db.GetOperations(batchId).Where(o => o.Kind == OperationKind.MoveToRecovery && o.Status == OperationStatus.Done))
        {
            if (op.Destination.StartsWith(OperationExecutor.RecycleBinPrefix, StringComparison.Ordinal)) continue;
            if (!PathUtil.IsUnder(op.Destination, batch.RecoveryFolder) && !op.Destination.Contains(AppPaths.RecoveryFolderName, StringComparison.OrdinalIgnoreCase))
                continue; // удаляем только внутри папки восстановления
            try
            {
                if (File.Exists(op.Destination))
                {
                    var attrs = File.GetAttributes(op.Destination);
                    if ((attrs & FileAttributes.ReadOnly) != 0) File.SetAttributes(op.Destination, attrs & ~FileAttributes.ReadOnly);
                    File.Delete(op.Destination);
                }
                _db.UpdateOperation(op.Id, OperationStatus.Purged);
                _log.Info("Purge", op.Destination);
                purged++;
            }
            catch (Exception ex)
            {
                _log.Error("Purge", op.Destination, ex);
            }
        }
        CleanupEmptyRecovery(batch.RecoveryFolder);
        return purged;
    }

    /// <summary>Удаляет пустые подпапки внутри папки восстановления пакета (включая её саму, если пуста).</summary>
    private static void CleanupEmptyRecovery(string recoveryFolder)
    {
        if (string.IsNullOrEmpty(recoveryFolder) || !Directory.Exists(recoveryFolder)) return;
        if (!recoveryFolder.Contains(AppPaths.RecoveryFolderName, StringComparison.OrdinalIgnoreCase)) return;
        try
        {
            foreach (var d in Directory.EnumerateDirectories(recoveryFolder, "*", SearchOption.AllDirectories).OrderByDescending(p => p.Length))
                if (FileMover.IsDirectoryEmpty(d)) Directory.Delete(d);
            if (FileMover.IsDirectoryEmpty(recoveryFolder)) Directory.Delete(recoveryFolder);
            var parent = Path.GetDirectoryName(recoveryFolder);
            if (parent != null && Path.GetFileName(parent) == AppPaths.RecoveryFolderName && Directory.Exists(parent) && FileMover.IsDirectoryEmpty(parent))
                Directory.Delete(parent);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Не критично.
        }
    }

    /// <summary>
    /// Восстановление после аварийного завершения: для пакетов в статусе Running определяет фактическое состояние
    /// операций Pending по файловой системе и помечает пакет как «Прерван». Возвращает число исправленных записей.
    /// </summary>
    public int RecoverInterrupted()
    {
        int fixedCount = 0;
        foreach (var b in _db.GetBatches(int.MaxValue).Where(b => b.Status == BatchStatus.Running))
        {
            foreach (var op in _db.GetOperations(b.Id).Where(o => o.Status == OperationStatus.Pending))
            {
                OperationStatus actual;
                string? note = "Программа была аварийно завершена во время операции";
                switch (op.Kind)
                {
                    case OperationKind.CreateFolder:
                        actual = Directory.Exists(op.Source) ? OperationStatus.Done : OperationStatus.Failed;
                        break;
                    case OperationKind.DeleteEmptyFolder:
                        actual = Directory.Exists(op.Source) ? OperationStatus.Failed : OperationStatus.Done;
                        break;
                    default:
                        bool src = File.Exists(op.Source), dst = File.Exists(op.Destination);
                        if (dst && !src) actual = OperationStatus.Done;
                        else if (src && !dst) actual = OperationStatus.Failed;
                        else if (src && dst)
                        {
                            // Прерванное копирование между дисками: исходник цел, частичную копию не трогаем.
                            actual = OperationStatus.Failed;
                            note += "; найдены оба файла — проверьте копию вручную";
                        }
                        else { actual = OperationStatus.Failed; note += "; файл не найден ни в исходном, ни в новом месте"; }
                        break;
                }
                _db.UpdateOperation(op.Id, actual, actual == OperationStatus.Done ? null : note);
                fixedCount++;
            }
            _db.SetBatchStatus(b.Id, BatchStatus.Interrupted);
            _log.Warn("Recovery", b.RootPath, $"batch {b.Id} помечен как прерванный");
        }
        return fixedCount;
    }
}
