using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

public interface IHashCache
{
    bool TryGet(string path, long size, DateTime mtimeUtc, out string sha256);
    void Set(string path, long size, DateTime mtimeUtc, string sha256);
    void Flush() { }
}

public sealed record DuplicateProgress(int Hashed, int Total, int GroupsFound, string CurrentFile);

/// <summary>
/// Поиск точных дубликатов в три этапа:
/// 1) группировка по размеру (файлы уникального размера сразу отбрасываются);
/// 2) быстрый хеш первых и последних 64 КБ;
/// 3) полный SHA-256 только для совпавших по быстрому хешу.
/// Полные хеши кэшируются (путь + размер + дата изменения), повторный анализ не перечитывает файлы.
/// </summary>
public sealed partial class DuplicateFinder
{
    private const int PartialBlock = 64 * 1024;
    private readonly IHashCache? _cache;
    public int MaxParallelism { get; init; } = Math.Clamp(Environment.ProcessorCount / 2, 1, 4);
    /// <summary>Файлы меньше этого размера не рассматриваются (пустые файлы дубликатами не считаем).</summary>
    public long MinSize { get; init; } = 1;
    public List<ScanError> Errors { get; } = new();

    public DuplicateFinder(IHashCache? cache = null) { _cache = cache; }

    public List<DuplicateGroup> Find(IReadOnlyList<FileEntry> files, IProgress<DuplicateProgress>? progress = null, CancellationToken ct = default)
    {
        var errors = new ConcurrentBag<ScanError>();
        var po = new ParallelOptions { MaxDegreeOfParallelism = MaxParallelism, CancellationToken = ct };
        try
        {
            // Этап 1: только файлы, размер которых встречается больше одного раза.
            var sameSize = files.Where(f => f.Size >= MinSize)
                .GroupBy(f => f.Size)
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            // Этап 2: быстрый хеш начала и конца — только для крупных файлов (мелкие сразу хешируются целиком).
            var big = sameSize.Where(f => f.Size > PartialBlock * 2).ToList();
            var partial = new ConcurrentDictionary<FileEntry, string>();
            Parallel.ForEach(big, po, f =>
            {
                var h = TryHash(f, partial: true, errors);
                if (h != null) partial[f] = h;
            });
            var candidates = sameSize
                .Where(f => f.Size <= PartialBlock * 2 || partial.ContainsKey(f))
                .GroupBy(f => (f.Size, partial.TryGetValue(f, out var ph) ? ph : ""))
                .Where(g => g.Count() > 1)
                .SelectMany(g => g)
                .ToList();

            // Этап 3: полный SHA-256.
            int total = candidates.Count, hashed = 0;
            var full = new ConcurrentDictionary<FileEntry, string>();
            Parallel.ForEach(candidates, po, f =>
            {
                var h = TryHash(f, partial: false, errors);
                if (h != null) full[f] = h;
                var n = Interlocked.Increment(ref hashed);
                if (n % 200 == 0) progress?.Report(new DuplicateProgress(n, total, 0, f.FullPath));
            });

            var groups = full
                .GroupBy(kv => (kv.Key.Size, kv.Value))
                .Where(g => g.Count() > 1)
                .Select(g =>
                {
                    var dg = new DuplicateGroup { Hash = g.Key.Value, Size = g.Key.Size, Files = g.Select(kv => kv.Key).OrderBy(x => x.FullPath, StringComparer.OrdinalIgnoreCase).ToList() };
                    Recommend(dg);
                    return dg;
                })
                .OrderByDescending(g => g.RedundantBytes)
                .ThenBy(g => g.Files[0].FullPath, StringComparer.OrdinalIgnoreCase)
                .ToList();
            progress?.Report(new DuplicateProgress(total, total, groups.Count, ""));
            return groups;
        }
        finally
        {
            _cache?.Flush();
            Errors.AddRange(errors);
        }
    }

    private string? TryHash(FileEntry f, bool partial, ConcurrentBag<ScanError> errors)
    {
        try
        {
            if (!partial && _cache != null && _cache.TryGet(f.FullPath, f.Size, f.LastWriteTimeUtc, out var cached)) return cached;
            var h = partial ? PartialHash(f.FullPath, f.Size) : FullHash(f.FullPath);
            if (!partial) _cache?.Set(f.FullPath, f.Size, f.LastWriteTimeUtc, h);
            return h;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            errors.Add(new ScanError(f.FullPath, ex is UnauthorizedAccessException ? ScanErrorKind.AccessDenied : ScanErrorKind.IoError, ex.Message));
            return null;
        }
    }

    public static string FullHash(string path)
    {
        // Без внутреннего буфера FileStream: читаем блоками из пула (важно для сотен тысяч мелких файлов).
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 0, FileOptions.SequentialScan);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buf = System.Buffers.ArrayPool<byte>.Shared.Rent(1 << 20);
        try
        {
            int n;
            while ((n = fs.Read(buf, 0, buf.Length)) > 0) hash.AppendData(buf, 0, n);
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buf);
        }
        return Convert.ToHexString(hash.GetHashAndReset());
    }

    private static string PartialHash(string path, long size)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 0);
        var buf = new byte[PartialBlock * 2];
        int n = fs.Read(buf, 0, PartialBlock);
        if (size > PartialBlock * 2)
        {
            fs.Seek(-PartialBlock, SeekOrigin.End);
            n += fs.Read(buf, PartialBlock, PartialBlock);
        }
        return Convert.ToHexString(SHA256.HashData(buf.AsSpan(0, n)));
    }

    [GeneratedRegex(@"(\(\d+\)|\bcopy\b|копия|\s-\s?копия|_copy|\bdup\b)", RegexOptions.IgnoreCase)]
    private static partial Regex CopyMarker();

    private static readonly string[] TempLikeFolders = { "downloads", "загрузки", "temp", "tmp", "telegram desktop", "whatsapp", "дубликаты", "duplicates", "копии" };

    /// <summary>
    /// Рекомендация, какой экземпляр оставить: имя без признаков копии, не в «Загрузках»/временной папке,
    /// более короткий путь, при равенстве — более старый (вероятно, оригинал).
    /// </summary>
    public static void Recommend(DuplicateGroup g)
    {
        int Score(FileEntry f)
        {
            int s = 0;
            if (CopyMarker().IsMatch(f.NameWithoutExtension)) s += 100;
            var dirLower = f.DirectoryPath.ToLowerInvariant();
            if (TempLikeFolders.Any(t => dirLower.Contains(Path.DirectorySeparatorChar + t))) s += 50;
            s += f.FullPath.Count(c => c == Path.DirectorySeparatorChar);
            return s;
        }
        var best = g.Files.OrderBy(Score).ThenBy(f => f.LastWriteTimeUtc).ThenBy(f => f.FullPath.Length).First();
        g.Recommended = best;
        var reasons = new List<string>();
        if (!CopyMarker().IsMatch(best.NameWithoutExtension) && g.Files.Any(f => CopyMarker().IsMatch(f.NameWithoutExtension)))
            reasons.Add("имя без признаков копии");
        var bestDir = best.DirectoryPath.ToLowerInvariant();
        if (!TempLikeFolders.Any(t => bestDir.Contains(Path.DirectorySeparatorChar + t)))
            reasons.Add("лежит не в «Загрузках» и не во временной папке");
        if (g.Files.All(f => f.LastWriteTimeUtc >= best.LastWriteTimeUtc)) reasons.Add("самая ранняя дата изменения");
        g.RecommendationReason = reasons.Count > 0 ? string.Join(", ", reasons) : "самый короткий путь";
    }
}
