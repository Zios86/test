using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Classification;

public enum ClassificationSource { FileType, FileName, Neighbors, Content, AI, UserRule }

/// <summary>Результат умной классификации: куда предлагается положить файл, почему и насколько уверенно.</summary>
public sealed class ClassificationResult
{
    public required string Topic { get; init; }
    /// <summary>Относительный путь целевой папки (относительно корня сортировки).</summary>
    public required string TargetFolder { get; init; }
    public double Confidence { get; init; }
    public ClassificationSource Source { get; init; }
    public List<string> Reasons { get; init; } = new();

    public string Explanation => string.Join("; ", Reasons) + ".";
}

/// <summary>
/// Умная классификация с приоритетом источников:
/// 1) простые правила (тип файла), 2) метаданные, 3) имя файла, 4) содержимое, 5) ИИ.
/// ИИ вызывается только при низкой уверенности остальных методов и только если включён.
/// </summary>
public sealed class SmartClassifier
{
    private readonly List<TopicRule> _rules;

    public SmartClassifier(List<TopicRule>? rules = null) { _rules = rules ?? TopicRule.Defaults(); }

    public IReadOnlyList<TopicRule> Rules => _rules;

    public List<TopicRule> MatchName(FileEntry f)
    {
        var norm = TextNormalizer.Normalize(TextNormalizer.SplitWords(f.NameWithoutExtension));
        var normRaw = TextNormalizer.Normalize(f.NameWithoutExtension);
        return _rules.Where(r => (r.Categories.Length == 0 || r.Categories.Contains(f.Category))
                                 && r.NamePatterns.Any(p => p.IsMatch(norm) || p.IsMatch(normRaw))).ToList();
    }

    public TopicRule? MatchContent(FileEntry f, string text, out int hits)
    {
        hits = 0;
        var norm = TextNormalizer.Normalize(text);
        TopicRule? best = null;
        foreach (var r in _rules)
        {
            if (r.Categories.Length > 0 && !r.Categories.Contains(f.Category)) continue;
            int h = r.NamePatterns.Sum(p => p.Matches(norm).Count);
            if (h > hits) { hits = h; best = r; }
        }
        return best;
    }

    /// <summary>Классификация без чтения содержимого: тип + имя + соседние файлы.</summary>
    /// <param name="neighbors">Другие файлы той же папки (для контекста).</param>
    public ClassificationResult Classify(FileEntry f, IReadOnlyList<FileEntry>? neighbors = null)
    {
        var matches = MatchName(f);
        if (matches.Count > 0)
        {
            var rule = matches[0];
            var reasons = new List<string>
            {
                $"название содержит ключевые слова темы «{rule.Topic}» ({rule.Keywords})",
                $"тип файла: {f.Category.DisplayName()} ({(f.Extension.Length > 0 ? f.Extension.ToUpperInvariant() : "без расширения")})",
            };
            double conf = 0.8;
            if (neighbors != null)
            {
                int same = neighbors.Count(n => !ReferenceEquals(n, f) && MatchName(n).Any(r => r.Topic == rule.Topic));
                if (same > 0)
                {
                    conf += Math.Min(0.15, 0.05 * same + 0.05);
                    reasons.Add($"в этой папке обнаружены ещё {same} файл(а) этой темы");
                }
            }
            if (matches.Count > 1)
            {
                conf -= 0.2;
                reasons.Add($"файл подходит и под другие темы: {string.Join(", ", matches.Skip(1).Select(m => m.Topic))}");
            }
            return new ClassificationResult
            {
                Topic = rule.Topic, TargetFolder = rule.Folder, Confidence = Math.Clamp(conf, 0, 0.97),
                Source = ClassificationSource.FileName, Reasons = reasons,
            };
        }

        return ByType(f);
    }

    /// <summary>Только по типу файла — точное правило, высокая уверенность.</summary>
    public static ClassificationResult ByType(FileEntry f)
    {
        var folder = f.Category.DisplayName();
        return new ClassificationResult
        {
            Topic = folder,
            TargetFolder = folder,
            Confidence = f.Category == FileCategory.Other ? 0.5 : 0.95,
            Source = ClassificationSource.FileType,
            Reasons =
            {
                f.Category == FileCategory.Other
                    ? $"тип файла не распознан ({(f.Extension.Length > 0 ? "." + f.Extension : "без расширения")})"
                    : $"расширение .{f.Extension} относится к категории «{folder}»",
            },
        };
    }

    /// <summary>
    /// Полная классификация с анализом содержимого и (опционально) ИИ.
    /// Содержимое читается только если по имени тема не определена.
    /// </summary>
    public async Task<ClassificationResult> ClassifyDeepAsync(FileEntry f, IReadOnlyList<FileEntry>? neighbors, IAIClassifier? ai, double aiThreshold, CancellationToken ct)
    {
        var basic = Classify(f, neighbors);
        if (basic.Source == ClassificationSource.FileName && basic.Confidence >= aiThreshold) return basic;
        if (!ContentExtractor.CanExtract(f.Extension)) return basic;

        var text = ContentExtractor.TryExtract(f.FullPath);
        if (!string.IsNullOrWhiteSpace(text))
        {
            var rule = MatchContent(f, text, out var hits);
            if (rule != null && hits >= 2)
            {
                var conf = Math.Min(0.8, 0.6 + 0.05 * (hits - 2));
                if (conf > (basic.Source == ClassificationSource.FileName ? basic.Confidence : 0))
                {
                    basic = new ClassificationResult
                    {
                        Topic = rule.Topic, TargetFolder = rule.Folder, Confidence = conf, Source = ClassificationSource.Content,
                        Reasons = { $"в тексте документа {hits} раз(а) встречаются ключевые слова темы «{rule.Topic}»", $"тип файла: {f.Category.DisplayName()}" },
                    };
                }
            }
        }

        // Тема по имени/содержимому не определена (или определена неуверенно) — последний уровень: ИИ.
        bool topicKnown = basic.Source is ClassificationSource.FileName or ClassificationSource.Content;
        if (ai != null && basic.Source != ClassificationSource.Content && (!topicKnown || basic.Confidence < aiThreshold))
        {
            try
            {
                var r = await ai.ClassifyAsync(f.Name, text, ct).ConfigureAwait(false);
                if (r != null && r.Confidence >= 0.6 && (!topicKnown || r.Confidence > basic.Confidence)
                    && !string.Equals(r.DocumentType, "Прочее", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = new List<string> { Infrastructure.PathUtil.SanitizeFileName(r.DocumentType) };
                    if (r.Project != null) parts.Add(Infrastructure.PathUtil.SanitizeFileName(r.Project));
                    if (r.Year != null) parts.Add(r.Year.Value.ToString());
                    basic = new ClassificationResult
                    {
                        Topic = r.DocumentType, TargetFolder = string.Join("/", parts), Confidence = Math.Min(r.Confidence, 0.8), Source = ClassificationSource.AI,
                        Reasons = { $"ИИ ({ai.Name}) определил: {r.DocumentType}{(r.Project != null ? " / " + r.Project : "")}{(r.Year != null ? " / " + r.Year : "")}" },
                    };
                }
            }
            catch (Exception) when (!ct.IsCancellationRequested)
            {
                // ИИ недоступен — остаёмся с результатом правил.
            }
        }
        return basic;
    }
}
