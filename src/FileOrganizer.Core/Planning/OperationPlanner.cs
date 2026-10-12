using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Rules;

namespace FileOrganizer.Core.Planning;

public enum SortStrategy
{
    /// <summary>По типам файлов: Документы, Таблицы, Изображения…</summary>
    ByType,
    /// <summary>Умная сортировка: правила → имя → соседние файлы → тип.</summary>
    Smart,
    /// <summary>Только пользовательские правила (ручной режим).</summary>
    RulesOnly,
}

public sealed class SortOptions
{
    public SortStrategy Strategy { get; set; } = SortStrategy.Smart;
    /// <summary>Сортировать только файлы, лежащие прямо в выбранной папке (Загрузки, Рабочий стол). Структура подпапок не трогается.</summary>
    public bool TopLevelOnly { get; set; } = true;
    /// <summary>Куда раскладывать (по умолчанию — в выбранную папку).</summary>
    public string? TargetRoot { get; set; }
    public bool UseUserRules { get; set; } = true;
    public bool GroupProjects { get; set; } = true;
    /// <summary>Не трогать файлы «Прочее» (тип не распознан).</summary>
    public bool SkipUnknown { get; set; } = true;
    /// <summary>Готовые результаты глубокой классификации (по содержимому/ИИ), если они были получены.</summary>
    public Dictionary<string, ClassificationResult>? DeepResults { get; set; }
}

public enum DuplicateDecision { KeepAll, KeepRecommended, KeepNewest, KeepOldest, KeepSelected, MoveCopiesToDuplicatesFolder }

/// <summary>Формирует предложения (план) по результатам анализа. Ничего не меняет на диске.</summary>
public static class OperationPlanner
{
    public const string GroupSort = "Сортировка";
    public const string GroupProjects = "Проекты";
    public const string GroupDuplicates = "Дубликаты";
    public const string GroupEmpty = "Пустые папки";
    public const string GroupRename = "Переименование";
    public const string GroupMerge = "Объединение папок";
    public const string GroupMisplaced = "Возможно лежат не там";
    public const string GroupOld = "Давно не использовались";
    public const string GroupTemp = "Временные файлы";

    /// <summary>Сортировка файлов по папкам (по типу, умная или по правилам).</summary>
    public static void PlanSort(PlanBuilder b, ScanResult scan, SortOptions o, SmartClassifier? smart = null, RulesEngine? rules = null, IEnumerable<ProjectGroup>? projects = null)
    {
        smart ??= new SmartClassifier();
        var targetRoot = PathUtil.Normalize(o.TargetRoot ?? scan.RootPath);
        var files = o.TopLevelOnly ? scan.Root.Files : scan.Files;
        var fileSet = new HashSet<FileEntry>(files);

        // Проектные группы: файлы одного проекта — в «Проекты/Проект_X/<подпапка>».
        if (o.GroupProjects && o.Strategy == SortStrategy.Smart && projects != null)
        {
            foreach (var p in projects)
            {
                var members = p.Files.Where(fileSet.Contains).ToList();
                if (members.Count < 3) continue;
                var projectFolder = Path.Combine(targetRoot, "Проекты", PathUtil.SanitizeFileName("Проект_" + p.Name));
                foreach (var f in members)
                {
                    if (rules?.Match(f) != null) continue; // правило пользователя приоритетнее
                    var dest = Path.Combine(projectFolder, ProjectDetector.SubfolderFor(f.Category));
                    if (IsAlreadyIn(f, dest)) continue;
                    b.AddMove(f, dest, $"Проектная группа «{p.Name}»: {p.Reason}", 0.75, GroupProjects);
                }
            }
        }

        foreach (var f in files)
        {
            if (b.IsSourceUsed(f.FullPath)) continue;

            // 1. Пользовательские правила — наивысший приоритет.
            if (o.UseUserRules && rules != null && rules.Match(f) is { } m)
            {
                if (m.Rule.Action == RuleActionKind.Skip || m.TargetFolder == null) continue;
                var dest = Path.Combine(targetRoot, m.TargetFolder);
                if (!IsAlreadyIn(f, dest))
                    b.AddMove(f, dest, $"Правило «{m.Rule.Name}»: {m.Rule.Description}", 1.0, GroupSort);
                continue;
            }
            if (o.Strategy == SortStrategy.RulesOnly) continue;

            ClassificationResult c;
            if (o.Strategy == SortStrategy.ByType) c = SmartClassifier.ByType(f);
            else if (o.DeepResults != null && o.DeepResults.TryGetValue(f.FullPath, out var deep)) c = deep;
            else c = smart.Classify(f, f.DirectoryPath == scan.RootPath ? scan.Root.Files : scan.FindFolder(f.DirectoryPath)?.Files);

            if (o.SkipUnknown && f.Category == FileCategory.Other && c.Source == ClassificationSource.FileType) continue;
            if (f.Category == FileCategory.Temporary) continue; // временные файлы — отдельный раздел

            var target = Path.Combine(targetRoot, c.TargetFolder.Replace('/', Path.DirectorySeparatorChar));
            if (IsAlreadyIn(f, target) || IsInsideTopicFolder(f, c, targetRoot)) continue;
            b.AddMove(f, target, Capitalize(c.Explanation), c.Confidence, GroupSort);
        }
    }

    private static bool IsAlreadyIn(FileEntry f, string folder) => PathUtil.SamePath(f.DirectoryPath, folder);

    /// <summary>Файл уже лежит в папке с подходящим названием (например, «Счета/2024») — оставляем.</summary>
    private static bool IsInsideTopicFolder(FileEntry f, ClassificationResult c, string targetRoot)
    {
        var last = c.TargetFolder.Split('/', '\\').Last();
        var dir = f.DirectoryPath;
        while (dir != null && PathUtil.IsUnder(dir, targetRoot) && !PathUtil.SamePath(dir, targetRoot))
        {
            if (string.Equals(TextNormalizer.Normalize(Path.GetFileName(dir)), TextNormalizer.Normalize(last), StringComparison.Ordinal)) return true;
            dir = Path.GetDirectoryName(dir);
        }
        return false;
    }

    private static string Capitalize(string s) => s.Length == 0 ? s : char.ToUpper(s[0]) + s[1..];

    /// <summary>Решение по группе дубликатов. Лишние копии уходят в папку восстановления или в папку «Дубликаты».</summary>
    public static void PlanDuplicates(PlanBuilder b, DuplicateGroup g, DuplicateDecision decision, IReadOnlyCollection<string>? keepPaths = null)
    {
        if (decision == DuplicateDecision.KeepAll) return;
        FileEntry? keep = decision switch
        {
            DuplicateDecision.KeepNewest => g.Files.OrderByDescending(f => f.LastWriteTimeUtc).First(),
            DuplicateDecision.KeepOldest => g.Files.OrderBy(f => f.LastWriteTimeUtc).First(),
            DuplicateDecision.KeepSelected => null,
            _ => g.Recommended ?? g.Files[0],
        };
        var keepSet = new HashSet<string>(keepPaths ?? Array.Empty<string>(), PathUtil.Comparer);
        if (keep != null) keepSet.Add(keep.FullPath);
        if (keepSet.Count == 0) return; // нельзя убрать все копии
        var kept = string.Join(", ", keepSet.Select(Path.GetFileName));

        foreach (var f in g.Files.Where(f => !keepSet.Contains(f.FullPath)))
        {
            var reason = $"Точная копия (SHA-256 совпадает), сохраняется: {kept}";
            if (decision == DuplicateDecision.MoveCopiesToDuplicatesFolder)
                b.AddMove(f, Path.Combine(b.Plan.RootPath, "Дубликаты"), reason, 1.0, GroupDuplicates);
            else
                b.AddMoveToRecovery(f, reason + ". Копия будет перемещена в папку восстановления", GroupDuplicates);
        }
    }

    /// <summary>Удаление пустых папок (с переносом служебных файлов Thumbs.db/desktop.ini в папку восстановления).</summary>
    public static void PlanEmptyFolders(PlanBuilder b, IEnumerable<EmptyFolderInfo> folders)
    {
        foreach (var info in folders)
        {
            if (!Directory.Exists(info.Path)) continue;
            foreach (var junk in info.JunkFiles)
            {
                long size = 0;
                try { size = new FileInfo(junk).Length; } catch { /* ignore */ }
                b.AddMoveToRecovery(junk, size, "Служебный файл в пустой папке", GroupEmpty);
            }
            // Вложенные пустые папки — от самых глубоких к верхней.
            List<string> nested;
            try
            {
                nested = Directory.EnumerateDirectories(info.Path, "*", new EnumerationOptions { RecurseSubdirectories = true, AttributesToSkip = 0 })
                    .OrderByDescending(p => p.Length).ToList();
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { continue; }
            foreach (var n in nested) b.AddDeleteEmptyFolder(n, "Пустая вложенная папка", GroupEmpty);
            var reason = info.OnlySystemFiles ? "Папка содержит только служебные файлы" : info.NestedEmptyCount > 0 ? $"Цепочка из {info.NestedEmptyCount + 1} пустых папок" : "Пустая папка";
            b.AddDeleteEmptyFolder(info.Path, reason, GroupEmpty);
        }
    }

    public static void PlanRenames(PlanBuilder b, IEnumerable<(FileEntry File, string NewName, string Reason)> renames)
    {
        foreach (var (f, newName, reason) in renames)
        {
            if (string.Equals(f.Name, newName, StringComparison.Ordinal)) continue;
            b.AddRename(f, PathUtil.SanitizeFileName(newName), reason, 1.0, GroupRename);
        }
    }

    /// <summary>Объединение похожих папок в целевую. Структура вложенных папок сохраняется; конфликты помечаются.</summary>
    public static void PlanMergeFolders(PlanBuilder b, SimilarFolderGroup g, FolderEntry? target = null)
    {
        target ??= g.Target;
        foreach (var folder in g.Folders.Where(f => f != target))
        {
            foreach (var file in SimilarFolderFinder.AllFiles(folder))
            {
                var rel = Path.GetRelativePath(folder.FullPath, file.DirectoryPath);
                var dest = rel == "." ? target.FullPath : Path.Combine(target.FullPath, rel);
                b.AddMove(file, dest, $"Объединение «{folder.Name}» → «{target.Name}» (похожие названия)", 0.9, GroupMerge);
            }
        }
    }

    public static void PlanMisplaced(PlanBuilder b, IEnumerable<MisplacedFileInfo> items)
    {
        foreach (var m in items)
        {
            if (m.SuggestedFolder == null) continue;
            var dest = Path.Combine(b.Plan.RootPath, m.SuggestedFolder.Replace('/', Path.DirectorySeparatorChar));
            b.AddMove(m.File, dest, Capitalize(m.Reason), m.Confidence, GroupMisplaced);
        }
    }

    /// <summary>Перенос давно не использовавшихся файлов в архив (с сохранением относительного пути).</summary>
    public static void PlanArchiveOld(PlanBuilder b, IEnumerable<FileEntry> files, string archiveFolderName = "99_Архив")
    {
        var archive = Path.Combine(b.Plan.RootPath, archiveFolderName);
        foreach (var f in files)
        {
            if (PathUtil.IsUnder(f.FullPath, archive)) continue;
            var rel = Path.GetRelativePath(b.Plan.RootPath, f.DirectoryPath);
            var dest = rel == "." ? archive : Path.Combine(archive, rel);
            b.AddMove(f, dest, $"Не изменялся с {f.LastWriteTimeUtc.ToLocalTime():dd.MM.yyyy}", 1.0, GroupOld);
        }
    }

    public static void PlanTempFiles(PlanBuilder b, IEnumerable<TempFileInfo> items)
    {
        foreach (var t in items)
            b.AddMoveToRecovery(t.File, Capitalize(t.Reason) + ". Файл будет перемещён в папку восстановления", GroupTemp);
    }

    /// <summary>
    /// «Навести порядок»: комплексный план по отчёту анализа. Предложения с низкой уверенностью не отмечаются.
    /// Дубликаты и временные файлы по умолчанию НЕ отмечены — пользователь решает сам.
    /// </summary>
    public static OperationPlan PlanTidyUp(AnalysisReport report, SortOptions sort, SmartClassifier? smart = null, RulesEngine? rules = null, string? recoveryFolder = null)
    {
        var b = new PlanBuilder(report.Scan.RootPath, recoveryFolder, $"Наведение порядка: {Path.GetFileName(report.Scan.RootPath)}");
        PlanSort(b, report.Scan, sort, smart, rules, report.Projects);
        foreach (var g in report.Duplicates) PlanDuplicates(b, g, DuplicateDecision.KeepRecommended);
        PlanEmptyFolders(b, report.EmptyFolders);
        foreach (var op in b.Plan.Operations.Where(o => o.Group is GroupDuplicates))
            op.Selected = false;
        return b.Plan;
    }
}
