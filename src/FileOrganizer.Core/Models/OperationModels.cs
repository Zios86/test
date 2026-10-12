namespace FileOrganizer.Core.Models;

public enum OperationKind
{
    CreateFolder,
    Move,
    Rename,
    /// <summary>«Удаление» — перемещение в папку восстановления FileOrganizer_Recovery (обратимо).</summary>
    MoveToRecovery,
    DeleteEmptyFolder,
}

public enum OperationStatus { Planned, Pending, Done, Failed, Skipped, RolledBack, RollbackFailed, Purged }

public enum ConflictResolution
{
    /// <summary>Оставить оба: новому файлу даётся уникальное имя «имя (2).ext».</summary>
    KeepBoth,
    Skip,
    /// <summary>Заменить: существующий файл сначала уходит в папку восстановления (обратимо).</summary>
    Replace,
}

public enum ConfidenceLevel { Low, Medium, High }

/// <summary>Одно запланированное изменение. Пока пользователь не подтвердил план — это только предложение.</summary>
public sealed class PlannedOperation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public OperationKind Kind { get; init; }
    public required string Source { get; init; }
    /// <summary>Для Move/Rename/MoveToRecovery — конечный путь. Для CreateFolder/DeleteEmptyFolder совпадает с Source.</summary>
    public string Destination { get; set; } = "";
    /// <summary>Объяснение, почему предлагается это действие.</summary>
    public string Reason { get; init; } = "";
    /// <summary>Уверенность 0..1. Для действий по точным правилам — 1.</summary>
    public double Confidence { get; init; } = 1.0;
    public bool Selected { get; set; } = true;
    public bool HasConflict { get; set; }
    public ConflictResolution Resolution { get; set; } = ConflictResolution.KeepBoth;
    /// <summary>Путь назначения, указанный изначально (до разрешения конфликта).</summary>
    public string? OriginalDestination { get; set; }
    public long Size { get; init; }
    /// <summary>Группа действия для предпросмотра («Сортировка по типам», «Дубликаты»…).</summary>
    public string Group { get; init; } = "";

    public ConfidenceLevel Level => Confidence >= 0.85 ? ConfidenceLevel.High : Confidence >= 0.6 ? ConfidenceLevel.Medium : ConfidenceLevel.Low;

    public override string ToString() => $"{Kind}: {Source} -> {Destination}";
}

public sealed class OperationPlan
{
    public required string RootPath { get; init; }
    public string Title { get; set; } = "";
    public List<PlannedOperation> Operations { get; } = new();
    public string RecoveryFolder { get; set; } = "";

    public IEnumerable<PlannedOperation> SelectedOperations => Operations.Where(o => o.Selected);

    public PlanSummary Summary()
    {
        var sel = SelectedOperations.ToList();
        return new PlanSummary(
            Moved: sel.Count(o => o.Kind == OperationKind.Move),
            Renamed: sel.Count(o => o.Kind == OperationKind.Rename),
            FoldersCreated: sel.Count(o => o.Kind == OperationKind.CreateFolder),
            EmptyFoldersRemoved: sel.Count(o => o.Kind == OperationKind.DeleteEmptyFolder),
            SentToRecovery: sel.Count(o => o.Kind == OperationKind.MoveToRecovery),
            Conflicts: sel.Count(o => o.HasConflict),
            Total: sel.Count,
            NotSelected: Operations.Count - sel.Count);
    }
}

public sealed record PlanSummary(int Moved, int Renamed, int FoldersCreated, int EmptyFoldersRemoved, int SentToRecovery, int Conflicts, int Total, int NotSelected);

public enum BatchStatus { Running, Completed, CompletedWithErrors, Cancelled, Interrupted, RolledBack, PartiallyRolledBack }

/// <summary>Запись журнала об одной выполненной операции — всё, что нужно для отката.</summary>
public sealed class OperationRecord
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public int Seq { get; set; }
    public OperationKind Kind { get; set; }
    public string Source { get; set; } = "";
    public string Destination { get; set; } = "";
    public string OldName => Path.GetFileName(Source);
    public string NewName => Path.GetFileName(Destination);
    public OperationStatus Status { get; set; }
    public string? Error { get; set; }
    public string? Reason { get; set; }
    public DateTime? ExecutedUtc { get; set; }
    public long Size { get; set; }
}

public sealed class BatchInfo
{
    public long Id { get; set; }
    public string Title { get; set; } = "";
    public string RootPath { get; set; } = "";
    public DateTime CreatedUtc { get; set; }
    public BatchStatus Status { get; set; }
    public int OperationCount { get; set; }
    public int DoneCount { get; set; }
    public int FailedCount { get; set; }
    public string RecoveryFolder { get; set; } = "";
}

public sealed record ExecutionProgress(int Done, int Total, string CurrentPath, int Failed);

public sealed class ExecutionResult
{
    public long BatchId { get; init; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }
    public bool Cancelled { get; set; }
    public List<(PlannedOperation Op, string Error)> Errors { get; } = new();
}

public sealed class RollbackResult
{
    public int Restored { get; set; }
    public int Failed { get; set; }
    public List<string> Messages { get; } = new();
}
