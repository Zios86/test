using System.Text;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;
using FileOrganizer.Core.Rules;
using FileOrganizer.Core.Scanning;

namespace FileOrganizer.Cli;

/// <summary>
/// Консольная версия: удобна для автоматизации и проверки. По умолчанию ничего не меняет —
/// команда apply требует явного подтверждения.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch (IOException) { /* консоль недоступна (вывод перенаправлен) */ }
        var dbPath = Arg(args, "--db");
        if (dbPath != null)
        {
            var i = Array.IndexOf(args, "--db");
            args = args.Take(i).Concat(args.Skip(i + 2)).ToArray();
        }
        if (args.Length == 0 || args[0] is "-h" or "--help" or "help") { Help(); return 0; }

        var db = new Database(dbPath ?? AppPaths.DatabasePath);
        var log = new Logger(AppPaths.LogDirectory);
        var recovered = new RollbackManager(db, log).RecoverInterrupted();
        if (recovered > 0) Console.WriteLine($"Восстановлено состояние после аварийного завершения: {recovered} операций помечено. См. «fo history».");

        try
        {
            switch (args[0])
            {
                case "analyze": return await Analyze(db, log, args);
                case "plan": return await PlanOrApply(db, log, args, apply: false);
                case "apply": return await PlanOrApply(db, log, args, apply: true);
                case "history": return History(db, args);
                case "undo": return Undo(db, log, args);
                case "restore": return Restore(db, log, args);
                case "purge": return Purge(db, log, args);
                default: Help(); return 2;
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine("Ошибка: " + ex.Message);
            return 1;
        }
    }

    private static void Help()
    {
        Console.WriteLine("""
            Умный файловый организатор — консольная версия

            fo analyze <папка> [--old-days N] [--all-dups]       Анализ: статистика, дубликаты, пустые папки, крупные/старые файлы
            fo plan    <папка> [--strategy type|smart|rules] [--recursive] [--dups] [--empty]
                                                                 Предпросмотр: что будет сделано (ничего не меняет)
            fo apply   <папка> [те же параметры] [--include-low] [--yes]
                                                                 Выполнить план после подтверждения
            fo history                                          История операций
            fo undo [номер]                                     Отменить последнее действие (или пакет с номером)
            fo restore --before "ГГГГ-ММ-ДД ЧЧ:ММ"              Восстановить состояние до указанного времени
            fo purge <номер> --yes                              Удалить окончательно файлы пакета из папки восстановления

            Общие параметры: --db <путь к базе>
            """);
    }

    private static string? Arg(string[] args, string name)
    {
        var i = Array.IndexOf(args, name);
        return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
    }

    private static bool Flag(string[] args, string name) => args.Contains(name);

    private static string Folder(string[] args) =>
        args.Length > 1 && !args[1].StartsWith("--") ? Path.GetFullPath(args[1]) : throw new ArgumentException("Укажите папку.");

    private static async Task<AnalysisReport> RunAnalysis(Database db, Logger log, string folder, AnalysisOptions options)
    {
        var profile = db.GetActiveProfile();
        var settings = db.LoadSettings();
        var scanner = new Scanner(Classifier(settings), log);
        var progress = new Progress<ScanProgress>(p => Console.Write($"\rСканируется: {Format.Number(p.FilesScanned)} файлов, {Format.Number(p.FoldersScanned)} папок   "));
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        var scan = await scanner.ScanAsync(folder, new ScanOptions { Exclusions = db.GetExclusions(profile.Id), IncludeHidden = settings.IncludeHidden }, progress, cts.Token);
        Console.WriteLine();
        if (scan.Cancelled) Console.WriteLine("Сканирование остановлено пользователем — результаты неполные.");
        var report = await new Analyzer(db, null, log).AnalyzeAsync(scan, options, null, cts.Token);
        db.SaveScanSummary(report);
        return report;
    }

    private static FileClassifier Classifier(AppSettings s) =>
        s.CustomCategories is { Count: > 0 } c
            ? new FileClassifier(c.Where(kv => Enum.TryParse<FileCategory>(kv.Key, out _)).ToDictionary(kv => Enum.Parse<FileCategory>(kv.Key), kv => kv.Value))
            : new FileClassifier();

    private static async Task<int> Analyze(Database db, Logger log, string[] args)
    {
        var folder = Folder(args);
        var opts = AnalysisOptions.Full();
        if (int.TryParse(Arg(args, "--old-days"), out var days)) opts.OldThreshold = TimeSpan.FromDays(days);
        var r = await RunAnalysis(db, log, folder, opts);

        Console.WriteLine($"""

            Проанализировано: {folder}
              Файлов:                    {Format.Number(r.FileCount)}
              Папок:                     {Format.Number(r.FolderCount)}
              Общий размер:              {Format.Size(r.TotalSize)}
              Типов файлов:              {r.FileTypeCount}
              Дубликатов:                {Format.Number(r.DuplicateFileCount)} ({Format.Size(r.DuplicateBytes)})
              Похожих версий документов: {r.SimilarVersions.Count} групп
              Пустых папок:              {r.EmptyFolders.Count}
              Файлов старше {(int)r.OldThreshold.TotalDays} дн.:     {Format.Number(r.OldFiles.Count)} ({Format.Size(r.OldFilesBytes)})
              Файлов > 1 ГБ:             {r.LargeFileThresholdCount}
              Плохих названий:           {r.BadNames.Count}
              Похожих папок:             {r.SimilarFolders.Count} групп
              Возможно лежат не там:     {r.Misplaced.Count}
              Временных файлов:          {r.TempFiles.Count}
              Проектных групп:           {r.Projects.Count}
              Потенциально избыточные данные: {Format.Size(r.PotentiallyRedundantBytes)}
            """);
        if (r.Scan.Errors.Count > 0) Console.WriteLine($"  Нет доступа / ошибки: {r.Scan.Errors.Count} (первые: {string.Join("; ", r.Scan.Errors.Take(3).Select(e => e.Path))})");

        if (r.Duplicates.Count > 0)
        {
            Console.WriteLine("\nКрупнейшие группы дубликатов:");
            foreach (var g in r.Duplicates.Take(Flag(args, "--all-dups") ? int.MaxValue : 5))
            {
                Console.WriteLine($"  {Format.Size(g.Size)} × {g.Files.Count}");
                foreach (var f in g.Files)
                    Console.WriteLine($"    {(f == g.Recommended ? "★" : " ")} {f.FullPath}  ({Format.Date(f.LastWriteTimeUtc)})");
                Console.WriteLine($"      рекомендуется оставить ★: {g.RecommendationReason}");
            }
        }
        if (r.BadNames.Count > 0)
        {
            Console.WriteLine("\nНазвания, которые можно улучшить:");
            foreach (var b in r.BadNames.Take(10)) Console.WriteLine($"  {b.File.Name} → {b.SuggestedName}  ({b.Problem})");
        }
        if (r.LargeFiles.Count > 0)
        {
            Console.WriteLine("\nЧто занимает место (топ-10):");
            foreach (var f in r.LargeFiles.Take(10)) Console.WriteLine($"  {Format.Size(f.Size),10}  {f.FullPath}");
        }
        return 0;
    }

    private static async Task<int> PlanOrApply(Database db, Logger log, string[] args, bool apply)
    {
        var folder = Folder(args);
        var strategy = (Arg(args, "--strategy") ?? "smart") switch
        {
            "type" => SortStrategy.ByType,
            "rules" => SortStrategy.RulesOnly,
            _ => SortStrategy.Smart,
        };
        var r = await RunAnalysis(db, log, folder, AnalysisOptions.Full());
        var settings = db.LoadSettings();
        var rules = new RulesEngine(db.GetRules(db.GetActiveProfile().Id));
        var b = new PlanBuilder(folder, title: $"Наведение порядка: {Path.GetFileName(folder)}") { AutoSelectThreshold = settings.AutoSelectThreshold };
        if (Flag(args, "--dups")) foreach (var g in r.Duplicates) OperationPlanner.PlanDuplicates(b, g, DuplicateDecision.KeepRecommended);
        OperationPlanner.PlanSort(b, r.Scan, new SortOptions { Strategy = strategy, TopLevelOnly = !Flag(args, "--recursive") }, new SmartClassifier(), rules, r.Projects);
        if (Flag(args, "--empty")) OperationPlanner.PlanEmptyFolders(b, r.EmptyFolders);
        if (Flag(args, "--include-low")) foreach (var op in b.Plan.Operations) b.SetSelected(op, true);
        b.PruneUnusedFolders();

        PrintPlan(b.Plan, folder);
        if (!apply) { Console.WriteLine("\nЭто предпросмотр — файлы не изменены. Для выполнения: fo apply ..."); return 0; }

        var s = b.Plan.Summary();
        if (s.Total == 0) { Console.WriteLine("Нечего выполнять."); return 0; }
        if (!Flag(args, "--yes"))
        {
            Console.Write($"\nВыполнить {s.Total} операций? Все изменения можно будет отменить (fo undo). [y/N]: ");
            var answer = Console.ReadLine()?.Trim().ToLowerInvariant();
            if (answer is not ("y" or "yes" or "д" or "да")) { Console.WriteLine("Отменено."); return 0; }
        }
        var exec = new OperationExecutor(db, log) { SimulationMode = false };
        var progress = new Progress<ExecutionProgress>(p => Console.Write($"\rВыполнено {p.Done} / {p.Total}   "));
        var result = await exec.ExecuteAsync(b.Plan, progress);
        Console.WriteLine($"\nГотово: успешно {result.Succeeded}, пропущено {result.Skipped}, ошибок {result.Failed}. Пакет №{result.BatchId}.");
        foreach (var (op, err) in result.Errors.Take(20)) Console.WriteLine($"  ✗ {op.Source}: {err}");
        return result.Failed > 0 ? 3 : 0;
    }

    private static void PrintPlan(OperationPlan plan, string root)
    {
        var s = plan.Summary();
        Console.WriteLine($"""

            Будет выполнено:
              Перемещено:                {s.Moved}
              Переименовано:             {s.Renamed}
              Создано папок:             {s.FoldersCreated}
              Удалено пустых каталогов:  {s.EmptyFoldersRemoved}
              В папку восстановления:    {s.SentToRecovery}
              Конфликтов имён:           {s.Conflicts}
              Не отмечено (низкая/средняя уверенность): {s.NotSelected}
            """);
        foreach (var group in plan.Operations.Where(o => o.Kind != OperationKind.CreateFolder).GroupBy(o => o.Group))
        {
            Console.WriteLine($"\n[{group.Key}]");
            foreach (var op in group.Take(50))
            {
                var mark = op.Selected ? "☑" : "☐";
                var conf = op.Level switch { ConfidenceLevel.High => "высокая", ConfidenceLevel.Medium => "средняя", _ => "низкая" };
                var dest = op.Kind == OperationKind.DeleteEmptyFolder ? "удалить" : Path.GetRelativePath(root, op.Destination);
                Console.WriteLine($"  {mark} {Path.GetRelativePath(root, op.Source)} → {dest}  [{conf} {Format.Percent(op.Confidence)}]{(op.HasConflict ? " ⚠ конфликт имён" : "")}");
                Console.WriteLine($"      причина: {op.Reason}");
            }
            if (group.Count() > 50) Console.WriteLine($"  … и ещё {group.Count() - 50}");
        }
    }

    private static int History(Database db, string[] args)
    {
        var batches = db.GetBatches();
        if (batches.Count == 0) { Console.WriteLine("История пуста."); return 0; }
        foreach (var b in batches)
            Console.WriteLine($"№{b.Id,-4} {Format.Date(b.CreatedUtc)}  {b.Title}  — {b.DoneCount} операций, ошибок {b.FailedCount}, статус: {b.Status}");
        return 0;
    }

    private static int Undo(Database db, Logger log, string[] args)
    {
        var rm = new RollbackManager(db, log);
        var r = args.Length > 1 && long.TryParse(args[1], out var id) ? rm.RollbackBatch(id) : rm.UndoLast();
        Console.WriteLine($"Восстановлено: {r.Restored}, не удалось: {r.Failed}");
        foreach (var m in r.Messages) Console.WriteLine("  " + m);
        return r.Failed > 0 ? 3 : 0;
    }

    private static int Restore(Database db, Logger log, string[] args)
    {
        var s = Arg(args, "--before") ?? throw new ArgumentException("Укажите --before \"ГГГГ-ММ-ДД ЧЧ:ММ\"");
        var point = DateTime.Parse(s, System.Globalization.CultureInfo.GetCultureInfo("ru-RU")).ToUniversalTime();
        var r = new RollbackManager(db, log).RestoreTo(point);
        Console.WriteLine($"Восстановлено: {r.Restored}, не удалось: {r.Failed}");
        foreach (var m in r.Messages) Console.WriteLine("  " + m);
        return 0;
    }

    private static int Purge(Database db, Logger log, string[] args)
    {
        if (args.Length < 2 || !long.TryParse(args[1], out var id)) throw new ArgumentException("Укажите номер пакета.");
        Console.WriteLine("ВНИМАНИЕ: файлы пакета будут удалены из папки восстановления БЕЗВОЗВРАТНО. Отменить это действие нельзя.");
        if (!Flag(args, "--yes"))
        {
            Console.Write("Введите «УДАЛИТЬ» для подтверждения: ");
            if (Console.ReadLine()?.Trim() != "УДАЛИТЬ") { Console.WriteLine("Отменено."); return 0; }
        }
        Console.WriteLine($"Удалено окончательно: {new RollbackManager(db, log).PurgeRecovery(id)}");
        return 0;
    }
}
