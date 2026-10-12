using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Rules;

public sealed record RuleMatch(Rule Rule, string? TargetFolder);

/// <summary>Применяет пользовательские правила по приоритету; срабатывает первое подходящее.</summary>
public sealed class RulesEngine
{
    private readonly List<Rule> _rules;

    public RulesEngine(IEnumerable<Rule> rules)
    {
        _rules = rules.Where(r => r.Enabled).OrderBy(r => r.Priority).ThenBy(r => r.Id).ToList();
    }

    public IReadOnlyList<Rule> Rules => _rules;

    public RuleMatch? Match(FileEntry f, DateTime? nowUtc = null)
    {
        foreach (var r in _rules)
        {
            if (r.Action == RuleActionKind.Tag) continue;
            if (r.IsMatch(f, nowUtc))
                return new RuleMatch(r, r.Action == RuleActionKind.MoveTo ? r.ResolveTarget(f) : null);
        }
        return null;
    }

    public IEnumerable<string> MatchTags(FileEntry f, DateTime? nowUtc = null) =>
        _rules.Where(r => r.Action == RuleActionKind.Tag && r.IsMatch(f, nowUtc)).Select(r => r.Target);

    /// <summary>Примеры правил, показываемые в конструкторе.</summary>
    public static List<Rule> Examples() => new()
    {
        new Rule
        {
            Name = "Счета в PDF", Priority = 1,
            Conditions = { new() { Field = RuleField.Extension, Operator = RuleOperator.Equals, Value = "pdf" },
                           new() { Field = RuleField.Name, Operator = RuleOperator.Contains, Value = "счет" } },
            Action = RuleActionKind.MoveTo, Target = "Документы/Счета",
        },
        new Rule
        {
            Name = "Старые фото в архив", Priority = 2,
            Conditions = { new() { Field = RuleField.Extension, Operator = RuleOperator.Equals, Value = "jpg" },
                           new() { Field = RuleField.Modified, Operator = RuleOperator.Before, Value = "2024" } },
            Action = RuleActionKind.MoveTo, Target = "Фото/Архив/{year}",
        },
    };
}
