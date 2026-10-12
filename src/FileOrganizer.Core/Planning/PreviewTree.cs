using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Planning;

/// <summary>Цветовые обозначения изменений в дереве.</summary>
public enum ChangeMark { Keep, Move, Rename, Merge, Delete, Create }

public sealed class PreviewNode
{
    public required string Name { get; init; }
    public required string FullPath { get; init; }
    public bool IsFolder { get; init; }
    public ChangeMark Mark { get; set; }
    /// <summary>Подсказка: куда/откуда перемещается.</summary>
    public string? Detail { get; set; }
    public long Size { get; set; }
    public List<PreviewNode> Children { get; } = new();
    public int FileCount { get; set; }
    /// <summary>Развёрнут ли узел в дереве (корень и изменённые папки раскрыты сразу).</summary>
    public bool IsExpanded { get; set; }

    public static string MarkName(ChangeMark m) => m switch
    {
        ChangeMark.Move => "Переместить",
        ChangeMark.Rename => "Переименовать",
        ChangeMark.Merge => "Объединить",
        ChangeMark.Delete => "Удалить после подтверждения",
        ChangeMark.Create => "Создать",
        _ => "Оставить",
    };
}

/// <summary>Строит дерево «как сейчас» и «как будет» по результату сканирования и выбранным действиям плана.</summary>
public static class PreviewTreeBuilder
{
    public static PreviewNode BuildCurrent(ScanResult scan, OperationPlan? plan)
    {
        var bySource = new Dictionary<string, PlannedOperation>(PathUtil.Comparer);
        if (plan != null)
            foreach (var op in plan.SelectedOperations)
                if (op.Kind != OperationKind.CreateFolder) bySource[op.Source] = op;

        PreviewNode Build(FolderEntry f)
        {
            var node = new PreviewNode { Name = f.Name, FullPath = f.FullPath, IsFolder = true, Size = f.TotalSize, FileCount = f.TotalFileCount };
            if (bySource.TryGetValue(f.FullPath, out var fop) && fop.Kind == OperationKind.DeleteEmptyFolder)
            {
                node.Mark = ChangeMark.Delete;
                node.Detail = fop.Reason;
            }
            foreach (var s in f.SubFolders.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase)) node.Children.Add(Build(s));
            foreach (var file in f.Files.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
            {
                var fn = new PreviewNode { Name = file.Name, FullPath = file.FullPath, Size = file.Size };
                if (bySource.TryGetValue(file.FullPath, out var op))
                {
                    fn.Mark = op.Kind switch
                    {
                        OperationKind.Rename => ChangeMark.Rename,
                        OperationKind.MoveToRecovery => ChangeMark.Delete,
                        _ when op.Group == OperationPlanner.GroupMerge => ChangeMark.Merge,
                        _ => ChangeMark.Move,
                    };
                    fn.Detail = op.Kind == OperationKind.MoveToRecovery ? op.Reason : $"→ {Rel(plan!.RootPath, op.Destination)}";
                }
                node.Children.Add(fn);
            }
            if (node.Mark == ChangeMark.Keep && node.Children.Count > 0 && node.Children.All(c => c.Mark == ChangeMark.Merge))
                node.Mark = ChangeMark.Merge;
            return node;
        }
        var root = Build(scan.Root);
        root.IsExpanded = true;
        return root;
    }

    /// <summary>Будущая структура: исходное дерево с применёнными (виртуально) действиями.</summary>
    public static PreviewNode BuildProposed(ScanResult scan, OperationPlan plan)
    {
        var root = new PreviewNode { Name = scan.Root.Name, FullPath = scan.RootPath, IsFolder = true, IsExpanded = true };
        var index = new Dictionary<string, PreviewNode>(PathUtil.Comparer) { [scan.RootPath] = root };
        var selected = plan.SelectedOperations.ToList();
        var removed = new HashSet<string>(selected.Where(o => o.Kind != OperationKind.CreateFolder).Select(o => o.Source), PathUtil.Comparer);
        var created = new HashSet<string>(selected.Where(o => o.Kind == OperationKind.CreateFolder).Select(o => o.Source), PathUtil.Comparer);
        var recovery = plan.RecoveryFolder;

        PreviewNode EnsureFolder(string path)
        {
            if (index.TryGetValue(path, out var n)) return n;
            var parentPath = Path.GetDirectoryName(path);
            var parent = parentPath != null && PathUtil.IsUnder(parentPath, scan.RootPath) ? EnsureFolder(parentPath) : root;
            n = new PreviewNode
            {
                Name = Path.GetFileName(path), FullPath = path, IsFolder = true,
                Mark = created.Contains(path) || !Directory.Exists(path) ? ChangeMark.Create : ChangeMark.Keep,
            };
            n.IsExpanded = n.Mark == ChangeMark.Create;
            parent.Children.Add(n);
            index[path] = n;
            return n;
        }

        foreach (var folder in scan.Folders.Skip(1))
            if (!removed.Contains(folder.FullPath)) EnsureFolder(folder.FullPath);

        foreach (var f in scan.Files)
        {
            if (removed.Contains(f.FullPath)) continue;
            EnsureFolder(f.DirectoryPath).Children.Add(new PreviewNode { Name = f.Name, FullPath = f.FullPath, Size = f.Size });
        }

        foreach (var op in selected)
        {
            switch (op.Kind)
            {
                case OperationKind.CreateFolder:
                    if (PathUtil.IsUnder(op.Source, scan.RootPath) && !PathUtil.IsUnder(op.Source, recovery)) EnsureFolder(op.Source);
                    break;
                case OperationKind.Move:
                case OperationKind.Rename:
                    var dir = Path.GetDirectoryName(op.Destination)!;
                    if (!PathUtil.IsUnder(dir, scan.RootPath)) continue;
                    EnsureFolder(dir).Children.Add(new PreviewNode
                    {
                        Name = Path.GetFileName(op.Destination), FullPath = op.Destination, Size = op.Size,
                        Mark = op.Kind == OperationKind.Rename ? ChangeMark.Rename : op.Group == OperationPlanner.GroupMerge ? ChangeMark.Merge : ChangeMark.Move,
                        Detail = op.Kind == OperationKind.Rename ? $"было: {Path.GetFileName(op.Source)}" : $"из: {Rel(scan.RootPath, op.Source)}",
                    });
                    break;
            }
        }

        void Finish(PreviewNode n)
        {
            n.Children.Sort((a, b) => a.IsFolder != b.IsFolder ? (a.IsFolder ? -1 : 1) : string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
            foreach (var c in n.Children.Where(c => c.IsFolder)) Finish(c);
            n.Size = n.Children.Sum(c => c.Size);
            n.FileCount = n.Children.Sum(c => c.IsFolder ? c.FileCount : 1);
        }
        Finish(root);
        return root;
    }

    private static string Rel(string root, string path) => PathUtil.IsUnder(path, root) ? Path.GetRelativePath(root, path) : path;
}
