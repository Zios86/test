using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using FileOrganizer.App.Mvvm;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;
using FileOrganizer.Core.Rules;
using FileOrganizer.Core.Scanning;

namespace FileOrganizer.App.ViewModels;

public enum WorkMode { Quick, Smart, Deep, Manual }

/// <summary>
/// Главная модель: выбранная папка, анализ, общий план изменений («корзина» предложений), навигация.
/// Ничего не меняется на диске без экрана «Будет выполнено» и подтверждения.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    private CancellationTokenSource? _cts;
    private readonly DispatcherTimer _inboxTimer;

    public MainViewModel()
    {
        Dashboard = new DashboardViewModel(this);
        Structure = new StructureViewModel(this);
        Plan = new PlanViewModel(this);
        Duplicates = new DuplicatesViewModel(this);
        Space = new SpaceViewModel(this);
        OldFiles = new OldFilesViewModel(this);
        EmptyFolders = new EmptyFoldersViewModel(this);
        Names = new NamesViewModel(this);
        SimilarFolders = new SimilarFoldersViewModel(this);
        Misplaced = new MisplacedViewModel(this);
        TempFiles = new TempFilesViewModel(this);
        Projects = new ProjectsViewModel(this);
        Search = new SearchViewModel(this);
        History = new HistoryViewModel(this);
        Settings = new SettingsViewModel(this);

        Pages = new ObservableCollection<PageViewModel>
        {
            Dashboard, Structure, Plan, Duplicates, Space, OldFiles, EmptyFolders, Names, SimilarFolders, Misplaced, TempFiles, Projects, Search, History, Settings,
        };
        _currentPage = Dashboard;

        Folder = AppServices.Settings.LastFolder ?? "";
        PickFolderCommand = new RelayCommand(PickFolder, () => !IsBusy);
        AnalyzeCommand = new AsyncCommand(() => AnalyzeAsync(WorkMode.Smart), CanRun);
        TidyUpCommand = new AsyncCommand(TidyUpAsync, CanRun);
        QuickCleanupCommand = new AsyncCommand(() => RunModeAsync(WorkMode.Quick), CanRun);
        SmartSortCommand = new AsyncCommand(() => RunModeAsync(WorkMode.Smart), CanRun);
        DeepAnalysisCommand = new AsyncCommand(() => RunModeAsync(WorkMode.Deep), CanRun);
        ManualModeCommand = new RelayCommand(() => { Settings.SelectedTab = 2; Navigate(Settings); });
        StopCommand = new RelayCommand(() => _cts?.Cancel(), () => IsBusy);
        NavigateCommand = new RelayCommand(p => { if (p is PageViewModel page) Navigate(page); });
        UndoLastCommand = new AsyncCommand(History.UndoLastAsync, () => !IsBusy);
        GoFindDuplicates = new AsyncCommand(() => AnalyzeAndShow(Duplicates), CanRun);
        GoEmptyFolders = new AsyncCommand(() => AnalyzeAndShow(EmptyFolders), CanRun);
        GoLargeFiles = new AsyncCommand(() => AnalyzeAndShow(Space), CanRun);
        GoOldFiles = new AsyncCommand(() => AnalyzeAndShow(OldFiles), CanRun);
        ShowErrorsCommand = new RelayCommand(ShowErrors, () => ErrorCount > 0);
        DisableSimulationCommand = new RelayCommand(() => SimulationMode = false);

        _inboxTimer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(Math.Max(5, AppServices.Settings.InboxCheckMinutes)) };
        _inboxTimer.Tick += (_, _) => CheckInbox();
        _inboxTimer.Start();
    }

    // ---------------- Страницы ----------------
    public DashboardViewModel Dashboard { get; }
    public StructureViewModel Structure { get; }
    public PlanViewModel Plan { get; }
    public DuplicatesViewModel Duplicates { get; }
    public SpaceViewModel Space { get; }
    public OldFilesViewModel OldFiles { get; }
    public EmptyFoldersViewModel EmptyFolders { get; }
    public NamesViewModel Names { get; }
    public SimilarFoldersViewModel SimilarFolders { get; }
    public MisplacedViewModel Misplaced { get; }
    public TempFilesViewModel TempFiles { get; }
    public ProjectsViewModel Projects { get; }
    public SearchViewModel Search { get; }
    public HistoryViewModel History { get; }
    public SettingsViewModel Settings { get; }
    public ObservableCollection<PageViewModel> Pages { get; }

    private PageViewModel _currentPage;
    public PageViewModel CurrentPage
    {
        get => _currentPage;
        set
        {
            if (Set(ref _currentPage, value))
            {
                value.OnNavigatedTo();
                OnPropertyChanged(nameof(ShowNoReportHint));
            }
        }
    }

    /// <summary>Подсказка «сначала выполните анализ» на страницах, которым нужны его результаты.</summary>
    public bool ShowNoReportHint => !HasReport && CurrentPage is not (DashboardViewModel or HistoryViewModel or SettingsViewModel or PlanViewModel);

    public void Navigate(PageViewModel page) => CurrentPage = page;

    // ---------------- Состояние ----------------
    private string _folder = "";
    public string Folder
    {
        get => _folder;
        set
        {
            if (Set(ref _folder, value))
            {
                OnPropertyChanged(nameof(HasFolder));
                OnPropertyChanged(nameof(FolderName));
            }
        }
    }
    public bool HasFolder => !string.IsNullOrWhiteSpace(Folder);
    public string FolderName => HasFolder ? Path.GetFileName(Path.TrimEndingDirectorySeparator(Folder)) is { Length: > 0 } n ? n : Folder : "Папка не выбрана";

    private AnalysisReport? _report;
    public AnalysisReport? Report
    {
        get => _report;
        private set
        {
            if (Set(ref _report, value))
            {
                OnPropertyChanged(nameof(HasReport));
                OnPropertyChanged(nameof(ErrorCount));
                OnPropertyChanged(nameof(ShowNoReportHint));
                foreach (var p in Pages) p.OnReportChanged();
            }
        }
    }
    public bool HasReport => Report != null;
    public int ErrorCount => Report?.Scan.Errors.Count ?? 0;

    /// <summary>Результаты глубокого анализа содержимого (путь → классификация).</summary>
    public Dictionary<string, ClassificationResult>? DeepResults { get; private set; }

    private bool _isBusy;
    public bool IsBusy { get => _isBusy; private set { if (Set(ref _isBusy, value)) CommandManager.InvalidateRequerySuggested(); } }

    private string _progressTitle = "";
    public string ProgressTitle { get => _progressTitle; set => Set(ref _progressTitle, value); }
    private string _progressText = "";
    public string ProgressText { get => _progressText; set => Set(ref _progressText, value); }
    private string _progressDetail = "";
    public string ProgressDetail { get => _progressDetail; set => Set(ref _progressDetail, value); }
    private double _progressValue;
    public double ProgressValue { get => _progressValue; set => Set(ref _progressValue, value); }
    private bool _progressIndeterminate = true;
    public bool ProgressIndeterminate { get => _progressIndeterminate; set => Set(ref _progressIndeterminate, value); }

    public bool SimulationMode
    {
        get => AppServices.Settings.SimulationMode;
        set
        {
            if (AppServices.Settings.SimulationMode == value) return;
            if (!value && !AppServices.Confirm(
                    "Отключить режим «Только посмотреть»?\n\nПосле этого программа сможет перемещать и переименовывать файлы — " +
                    "но только после экрана «Будет выполнено» и вашего подтверждения. Все изменения журналируются и могут быть отменены.",
                    "Режим «Только посмотреть»")) { OnPropertyChanged(); return; }
            AppServices.Settings.SimulationMode = value;
            AppServices.SaveSettings();
            OnPropertyChanged();
        }
    }

    private string? _inboxMessage;
    public string? InboxMessage { get => _inboxMessage; set => Set(ref _inboxMessage, value); }
    public string? InboxFolder { get; private set; }

    // ---------------- Общий план ----------------
    private PlanBuilder? _builder;
    public PlanBuilder? Builder => _builder;
    public event EventHandler? PlanChanged;

    public PlanBuilder EnsureBuilder()
    {
        if (_builder == null || !PathUtil.SamePath(_builder.Plan.RootPath, Report?.Scan.RootPath ?? Folder))
        {
            _builder = new PlanBuilder(Report?.Scan.RootPath ?? Folder, title: $"Организация папки {FolderName}")
            {
                AutoSelectThreshold = AppServices.Settings.AutoSelectThreshold,
            };
        }
        return _builder;
    }

    public void ResetPlan()
    {
        _builder = null;
        NotifyPlanChanged();
    }

    public void NotifyPlanChanged()
    {
        PlanChanged?.Invoke(this, EventArgs.Empty);
        OnPropertyChanged(nameof(PlanCount));
    }

    public void NotifyPlanCountChanged() => OnPropertyChanged(nameof(PlanCount));

    public int PlanCount => _builder?.Plan.Operations.Count(o => o.Selected && o.Kind != OperationKind.CreateFolder) ?? 0;

    /// <summary>Добавить предложения в план и показать экран «Будет выполнено».</summary>
    public void AddToPlan(Action<PlanBuilder> add, bool navigate = true)
    {
        if (Report == null) { AppServices.Info("Сначала выполните анализ папки."); return; }
        var b = EnsureBuilder();
        int before = b.Plan.Operations.Count;
        add(b);
        // Действия, добавленные пользователем вручную, отмечены сразу.
        foreach (var op in b.Plan.Operations.Skip(before)) if (op.Kind != OperationKind.CreateFolder && !(op.HasConflict && op.Resolution == ConflictResolution.Skip)) b.SetSelected(op, true);
        NotifyPlanChanged();
        if (b.Plan.Operations.Count == before) { AppServices.Info("Новых действий нет: для этих файлов действия уже запланированы или не требуются."); return; }
        if (navigate) Navigate(Plan);
    }

    // ---------------- Команды ----------------
    public ICommand PickFolderCommand { get; }
    public ICommand AnalyzeCommand { get; }
    public ICommand TidyUpCommand { get; }
    public ICommand QuickCleanupCommand { get; }
    public ICommand SmartSortCommand { get; }
    public ICommand DeepAnalysisCommand { get; }
    public ICommand ManualModeCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand NavigateCommand { get; }
    public ICommand UndoLastCommand { get; }
    public ICommand GoFindDuplicates { get; }
    public ICommand GoEmptyFolders { get; }
    public ICommand GoLargeFiles { get; }
    public ICommand GoOldFiles { get; }
    public ICommand ShowErrorsCommand { get; }
    public ICommand DisableSimulationCommand { get; }

    private bool CanRun() => !IsBusy && HasFolder;

    private void PickFolder()
    {
        var f = AppServices.PickFolder(HasFolder ? Folder : null);
        if (f == null) return;
        SetFolder(f);
    }

    public void SetFolder(string f)
    {
        // Диск с данными (D:\) выбрать можно; системный диск целиком и системные папки — нельзя.
        var driveRoot = Path.GetPathRoot(f);
        bool isDriveRoot = driveRoot != null && PathUtil.SamePath(driveRoot, f);
        bool isSystemDrive = isDriveRoot && PathUtil.SamePath(driveRoot!, Path.GetPathRoot(Environment.SystemDirectory) ?? "");
        if (isSystemDrive || (!isDriveRoot && SystemPathGuard.IsProtectedFolder(f)))
        {
            AppServices.Info("Это системный диск или системная папка. Программа не анализирует и не изменяет системные каталоги (Windows, Program Files, AppData и т. п.). Выберите папку с вашими файлами.");
            return;
        }
        Folder = f;
        Report = null;
        DeepResults = null;
        ResetPlan();
        AppServices.Settings.LastFolder = f;
        AppServices.Settings.RecentFolders.Remove(f);
        AppServices.Settings.RecentFolders.Insert(0, f);
        if (AppServices.Settings.RecentFolders.Count > 10) AppServices.Settings.RecentFolders.RemoveAt(10);
        AppServices.SaveSettings();
    }

    private async Task AnalyzeAndShow(PageViewModel page)
    {
        if (Report == null || !PathUtil.SamePath(Report.Scan.RootPath, Folder)) await AnalyzeAsync(WorkMode.Smart);
        if (Report != null) Navigate(page);
    }

    private async Task RunModeAsync(WorkMode mode)
    {
        await AnalyzeAsync(mode);
        if (Report == null) return;
        switch (mode)
        {
            case WorkMode.Quick:
                Navigate(Dashboard);
                break;
            case WorkMode.Smart:
            case WorkMode.Deep:
                BuildSortPlan(SortStrategy.Smart);
                Navigate(Structure);
                break;
        }
    }

    /// <summary>«Навести порядок»: анализ → предложения → предпросмотр. Без подтверждения ничего не выполняется.</summary>
    private async Task TidyUpAsync()
    {
        await AnalyzeAsync(AppServices.Settings.ContentAnalysisEnabled ? WorkMode.Deep : WorkMode.Smart);
        if (Report == null) return;
        var rules = new RulesEngine(AppServices.Db.GetRules(AppServices.Db.GetActiveProfile().Id));
        _builder = null;
        OperationPlanner.PlanTidyUp(EnsureBuilder(), Report, SortOptionsFor(SortStrategy.Smart), new SmartClassifier(), rules);
        NotifyPlanChanged();
        Navigate(Plan);
    }

    public SortOptions SortOptionsFor(SortStrategy strategy) => new()
    {
        Strategy = strategy,
        TopLevelOnly = AppServices.Settings.TopLevelOnly,
        DeepResults = DeepResults,
    };

    public void BuildSortPlan(SortStrategy strategy)
    {
        if (Report == null) return;
        var rules = new RulesEngine(AppServices.Db.GetRules(AppServices.Db.GetActiveProfile().Id));
        var b = EnsureBuilder();
        OperationPlanner.PlanSort(b, Report.Scan, SortOptionsFor(strategy), new SmartClassifier(), rules, Report.Projects);
        b.PruneUnusedFolders();
        NotifyPlanChanged();
    }

    public async Task AnalyzeAsync(WorkMode mode)
    {
        if (!HasFolder) { PickFolder(); if (!HasFolder) return; }
        if (!Directory.Exists(Folder)) { AppServices.Info("Папка не найдена: " + Folder); return; }

        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        IsBusy = true;
        ProgressTitle = mode switch { WorkMode.Quick => "Быстрая уборка", WorkMode.Deep => "Глубокий анализ", _ => "Анализ папки" };
        ProgressIndeterminate = true;
        ProgressText = "Подготовка…";
        ProgressDetail = "";
        try
        {
            var profile = AppServices.Db.GetActiveProfile();
            var scanOptions = new ScanOptions
            {
                Exclusions = AppServices.Db.GetExclusions(profile.Id),
                IncludeHidden = AppServices.Settings.IncludeHidden,
            };
            var scanner = new Scanner(AppServices.CreateClassifier(), AppServices.Log);
            var scanProgress = new Progress<ScanProgress>(p =>
            {
                ProgressText = $"Сканируется: {Format.Number(p.FilesScanned)} файлов, {Format.Number(p.FoldersScanned)} папок ({Format.Size(p.BytesScanned)})";
                ProgressDetail = p.CurrentFolder.Length > 0 ? "Текущий каталог: " + p.CurrentFolder : "";
            });
            var scan = await scanner.ScanAsync(Folder, scanOptions, scanProgress, ct);

            var options = mode == WorkMode.Quick ? AnalysisOptions.QuickCleanup() : AnalysisOptions.Full();
            options.OldThreshold = TimeSpan.FromDays(AppServices.Settings.OldFilesDays);
            options.LargeFileThreshold = AppServices.Settings.LargeFileMb * 1024L * 1024;
            ProgressIndeterminate = false;
            var analysisProgress = new Progress<AnalysisProgress>(p =>
            {
                ProgressText = $"Сканировано: {Format.Number(scan.Files.Count)} файлов. {p.Stage}…";
                ProgressDetail = p.Detail;
                ProgressValue = p.Percent;
            });
            var report = await new Analyzer(AppServices.Db, new SmartClassifier(), AppServices.Log).AnalyzeAsync(scan, options, analysisProgress, ct);
            if (scan.Cancelled) AppServices.Info("Анализ остановлен. Показаны результаты по уже просканированной части папки.");
            try { AppServices.Db.SaveScanSummary(report); } catch (Exception ex) { AppServices.Log.Error("SaveScanSummary", Folder, ex); }

            DeepResults = null;
            if (mode == WorkMode.Deep || (mode != WorkMode.Quick && AppServices.Settings.ContentAnalysisEnabled))
                DeepResults = await DeepClassifyAsync(report, ct);

            _builder = null;
            Report = report;
            NotifyPlanChanged();
        }
        catch (OperationCanceledException)
        {
            AppServices.Info("Анализ остановлен пользователем.");
        }
        finally
        {
            IsBusy = false;
            ProgressText = "";
            ProgressDetail = "";
        }
    }

    /// <summary>Анализ содержимого документов: только для файлов, тема которых не определена по имени уверенно.</summary>
    private async Task<Dictionary<string, ClassificationResult>> DeepClassifyAsync(AnalysisReport report, CancellationToken ct)
    {
        var result = new Dictionary<string, ClassificationResult>(PathUtil.Comparer);
        var ai = AppServices.CreateAIClassifier(out var warning);
        if (warning != null) AppServices.Info(warning, "ИИ-классификация");
        var smart = new SmartClassifier();
        var files = (AppServices.Settings.TopLevelOnly ? report.Scan.Root.Files : report.Scan.Files)
            .Where(f => ContentExtractor.CanExtract(f.Extension)).ToList();
        ProgressTitle = "Умный анализ содержимого";
        int i = 0;
        foreach (var f in files)
        {
            ct.ThrowIfCancellationRequested();
            i++;
            ProgressValue = 100.0 * i / Math.Max(1, files.Count);
            ProgressText = $"Анализ содержимого: {i} / {files.Count}";
            ProgressDetail = f.Name;
            var neighbors = report.Scan.FindFolder(f.DirectoryPath)?.Files;
            var r = await Task.Run(() => smart.ClassifyDeepAsync(f, neighbors, ai, AppServices.Settings.AutoSelectThreshold, ct), ct);
            if (r.Source is ClassificationSource.Content or ClassificationSource.AI) result[f.FullPath] = r;
        }
        return result;
    }

    private void ShowErrors()
    {
        if (Report == null) return;
        var w = new Views.ErrorsWindow(Report.Scan.Errors) { Owner = Application.Current.MainWindow };
        w.ShowDialog();
    }

    /// <summary>Папки «Разобрать»: сообщает о новых файлах в Загрузках, на Рабочем столе и т. п.</summary>
    public void CheckInbox()
    {
        try
        {
            foreach (var (id, path, enabled, last) in AppServices.Db.GetWatchedFolders())
            {
                if (!enabled || !Directory.Exists(path)) continue;
                var since = last ?? DateTime.MinValue;
                int count = new DirectoryInfo(path).EnumerateFiles("*", new EnumerationOptions { IgnoreInaccessible = true })
                    .Count(f => f.LastWriteTimeUtc > since || f.CreationTimeUtc > since);
                if (count > 0)
                {
                    InboxFolder = path;
                    InboxMessage = $"В папке «{Path.GetFileName(path)}» {count} новых файлов. Разобрать?";
                    return;
                }
            }
            InboxMessage = null;
        }
        catch (Exception ex)
        {
            AppServices.Log.Error("Inbox", null, ex);
        }
    }

    public async Task SortInboxAsync()
    {
        if (InboxFolder == null) return;
        var folder = InboxFolder;
        foreach (var w in AppServices.Db.GetWatchedFolders().Where(w => PathUtil.SamePath(w.Path, folder)))
            AppServices.Db.MarkWatchedFolderChecked(w.Id);
        InboxMessage = null;
        SetFolder(folder);
        await RunModeAsync(WorkMode.Smart);
    }

    /// <summary>После выполнения плана: журнал обновлён, план очищен, анализ повторяется.</summary>
    public async Task AfterExecutionAsync()
    {
        ResetPlan();
        History.Reload();
        await AnalyzeAsync(WorkMode.Smart);
    }

    /// <summary>Выполнение плана с прогрессом и возможностью остановки.</summary>
    public async Task<ExecutionResult?> ExecutePlanAsync(OperationPlan plan)
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressTitle = "Применение изменений";
        ProgressIndeterminate = false;
        try
        {
            var exec = new OperationExecutor(AppServices.Db, AppServices.Log, AppServices.RecycleBin)
            {
                SimulationMode = AppServices.Settings.SimulationMode,
                DeleteMode = AppServices.Settings.DeleteMode,
            };
            var progress = new Progress<ExecutionProgress>(p =>
            {
                ProgressValue = 100.0 * p.Done / Math.Max(1, p.Total);
                ProgressText = $"Выполнено {Format.Number(p.Done)} из {Format.Number(p.Total)}" + (p.Failed > 0 ? $", ошибок: {p.Failed}" : "");
                ProgressDetail = p.CurrentPath;
            });
            return await exec.ExecuteAsync(plan, progress, _cts.Token);
        }
        finally
        {
            IsBusy = false;
            ProgressText = "";
            ProgressDetail = "";
        }
    }

    public CancellationToken BeginBusy(string title)
    {
        _cts = new CancellationTokenSource();
        IsBusy = true;
        ProgressTitle = title;
        ProgressIndeterminate = true;
        return _cts.Token;
    }

    public void EndBusy()
    {
        IsBusy = false;
        ProgressText = "";
        ProgressDetail = "";
    }
}
