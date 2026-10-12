using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

public sealed class AnalysisOptions
{
    public bool FindDuplicates { get; set; } = true;
    public bool FindSimilarVersions { get; set; } = true;
    public bool FindEmptyFolders { get; set; } = true;
    public bool FindLargeFiles { get; set; } = true;
    public bool FindOldFiles { get; set; } = true;
    public bool AnalyzeNames { get; set; } = true;
    public bool FindSimilarFolders { get; set; } = true;
    public bool FindMisplaced { get; set; } = true;
    public bool FindTempFiles { get; set; } = true;
    public bool FindProjects { get; set; } = true;

    public TimeSpan OldThreshold { get; set; } = TimeSpan.FromDays(1095);
    public long LargeFileThreshold { get; set; } = 1024L * 1024 * 1024;
    public int TopLargestCount { get; set; } = 100;

    /// <summary>Быстрая уборка: дубли, пустые папки, старые установщики, крупные файлы.</summary>
    public static AnalysisOptions QuickCleanup() => new()
    {
        FindSimilarVersions = false, AnalyzeNames = false, FindSimilarFolders = false, FindMisplaced = false, FindProjects = false,
    };

    public static AnalysisOptions Full() => new();
}

public sealed record AnalysisProgress(string Stage, int Percent, string Detail);

/// <summary>Запускает все анализаторы над результатом сканирования и собирает отчёт.</summary>
public sealed class Analyzer
{
    private readonly IHashCache? _hashCache;
    private readonly SmartClassifier _smart;
    private readonly Logger _log;

    public Analyzer(IHashCache? hashCache = null, SmartClassifier? smart = null, Logger? log = null)
    {
        _hashCache = hashCache;
        _smart = smart ?? new SmartClassifier();
        _log = log ?? Logger.Null;
    }

    public Task<AnalysisReport> AnalyzeAsync(ScanResult scan, AnalysisOptions options, IProgress<AnalysisProgress>? progress = null, CancellationToken ct = default)
        => Task.Run(() => Analyze(scan, options, progress, ct), CancellationToken.None);

    public AnalysisReport Analyze(ScanResult scan, AnalysisOptions options, IProgress<AnalysisProgress>? progress = null, CancellationToken ct = default)
    {
        var report = new AnalysisReport { Scan = scan, OldThreshold = options.OldThreshold };
        var files = scan.Files;
        report.FileTypeCount = files.Select(f => f.Extension).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        report.CategoryUsage = SpaceAnalyzer.ByCategory(files);

        if (options.FindDuplicates)
        {
            progress?.Report(new AnalysisProgress("Поиск дубликатов", 10, ""));
            var finder = new DuplicateFinder(_hashCache);
            var dp = progress == null ? null : new Progress<DuplicateProgress>(p =>
                progress.Report(new AnalysisProgress("Поиск дубликатов", 10 + (p.Total == 0 ? 0 : 50 * p.Hashed / p.Total), $"Проверено {Format.Number(p.Hashed)} из {Format.Number(p.Total)}; групп: {p.GroupsFound}")));
            report.Duplicates = finder.Find(files, dp, ct);
            scan.Errors.AddRange(finder.Errors);
        }
        ct.ThrowIfCancellationRequested();

        if (options.FindSimilarVersions)
        {
            progress?.Report(new AnalysisProgress("Похожие версии документов", 62, ""));
            var dupPaths = new HashSet<string>(report.Duplicates.SelectMany(g => g.Files.Skip(1)).Select(f => f.FullPath), PathUtil.Comparer);
            report.SimilarVersions = SimilarVersionFinder.Find(files, dupPaths);
        }
        if (options.FindEmptyFolders)
        {
            progress?.Report(new AnalysisProgress("Пустые папки", 66, ""));
            report.EmptyFolders = EmptyFolderFinder.Find(scan);
        }
        ct.ThrowIfCancellationRequested();
        if (options.FindLargeFiles)
        {
            progress?.Report(new AnalysisProgress("Крупные файлы", 70, ""));
            report.LargeFiles = SpaceAnalyzer.TopLargest(files, options.TopLargestCount);
            report.SizeBuckets = SpaceAnalyzer.SizeBuckets(files);
            report.LargeFileThresholdCount = files.Count(f => f.Size > options.LargeFileThreshold);
        }
        if (options.FindOldFiles)
        {
            progress?.Report(new AnalysisProgress("Давно не использовавшиеся файлы", 74, ""));
            report.OldFiles = OldFileFinder.Find(files, options.OldThreshold);
        }
        if (options.AnalyzeNames)
        {
            progress?.Report(new AnalysisProgress("Анализ названий", 78, ""));
            report.BadNames = NameAnalyzer.Find(files);
        }
        ct.ThrowIfCancellationRequested();
        if (options.FindSimilarFolders)
        {
            progress?.Report(new AnalysisProgress("Похожие папки", 84, ""));
            report.SimilarFolders = SimilarFolderFinder.Find(scan);
        }
        if (options.FindMisplaced)
        {
            progress?.Report(new AnalysisProgress("Файлы не на своём месте", 88, ""));
            report.Misplaced = MisplacedFinder.Find(scan, _smart);
        }
        if (options.FindTempFiles)
        {
            progress?.Report(new AnalysisProgress("Временные файлы", 92, ""));
            report.TempFiles = TempFileDetector.Find(scan);
        }
        if (options.FindProjects)
        {
            progress?.Report(new AnalysisProgress("Проектные группы", 96, ""));
            report.Projects = ProjectDetector.Find(files);
        }

        // Потенциально избыточные данные: лишние точные копии + временные файлы, без двойного учёта.
        var redundant = new HashSet<string>(PathUtil.Comparer);
        long bytes = 0;
        foreach (var g in report.Duplicates)
            foreach (var f in g.Files.Where(f => f != g.Recommended))
                if (redundant.Add(f.FullPath)) bytes += f.Size;
        foreach (var t in report.TempFiles)
            if (redundant.Add(t.File.FullPath)) bytes += t.File.Size;
        report.PotentiallyRedundantBytes = bytes;

        progress?.Report(new AnalysisProgress("Готово", 100, ""));
        _log.Info("Analyze", scan.RootPath, $"duplicates={report.Duplicates.Count}; empty={report.EmptyFolders.Count}; badNames={report.BadNames.Count}");
        return report;
    }
}
