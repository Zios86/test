using System.Collections.ObjectModel;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Rules;

namespace FileOrganizer.App.ViewModels;

public sealed class ConditionViewModel : ObservableObject
{
    public static IReadOnlyList<Option<RuleField>> FieldOptions { get; } =
        Enum.GetValues<RuleField>().Select(f => new Option<RuleField>(f, RuleCondition.FieldName(f))).ToList();
    public static IReadOnlyList<Option<RuleOperator>> OperatorOptions { get; } =
        Enum.GetValues<RuleOperator>().Select(o => new Option<RuleOperator>(o, RuleCondition.OperatorName(o))).ToList();

    private RuleField _field;
    public RuleField Field { get => _field; set => Set(ref _field, value); }
    private RuleOperator _operator;
    public RuleOperator Operator { get => _operator; set => Set(ref _operator, value); }
    private string _value = "";
    public string Value { get => _value; set => Set(ref _value, value); }

    public RuleCondition ToModel() => new() { Field = Field, Operator = Operator, Value = Value };
    public static ConditionViewModel From(RuleCondition c) => new() { Field = c.Field, Operator = c.Operator, Value = c.Value };
}

/// <summary>Визуальный конструктор правила: ЕСЛИ условия ТО действие.</summary>
public sealed class RuleEditorViewModel : ObservableObject
{
    public RuleEditorViewModel(Rule rule)
    {
        Id = rule.Id;
        _name = rule.Name;
        _enabled = rule.Enabled;
        _priority = rule.Priority;
        _matchAll = rule.MatchAll;
        _action = rule.Action;
        _target = rule.Target;
        Conditions = new ObservableCollection<ConditionViewModel>(rule.Conditions.Select(ConditionViewModel.From));
        if (Conditions.Count == 0) Conditions.Add(new ConditionViewModel { Field = RuleField.Extension, Operator = RuleOperator.Equals });
        AddConditionCommand = new RelayCommand(() => Conditions.Add(new ConditionViewModel { Field = RuleField.Name, Operator = RuleOperator.Contains }));
        RemoveConditionCommand = new RelayCommand(p => { if (p is ConditionViewModel c && Conditions.Count > 1) Conditions.Remove(c); });
    }

    public long Id { get; set; }
    private string _name;
    public string Name { get => _name; set => Set(ref _name, value); }
    private bool _enabled;
    public bool Enabled { get => _enabled; set => Set(ref _enabled, value); }
    private int _priority;
    public int Priority { get => _priority; set => Set(ref _priority, value); }
    private bool _matchAll;
    public bool MatchAll { get => _matchAll; set => Set(ref _matchAll, value); }
    private RuleActionKind _action;
    public RuleActionKind Action { get => _action; set => Set(ref _action, value); }
    private string _target;
    public string Target { get => _target; set => Set(ref _target, value); }
    public ObservableCollection<ConditionViewModel> Conditions { get; }
    public ICommand AddConditionCommand { get; }
    public ICommand RemoveConditionCommand { get; }

    public static IReadOnlyList<Option<bool>> MatchOptions { get; } = new[] { new Option<bool>(true, "все условия (И)"), new Option<bool>(false, "любое условие (ИЛИ)") };
    public static IReadOnlyList<Option<RuleActionKind>> ActionOptions { get; } = new[]
    {
        new Option<RuleActionKind>(RuleActionKind.MoveTo, "переместить в папку"),
        new Option<RuleActionKind>(RuleActionKind.Tag, "присвоить тег"),
        new Option<RuleActionKind>(RuleActionKind.Skip, "не трогать"),
    };

    public Rule ToModel() => new()
    {
        Id = Id, Name = Name, Enabled = Enabled, Priority = Priority, MatchAll = MatchAll, Action = Action, Target = Target,
        Conditions = Conditions.Select(c => c.ToModel()).ToList(),
    };

    public string Description => ToModel().Description;
}

public sealed class ExclusionRow : ObservableObject
{
    public static IReadOnlyList<Option<ExclusionKind>> Kinds { get; } = new[]
    {
        new Option<ExclusionKind>(ExclusionKind.Folder, "Папка (имя или полный путь)"),
        new Option<ExclusionKind>(ExclusionKind.Extension, "Тип файла (расширение)"),
        new Option<ExclusionKind>(ExclusionKind.File, "Отдельный файл (полный путь)"),
        new Option<ExclusionKind>(ExclusionKind.Mask, "Маска (*.tmp, *.log)"),
    };
    private ExclusionKind _kind;
    public ExclusionKind Kind { get => _kind; set => Set(ref _kind, value); }
    private string _pattern = "";
    public string Pattern { get => _pattern; set => Set(ref _pattern, value); }
}

public sealed class CategoryRow : ObservableObject
{
    public FileCategory Category { get; init; }
    public string Name => Category.DisplayName();
    private string _extensions = "";
    public string Extensions { get => _extensions; set => Set(ref _extensions, value); }
}

/// <summary>Настройки: режим, тема, безопасность, ИИ, профили, исключения, правила, типы файлов, папки «Разобрать».</summary>
public sealed class SettingsViewModel : PageViewModel
{
    private AppSettings S => AppServices.Settings;

    public SettingsViewModel(MainViewModel shell) : base(shell)
    {
        Profiles = new ObservableCollection<Profile>(AppServices.Db.GetProfiles());
        SaveCommand = new RelayCommand(Save);
        AddProfileCommand = new RelayCommand(AddProfile);
        DeleteProfileCommand = new RelayCommand(DeleteProfile, () => Profiles.Count > 1);
        AddExclusionCommand = new RelayCommand(() => Exclusions.Add(new ExclusionRow { Kind = ExclusionKind.Mask, Pattern = "*.tmp" }));
        RemoveExclusionCommand = new RelayCommand(p => { if (p is ExclusionRow r) Exclusions.Remove(r); });
        NewRuleCommand = new RelayCommand(() => { var r = new RuleEditorViewModel(new Rule { Priority = Rules.Count + 1 }); Rules.Add(r); SelectedRule = r; });
        AddExampleRulesCommand = new RelayCommand(() => { foreach (var r in RulesEngine.Examples()) Rules.Add(new RuleEditorViewModel(r)); });
        DeleteRuleCommand = new RelayCommand(() =>
        {
            if (SelectedRule == null) return;
            if (SelectedRule.Id != 0) AppServices.Db.DeleteRule(SelectedRule.Id);
            Rules.Remove(SelectedRule);
            SelectedRule = Rules.FirstOrDefault();
        }, () => SelectedRule != null);
        ResetCategoriesCommand = new RelayCommand(() => { S.CustomCategories = null; LoadCategories(); });
        AddInboxCommand = new RelayCommand(AddInbox);
        RemoveInboxCommand = new RelayCommand(p => { if (p is InboxRow r) { AppServices.Db.RemoveWatchedFolder(r.Id); LoadInbox(); } });
        CheckInboxCommand = new RelayCommand(() => { Shell.CheckInbox(); if (Shell.InboxMessage == null) AppServices.Info("Новых файлов в папках «Разобрать» нет."); else Shell.Navigate(Shell.Dashboard); });
        OpenLogsCommand = new RelayCommand(() => { System.IO.Directory.CreateDirectory(AppPaths.LogDirectory); AppServices.OpenInExplorer(AppPaths.LogDirectory); });
        ClearHashCacheCommand = new RelayCommand(() => { AppServices.Db.ClearHashCache(); AppServices.Info("Кэш хешей очищен."); });

        _selectedProfile = Profiles.FirstOrDefault(p => p.IsActive) ?? Profiles.First();
        LoadProfileData();
        LoadCategories();
        LoadInbox();
    }

    public override string Title => "Настройки";
    public override string Icon => "";

    private int _selectedTab;
    public int SelectedTab { get => _selectedTab; set => Set(ref _selectedTab, value); }

    // ---- Общие ----
    public bool SimulationMode { get => Shell.SimulationMode; set { Shell.SimulationMode = value; OnPropertyChanged(); } }
    public static IReadOnlyList<Option<AppTheme>> Themes { get; } = new[] { new Option<AppTheme>(AppTheme.System, "Как в Windows"), new Option<AppTheme>(AppTheme.Light, "Светлая"), new Option<AppTheme>(AppTheme.Dark, "Тёмная") };
    public AppTheme Theme { get => S.Theme; set { S.Theme = value; ThemeManager.Apply(value); AppServices.SaveSettings(); OnPropertyChanged(); } }
    public static IReadOnlyList<Option<DeleteMode>> DeleteModes { get; } = new[]
    {
        new Option<DeleteMode>(DeleteMode.RecoveryFolder, "В папку FileOrganizer_Recovery (рекомендуется, отмена из программы)"),
        new Option<DeleteMode>(DeleteMode.RecycleBin, "В Корзину Windows (восстановление — из Корзины)"),
    };
    public DeleteMode DeleteMode { get => S.DeleteMode; set { S.DeleteMode = value; OnPropertyChanged(); } }
    public bool IncludeHidden { get => S.IncludeHidden; set { S.IncludeHidden = value; OnPropertyChanged(); } }
    public bool TopLevelOnly { get => S.TopLevelOnly; set { S.TopLevelOnly = value; OnPropertyChanged(); } }
    public int OldFilesDays { get => S.OldFilesDays; set { S.OldFilesDays = Math.Max(1, value); OnPropertyChanged(); } }
    public int LargeFileMb { get => S.LargeFileMb; set { S.LargeFileMb = Math.Max(1, value); OnPropertyChanged(); } }
    public int AutoSelectPercent { get => (int)Math.Round(S.AutoSelectThreshold * 100); set { S.AutoSelectThreshold = Math.Clamp(value, 50, 100) / 100.0; OnPropertyChanged(); } }

    // ---- ИИ ----
    public bool ContentAnalysisEnabled { get => S.ContentAnalysisEnabled; set { S.ContentAnalysisEnabled = value; OnPropertyChanged(); } }
    public bool AIEnabled { get => S.AIEnabled; set { S.AIEnabled = value; OnPropertyChanged(); } }
    public string AIEndpoint { get => S.AIEndpoint; set { S.AIEndpoint = value; OnPropertyChanged(); } }
    public string AIModel { get => S.AIModel; set { S.AIModel = value; OnPropertyChanged(); } }
    public bool AllowRemoteAI
    {
        get => S.AllowRemoteAI;
        set
        {
            if (value && !AppServices.Confirm("Разрешить отправку имени файла и короткого фрагмента текста документа во внешний ИИ-сервис?\n\nБез этого разрешения используется только локальная модель (например, Ollama на этом компьютере).", "Согласие на передачу данных"))
            { OnPropertyChanged(); return; }
            S.AllowRemoteAI = value;
            OnPropertyChanged();
        }
    }

    // ---- Профили ----
    public ObservableCollection<Profile> Profiles { get; }
    private Profile _selectedProfile;
    public Profile SelectedProfile
    {
        get => _selectedProfile;
        set
        {
            if (value == null || !Set(ref _selectedProfile, value)) return;
            AppServices.Db.SetActiveProfile(value.Id);
            LoadProfileData();
        }
    }
    private string _newProfileName = "";
    public string NewProfileName { get => _newProfileName; set => Set(ref _newProfileName, value); }

    public ObservableCollection<ExclusionRow> Exclusions { get; } = new();
    public ObservableCollection<RuleEditorViewModel> Rules { get; } = new();
    private RuleEditorViewModel? _selectedRule;
    public RuleEditorViewModel? SelectedRule { get => _selectedRule; set => Set(ref _selectedRule, value); }

    public ObservableCollection<CategoryRow> CategoryRows { get; } = new();
    public ObservableCollection<InboxRow> Inbox { get; } = new();

    public string DataFolder => AppPaths.DataDirectory;

    public ICommand SaveCommand { get; }
    public ICommand AddProfileCommand { get; }
    public ICommand DeleteProfileCommand { get; }
    public ICommand AddExclusionCommand { get; }
    public ICommand RemoveExclusionCommand { get; }
    public ICommand NewRuleCommand { get; }
    public ICommand AddExampleRulesCommand { get; }
    public ICommand DeleteRuleCommand { get; }
    public ICommand ResetCategoriesCommand { get; }
    public ICommand AddInboxCommand { get; }
    public ICommand RemoveInboxCommand { get; }
    public ICommand CheckInboxCommand { get; }
    public ICommand OpenLogsCommand { get; }
    public ICommand ClearHashCacheCommand { get; }

    private void LoadProfileData()
    {
        Exclusions.ReplaceWith(AppServices.Db.GetExclusions(SelectedProfile.Id).Items.Select(e => new ExclusionRow { Kind = e.Kind, Pattern = e.Pattern }));
        Rules.ReplaceWith(AppServices.Db.GetRules(SelectedProfile.Id).Select(r => new RuleEditorViewModel(r)));
        SelectedRule = Rules.FirstOrDefault();
    }

    private void LoadCategories()
    {
        var classifier = AppServices.CreateClassifier();
        CategoryRows.ReplaceWith(Enum.GetValues<FileCategory>().Where(c => c != FileCategory.Other).Select(c => new CategoryRow
        {
            Category = c,
            Extensions = string.Join(", ", classifier.Map.Where(kv => kv.Value == c).Select(kv => kv.Key).OrderBy(x => x)),
        }));
    }

    private void LoadInbox() =>
        Inbox.ReplaceWith(AppServices.Db.GetWatchedFolders().Select(w => new InboxRow(w.Id, w.Path, w.LastChecked)));

    private void AddProfile()
    {
        var name = NewProfileName.Trim();
        if (name.Length == 0) { AppServices.Info("Введите название профиля."); return; }
        if (Profiles.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase))) { AppServices.Info("Такой профиль уже есть."); return; }
        AppServices.Db.CreateProfile(name);
        Profiles.ReplaceWith(AppServices.Db.GetProfiles());
        SelectedProfile = Profiles.First(p => p.Name == name);
        NewProfileName = "";
    }

    private void DeleteProfile()
    {
        if (!AppServices.Confirm($"Удалить профиль «{SelectedProfile.Name}» вместе с его правилами и исключениями? Файлы не затрагиваются.")) return;
        AppServices.Db.DeleteProfile(SelectedProfile.Id);
        Profiles.ReplaceWith(AppServices.Db.GetProfiles());
        _selectedProfile = Profiles.First(p => p.IsActive);
        OnPropertyChanged(nameof(SelectedProfile));
        LoadProfileData();
    }

    private void AddInbox()
    {
        var f = AppServices.PickFolder();
        if (f == null) return;
        AppServices.Db.AddWatchedFolder(f);
        LoadInbox();
    }

    private void Save()
    {
        // Исключения
        var set = new ExclusionSet { Items = Exclusions.Where(e => !string.IsNullOrWhiteSpace(e.Pattern)).Select(e => new Exclusion(e.Kind, e.Pattern.Trim())).ToList() };
        AppServices.Db.SaveExclusions(SelectedProfile.Id, set);

        // Правила
        foreach (var r in Rules)
        {
            var model = r.ToModel();
            if (model.Action != RuleActionKind.Skip && string.IsNullOrWhiteSpace(model.Target))
            {
                AppServices.Info($"В правиле «{model.Name}» не указана папка (или тег).");
                SelectedTab = 2;
                return;
            }
            r.Id = AppServices.Db.SaveRule(SelectedProfile.Id, model);
        }

        // Типы файлов
        var custom = new Dictionary<string, string[]>();
        foreach (var c in CategoryRows)
            custom[c.Category.ToString()] = c.Extensions.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim().TrimStart('*', '.').ToLowerInvariant()).Distinct().ToArray();
        bool same = CategoryRows.All(c => FileClassifier.DefaultMap.TryGetValue(c.Category, out var d) && d.OrderBy(x => x).SequenceEqual(custom[c.Category.ToString()].OrderBy(x => x)));
        S.CustomCategories = same ? null : custom;

        AppServices.SaveSettings();
        AppServices.Info("Настройки сохранены. Новые правила, исключения и типы файлов будут учтены при следующем анализе.");
    }
}

public sealed record InboxRow(long Id, string Path, DateTime? LastChecked)
{
    public string LastCheckedText => LastChecked == null ? "ещё не разбиралась" : "разобрана " + Format.Date(LastChecked.Value);
}
