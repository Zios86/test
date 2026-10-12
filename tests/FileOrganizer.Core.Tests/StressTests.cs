using System.Diagnostics;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;
using FileOrganizer.Core.Search;
using Xunit.Abstractions;

namespace FileOrganizer.Core.Tests;

/// <summary>Нагрузочная проверка: каталог со 100 000+ файлов. Запуск: dotnet test --filter Category=Stress</summary>
[Trait("Category", "Stress")]
public class StressTests(ITestOutputHelper output)
{
    [Fact]
    public void Hundred_thousand_files_scan_analyze_plan_execute_rollback()
    {
        using var t = new TempDir();
        var sw = Stopwatch.StartNew();
        string[] exts = { "pdf", "docx", "jpg", "png", "mp3", "xlsx", "zip", "txt", "csv", "mp4" };
        for (int d = 0; d < 100; d++)
        {
            var dir = t.Dir($"Папка_{d:000}");
            for (int i = 0; i < 1000; i++)
                File.WriteAllText(Path.Combine(dir, $"файл_{i:0000}.{exts[i % exts.Length]}"), (i % 300).ToString()); // много одинаковых → дубликаты
        }
        for (int i = 0; i < 2000; i++) File.WriteAllText(t.P($"top_{i:0000}.{exts[i % exts.Length]}"), "top" + i);
        output.WriteLine($"Создание: {sw.Elapsed}");

        sw.Restart();
        var scan = t.Scan();
        output.WriteLine($"Сканирование {scan.Files.Count} файлов: {sw.Elapsed}");
        Assert.Equal(102_000, scan.Files.Count);

        sw.Restart();
        var db = t.Db();
        var report = new Analyzer(db).Analyze(scan, AnalysisOptions.Full());
        output.WriteLine($"Анализ: {sw.Elapsed}; групп дубликатов: {report.Duplicates.Count}");
        Assert.NotEmpty(report.Duplicates);

        sw.Restart();
        var hits = SearchService.Search(scan.Files, new SearchQuery { Name = "файл_0999*" });
        Assert.Equal(100, hits.Count);
        output.WriteLine($"Поиск: {sw.Elapsed}");

        sw.Restart();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, scan, new SortOptions { Strategy = SortStrategy.ByType });
        output.WriteLine($"План: {b.Plan.Operations.Count} операций, {sw.Elapsed}");
        Assert.Equal(2000, b.Plan.Summary().Moved);

        sw.Restart();
        var before = Snapshot.Take(t.Path);
        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(0, r.Failed);
        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(before, Snapshot.Take(t.Path));
        output.WriteLine($"Выполнение + откат: {sw.Elapsed}");
    }

    [Fact]
    public void File_larger_than_10_GB_is_scanned_bucketed_moved_and_restored()
    {
        using var t = new TempDir();
        var path = t.P("video.mkv");
        const long size = 11L * 1024 * 1024 * 1024;
        try
        {
            using var fs = new FileStream(path, FileMode.CreateNew);
            fs.SetLength(size); // разреженный файл: место на диске не занимает
        }
        catch (IOException) { return; } // файловая система не поддерживает такие файлы
        var scan = t.Scan();
        Assert.Equal(size, scan.Files.Single().Size);
        Assert.Equal(1, SpaceAnalyzer.SizeBuckets(scan.Files).Single(b => b.Label == "> 10 ГБ").Count);

        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, scan, new SortOptions { Strategy = SortStrategy.ByType });
        var db = t.Db();
        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(0, r.Failed);
        Assert.Equal(size, new FileInfo(t.P("Видео/video.mkv")).Length);
        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(size, new FileInfo(path).Length);
    }
}
