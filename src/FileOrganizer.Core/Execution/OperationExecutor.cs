using FileOrganizer.Core.Data;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Execution;

/// <summary>
/// Выполняет подтверждённый пользователем план.
/// Гарантии: ни один файл не перезаписывается; каждая операция журналируется ДО выполнения (Pending)
/// и после (Done/Failed) — после сбоя состояние восстанавливается; ошибка одного файла не останавливает остальные;
/// системные пути не изменяются; в режиме «Только посмотреть» выполнение запрещено.
/// </summary>
public sealed class OperationExecutor
{
    public const string RecycleBinPrefix = "recycle-bin://";

    private readonly Database _db;
    private readonly Logger _log;
    private readonly IRecycleBin? _recycleBin;

    public bool SimulationMode { get; set; } = true;
    public DeleteMode DeleteMode { get; set; } = DeleteMode.RecoveryFolder;

    public OperationExecutor(Database db, Logger? log = null, IRecycleBin? recycleBin = null)
    {
        _db = db;
        _log = log ?? Logger.Null;
        _recycleBin = recycleBin;
    }

    public Task<ExecutionResult> ExecuteAsync(OperationPlan plan, IProgress<ExecutionProgress>? progress = null, CancellationToken ct = default)
        => Task.Run(() => Execute(plan, progress, ct), CancellationToken.None);

    public ExecutionResult Execute(OperationPlan plan, IProgress<ExecutionProgress>? progress = null, CancellationToken ct = default)
    {
        if (SimulationMode)
            throw new InvalidOperationException("Включён режим «Только посмотреть»: изменения файловой системы запрещены. Отключите его в настройках, чтобы применить план.");

        var selected = plan.SelectedOperations.ToList();
        var ordered = selected.Where(o => o.Kind == OperationKind.CreateFolder).OrderBy(o => o.Source.Length)
            .Concat(selected.Where(o => o.Kind is OperationKind.Move or OperationKind.Rename or OperationKind.MoveToRecovery))
            .Concat(selected.Where(o => o.Kind == OperationKind.DeleteEmptyFolder).OrderByDescending(o => o.Source.Length))
            .ToList();

        var title = string.IsNullOrWhiteSpace(plan.Title) ? $"Изменения в {plan.RootPath}" : plan.Title;
        var batchId = _db.CreateBatch(title, plan.RootPath, plan.RecoveryFolder, ordered.Count);
        var result = new ExecutionResult { BatchId = batchId };
        _log.Info("Batch.Start", plan.RootPath, $"batch={batchId}; ops={ordered.Count}");
        int seq = 0, done = 0;

        foreach (var op in ordered)
        {
            if (ct.IsCancellationRequested) { result.Cancelled = true; break; }
            progress?.Report(new ExecutionProgress(done, ordered.Count, op.Source, result.Failed));
            try
            {
                var status = ExecuteOne(batchId, ref seq, op, plan);
                if (status == OperationStatus.Done) result.Succeeded++;
                else if (status == OperationStatus.Skipped) result.Skipped++;
            }
            catch (Exception ex)
            {
                result.Failed++;
                result.Errors.Add((op, Describe(ex)));
                _log.Error(op.Kind.ToString(), op.Source, ex);
            }
            done++;
        }

        progress?.Report(new ExecutionProgress(done, ordered.Count, "", result.Failed));
        var final = result.Cancelled ? BatchStatus.Cancelled : result.Failed > 0 ? BatchStatus.CompletedWithErrors : BatchStatus.Completed;
        _db.SetBatchStatus(batchId, final);
        _log.Info("Batch.End", plan.RootPath, $"batch={batchId}; ok={result.Succeeded}; failed={result.Failed}; skipped={result.Skipped}; cancelled={result.Cancelled}");
        return result;
    }

    private OperationStatus ExecuteOne(long batchId, ref int seq, PlannedOperation op, OperationPlan plan)
    {
        switch (op.Kind)
        {
            case OperationKind.CreateFolder:
            {
                if (Directory.Exists(op.Source)) return OperationStatus.Skipped;
                GuardDestination(op.Source, plan);
                CreateFolderJournaled(batchId, ref seq, op.Source, op.Reason);
                return OperationStatus.Done;
            }
            case OperationKind.DeleteEmptyFolder:
            {
                if (!Directory.Exists(op.Source)) return OperationStatus.Skipped;
                if (!SystemPathGuard.IsSafeToModify(op.Source) || PathUtil.SamePath(op.Source, plan.RootPath))
                    throw new UnauthorizedAccessException("Защищённая папка — удаление запрещено");
                if (!FileMover.IsDirectoryEmpty(op.Source))
                {
                    Journal(batchId, ref seq, op.Kind, op.Source, op.Source, op.Reason, 0, OperationStatus.Skipped, "Папка больше не пуста — пропущено");
                    return OperationStatus.Skipped;
                }
                var rec = Journal(batchId, ref seq, op.Kind, op.Source, op.Source, op.Reason, 0, OperationStatus.Pending);
                try
                {
                    Directory.Delete(op.Source, recursive: false);
                    _db.UpdateOperation(rec.Id, OperationStatus.Done);
                    _log.Info("DeleteEmptyFolder", op.Source);
                }
                catch (Exception ex)
                {
                    _db.UpdateOperation(rec.Id, OperationStatus.Failed, Describe(ex));
                    throw;
                }
                return OperationStatus.Done;
            }
            default:
                return ExecuteFileOp(batchId, ref seq, op, plan);
        }
    }

    private OperationStatus ExecuteFileOp(long batchId, ref int seq, PlannedOperation op, OperationPlan plan)
    {
        if (!File.Exists(op.Source))
            throw new FileNotFoundException("Файл не найден — возможно, он был перемещён или удалён после анализа", op.Source);
        if (!SystemPathGuard.IsSafeToModify(op.Source))
            throw new UnauthorizedAccessException("Системный или защищённый файл — изменение запрещено");

        // Удаление в Корзину Windows (если выбрано в настройках и доступно).
        if (op.Kind == OperationKind.MoveToRecovery && DeleteMode == DeleteMode.RecycleBin && _recycleBin is { IsSupported: true })
        {
            var r = Journal(batchId, ref seq, op.Kind, op.Source, RecycleBinPrefix + op.Source, op.Reason, op.Size, OperationStatus.Pending);
            try
            {
                _recycleBin.SendToRecycleBin(op.Source);
                _db.UpdateOperation(r.Id, OperationStatus.Done);
                _log.Info("RecycleBin", op.Source);
                return OperationStatus.Done;
            }
            catch (Exception ex)
            {
                _db.UpdateOperation(r.Id, OperationStatus.Failed, Describe(ex));
                throw;
            }
        }

        var dest = op.Destination;
        GuardDestination(dest, plan);

        if (File.Exists(dest) || Directory.Exists(dest))
        {
            if (PathUtil.SamePath(dest, op.Source) && !string.Equals(dest, op.Source, StringComparison.Ordinal))
            {
                // Переименование с изменением только регистра букв — это тот же файл.
            }
            else
            {
                switch (op.Resolution)
                {
                    case ConflictResolution.Skip:
                        Journal(batchId, ref seq, op.Kind, op.Source, dest, op.Reason, op.Size, OperationStatus.Skipped, "Файл с таким именем уже существует — пропущено");
                        return OperationStatus.Skipped;
                    case ConflictResolution.Replace when File.Exists(dest):
                        // Существующий файл не перезаписывается, а уходит в папку восстановления (обратимо).
                        var rel = PathUtil.IsUnder(dest, plan.RootPath) ? Path.GetRelativePath(plan.RootPath, dest) : Path.GetFileName(dest);
                        var backup = PathUtil.MakeUnique(Path.Combine(plan.RecoveryFolder, "Заменённые", rel));
                        MoveJournaled(batchId, ref seq, OperationKind.MoveToRecovery, dest, backup, "Заменён другим файлом (конфликт имён)", new FileInfo(dest).Length);
                        break;
                    default:
                        dest = PathUtil.MakeUnique(dest);
                        break;
                }
            }
        }

        MoveJournaled(batchId, ref seq, op.Kind, op.Source, dest, op.Reason, op.Size);
        return OperationStatus.Done;
    }

    private void MoveJournaled(long batchId, ref int seq, OperationKind kind, string source, string dest, string reason, long size)
    {
        var dir = Path.GetDirectoryName(dest)!;
        EnsureDirectoryJournaled(batchId, ref seq, dir);
        var rec = Journal(batchId, ref seq, kind, source, dest, reason, size, OperationStatus.Pending);
        try
        {
            FileMover.Move(source, dest);
            _db.UpdateOperation(rec.Id, OperationStatus.Done);
            _db.MoveFileTags(source, dest);
            _log.Info(kind.ToString(), $"{source} -> {dest}");
        }
        catch (Exception ex)
        {
            _db.UpdateOperation(rec.Id, OperationStatus.Failed, Describe(ex));
            throw;
        }
    }

    /// <summary>Создаёт недостающие папки, записывая каждую в журнал (при откате пустые папки будут удалены).</summary>
    private void EnsureDirectoryJournaled(long batchId, ref int seq, string dir)
    {
        var missing = new Stack<string>();
        var cur = dir;
        while (!string.IsNullOrEmpty(cur) && !Directory.Exists(cur))
        {
            missing.Push(cur);
            cur = Path.GetDirectoryName(cur);
        }
        while (missing.Count > 0) CreateFolderJournaled(batchId, ref seq, missing.Pop(), "Папка для размещения файлов");
    }

    private void CreateFolderJournaled(long batchId, ref int seq, string path, string reason)
    {
        var parent = Path.GetDirectoryName(path);
        if (parent != null && !Directory.Exists(parent)) EnsureDirectoryJournaled(batchId, ref seq, parent);
        if (File.Exists(path)) throw new IOException($"Нельзя создать папку: на этом месте уже есть файл {path}");
        var rec = Journal(batchId, ref seq, OperationKind.CreateFolder, path, path, reason, 0, OperationStatus.Pending);
        try
        {
            Directory.CreateDirectory(path);
            _db.UpdateOperation(rec.Id, OperationStatus.Done);
        }
        catch (Exception ex)
        {
            _db.UpdateOperation(rec.Id, OperationStatus.Failed, Describe(ex));
            throw;
        }
    }

    private static void GuardDestination(string dest, OperationPlan plan)
    {
        bool inRoot = PathUtil.IsUnder(dest, plan.RootPath);
        bool inRecovery = !string.IsNullOrEmpty(plan.RecoveryFolder) && PathUtil.IsUnder(dest, plan.RecoveryFolder);
        if (!inRoot && !inRecovery && SystemPathGuard.IsProtectedPath(dest))
            throw new UnauthorizedAccessException($"Запись в системный каталог запрещена: {dest}");
        if (inRoot && !inRecovery && !SystemPathGuard.IsSafeToModify(dest))
            throw new UnauthorizedAccessException($"Запись в защищённый каталог запрещена: {dest}");
    }

    private OperationRecord Journal(long batchId, ref int seq, OperationKind kind, string source, string dest, string reason, long size, OperationStatus status, string? error = null)
    {
        var rec = new OperationRecord
        {
            BatchId = batchId, Seq = ++seq, Kind = kind, Source = source, Destination = dest, Reason = reason, Size = size,
            Status = status, Error = error, ExecutedUtc = DateTime.UtcNow,
        };
        _db.AddOperation(rec);
        return rec;
    }

    public static string Describe(Exception ex) => ex switch
    {
        UnauthorizedAccessException => "Нет доступа: " + ex.Message,
        FileNotFoundException => ex.Message,
        DirectoryNotFoundException => "Папка не найдена: " + ex.Message,
        PathTooLongException => "Слишком длинный путь: " + ex.Message,
        IOException io when (io.HResult & 0xFFFF) is 32 or 33 => "Файл открыт в другой программе: " + ex.Message,
        _ => ex.Message,
    };
}
