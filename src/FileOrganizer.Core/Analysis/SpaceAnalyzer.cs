using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>«Что занимает место»: крупные файлы, корзины по размеру, распределение по типам.</summary>
public static class SpaceAnalyzer
{
    public const long MB = 1024L * 1024;
    public const long GB = 1024L * MB;

    public static readonly (string Label, long Min)[] Buckets =
    {
        ("> 100 МБ", 100 * MB),
        ("> 500 МБ", 500 * MB),
        ("> 1 ГБ", 1 * GB),
        ("> 5 ГБ", 5 * GB),
        ("> 10 ГБ", 10 * GB),
    };

    public static List<FileEntry> TopLargest(IEnumerable<FileEntry> files, int count = 100, long minSize = 0) =>
        files.Where(f => f.Size >= minSize).OrderByDescending(f => f.Size).Take(count).ToList();

    public static List<SizeBucket> SizeBuckets(IReadOnlyList<FileEntry> files) =>
        Buckets.Select(b =>
        {
            var list = files.Where(f => f.Size > b.Min).ToList();
            return new SizeBucket(b.Label, b.Min, list.Count, list.Sum(f => f.Size));
        }).ToList();

    public static List<CategoryUsage> ByCategory(IEnumerable<FileEntry> files) =>
        files.GroupBy(f => f.Category)
            .Select(g => new CategoryUsage(g.Key, g.Count(), g.Sum(f => f.Size)))
            .OrderByDescending(c => c.TotalBytes)
            .ToList();

    /// <summary>Самые «тяжёлые» папки первого уровня.</summary>
    public static List<FolderEntry> LargestFolders(ScanResult scan, int count = 20) =>
        scan.Root.SubFolders.OrderByDescending(f => f.TotalSize).Take(count).ToList();
}

/// <summary>«Давно не использовались». Старый файл не считается ненужным автоматически.</summary>
public static class OldFileFinder
{
    public static readonly (string Label, int Days)[] Presets =
    {
        ("Старше 6 месяцев", 182),
        ("Старше 1 года", 365),
        ("Старше 2 лет", 730),
        ("Старше 3 лет", 1095),
    };

    public static List<FileEntry> Find(IEnumerable<FileEntry> files, TimeSpan olderThan, DateTime? nowUtc = null)
    {
        var limit = (nowUtc ?? DateTime.UtcNow) - olderThan;
        // Ориентируемся на дату изменения: дата последнего доступа в Windows часто не обновляется.
        return files.Where(f => f.LastWriteTimeUtc < limit)
            .OrderBy(f => f.LastWriteTimeUtc)
            .ToList();
    }
}
