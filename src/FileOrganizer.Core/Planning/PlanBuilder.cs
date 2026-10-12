using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Planning;

/// <summary>
/// Собирает план изменений, следя за целостностью:
/// один файл — одна операция; путь назначения не перезаписывает существующий или уже запланированный файл;
/// недостающие папки добавляются как «Создать папку». Файловая система при этом не меняется.
/// </summary>
public sealed class PlanBuilder
{
    private readonly HashSet<string> _plannedDestinations = new(PathUtil.Comparer);
    private readonly HashSet<string> _plannedFolders = new(PathUtil.Comparer);
    private readonly HashSet<string> _usedSources = new(PathUtil.Comparer);

    public OperationPlan Plan { get; }
    public double AutoSelectThreshold { get; set; } = 0.85;
    public ConflictResolution DefaultResolution { get; set; } = ConflictResolution.KeepBoth;

    public PlanBuilder(string rootPath, string? recoveryFolder = null, string title = "")
    {
        var root = PathUtil.Normalize(rootPath);
        Plan = new OperationPlan
        {
            RootPath = root,
            Title = title,
            RecoveryFolder = recoveryFolder ?? Path.Combine(root, AppPaths.RecoveryFolderName, DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")),
        };
    }

    public bool IsSourceUsed(string path) => _usedSources.Contains(path);

    private bool IsTaken(string path, string? exceptSource = null)
    {
        if (exceptSource != null && PathUtil.SamePath(path, exceptSource)) return false;
        return File.Exists(path) || Directory.Exists(path) || _plannedDestinations.Contains(path) || _plannedFolders.Contains(path);
    }

    /// <summary>Добавляет операции «Создать папку» для всех отсутствующих уровней пути.</summary>
    public void EnsureFolder(string folder, string group, string reason = "папка для размещения файлов")
    {
        var missing = new Stack<string>();
        var cur = PathUtil.Normalize(folder);
        while (!string.IsNullOrEmpty(cur) && !Directory.Exists(cur) && !_plannedFolders.Contains(cur))
        {
            missing.Push(cur);
            cur = Path.GetDirectoryName(cur);
        }
        while (missing.Count > 0)
        {
            var f = missing.Pop();
            if (File.Exists(f)) throw new IOException($"Нельзя создать папку: на этом месте файл {f}");
            _plannedFolders.Add(f);
            Plan.Operations.Add(new PlannedOperation
            {
                Kind = OperationKind.CreateFolder, Source = f, Destination = f, Reason = reason, Group = group, Confidence = 1.0,
            });
        }
    }

    /// <summary>Переместить файл в папку (имя сохраняется, при конфликте — по правилу разрешения).</summary>
    public PlannedOperation? AddMove(FileEntry file, string destinationFolder, string reason, double confidence, string group, string? newName = null)
        => AddFileOp(OperationKind.Move, file.FullPath, file.Size, Path.Combine(destinationFolder, newName ?? file.Name), reason, confidence, group);

    public PlannedOperation? AddRename(FileEntry file, string newName, string reason, double confidence, string group)
        => AddFileOp(OperationKind.Rename, file.FullPath, file.Size, Path.Combine(file.DirectoryPath, newName), reason, confidence, group);

    /// <summary>«Удалить» = переместить в папку восстановления с сохранением относительного пути. Обратимо.</summary>
    public PlannedOperation? AddMoveToRecovery(string path, long size, string reason, string group, double confidence = 1.0)
    {
        var rel = PathUtil.IsUnder(path, Plan.RootPath) ? Path.GetRelativePath(Plan.RootPath, path) : Path.GetFileName(path);
        return AddFileOp(OperationKind.MoveToRecovery, path, size, Path.Combine(Plan.RecoveryFolder, rel), reason, confidence, group);
    }

    public PlannedOperation? AddMoveToRecovery(FileEntry f, string reason, string group, double confidence = 1.0)
        => AddMoveToRecovery(f.FullPath, f.Size, reason, group, confidence);

    public PlannedOperation? AddDeleteEmptyFolder(string folder, string reason, string group)
    {
        var path = PathUtil.Normalize(folder);
        if (_usedSources.Contains(path)) return null;
        if (!SystemPathGuard.IsSafeToModify(path) || PathUtil.SamePath(path, Plan.RootPath)) return null;
        _usedSources.Add(path);
        var op = new PlannedOperation
        {
            Kind = OperationKind.DeleteEmptyFolder, Source = path, Destination = path, Reason = reason, Group = group, Confidence = 1.0,
            Selected = true,
        };
        Plan.Operations.Add(op);
        return op;
    }

    private PlannedOperation? AddFileOp(OperationKind kind, string source, long size, string desired, string reason, double confidence, string group)
    {
        source = PathUtil.Normalize(source);
        desired = PathUtil.Normalize(desired);
        if (_usedSources.Contains(source)) return null;               // для файла уже есть действие
        if (PathUtil.SamePath(source, desired) && string.Equals(source, desired, StringComparison.Ordinal)) return null; // уже на месте
        if (!SystemPathGuard.IsSafeToModify(source)) return null;      // системные файлы не трогаем

        var op = new PlannedOperation
        {
            Kind = kind, Source = source, Destination = desired, OriginalDestination = desired, Reason = reason,
            Confidence = confidence, Group = group, Size = size, Resolution = DefaultResolution,
        };
        op.Selected = confidence >= AutoSelectThreshold;

        if (IsTaken(desired, source))
        {
            op.HasConflict = true;
            ApplyResolution(op);
        }

        var destDir = Path.GetDirectoryName(op.Destination)!;
        if (kind != OperationKind.MoveToRecovery) EnsureFolder(destDir, group);
        _usedSources.Add(source);
        _plannedDestinations.Add(op.Destination);
        Plan.Operations.Add(op);
        return op;
    }

    /// <summary>Пересчитывает путь назначения после смены способа разрешения конфликта.</summary>
    public void SetResolution(PlannedOperation op, ConflictResolution resolution)
    {
        if (!op.HasConflict) return;
        _plannedDestinations.Remove(op.Destination);
        op.Resolution = resolution;
        ApplyResolution(op);
        _plannedDestinations.Add(op.Destination);
    }

    private void ApplyResolution(PlannedOperation op)
    {
        var desired = op.OriginalDestination ?? op.Destination;
        switch (op.Resolution)
        {
            case ConflictResolution.KeepBoth:
                op.Destination = PathUtil.MakeUnique(desired, p => !PathUtil.SamePath(p, op.Source) && (_plannedDestinations.Contains(p) || _plannedFolders.Contains(p)));
                break;
            case ConflictResolution.Skip:
                op.Destination = desired;
                op.Selected = false;
                break;
            case ConflictResolution.Replace:
                op.Destination = desired;
                break;
        }
    }

    /// <summary>
    /// Включение/выключение действия с учётом зависимостей:
    /// включили перемещение — включаются нужные ему папки; выключили папку — выключаются действия внутри неё.
    /// </summary>
    public void SetSelected(PlannedOperation op, bool selected)
    {
        op.Selected = selected;
        if (op.Kind == OperationKind.CreateFolder && !selected)
        {
            foreach (var o in Plan.Operations.Where(o => o != op && o.Kind != OperationKind.MoveToRecovery && PathUtil.IsUnder(o.Destination, op.Source)))
                o.Selected = false;
        }
        else if (selected && op.Kind != OperationKind.CreateFolder)
        {
            foreach (var f in Plan.Operations.Where(o => o.Kind == OperationKind.CreateFolder && PathUtil.IsUnder(op.Destination, o.Source)))
                f.Selected = true;
        }
    }

    /// <summary>Убирает «Создать папку», которые больше не нужны ни одному выбранному действию.</summary>
    public void PruneUnusedFolders()
    {
        foreach (var f in Plan.Operations.Where(o => o.Kind == OperationKind.CreateFolder && o.Selected))
        {
            bool needed = Plan.Operations.Any(o => o.Selected && o.Kind is OperationKind.Move or OperationKind.Rename && PathUtil.IsUnder(o.Destination, f.Source));
            if (!needed) f.Selected = false;
        }
    }
}
