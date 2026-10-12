using System.Collections.ObjectModel;
using System.Windows.Input;
using FileOrganizer.App.Mvvm;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;

namespace FileOrganizer.App.ViewModels;

public sealed record StatCard(string Title, string Value, string Hint, PageViewModel? Target);

public sealed record CategoryBar(string Name, string Size, int Count, double Fraction);

/// <summary>Главная: выбор папки, режимы, прогресс, итоги анализа большими карточками.</summary>
public sealed class DashboardViewModel : PageViewModel
{
    public DashboardViewModel(MainViewModel shell) : base(shell)
    {
        OpenCardCommand = new RelayCommand(p => { if (p is StatCard { Target: { } t }) Shell.Navigate(t); });
        SortInboxCommand = new AsyncCommand(() => Shell.SortInboxAsync());
        PickRecentCommand = new RelayCommand(p => { if (p is string s) Shell.SetFolder(s); });
    }

    public override string Title => "Главная";
    public override string Icon => "";

    public ObservableCollection<StatCard> Cards { get; } = new();
    public ObservableCollection<StatCard> Stats { get; } = new();
    public ObservableCollection<CategoryBar> Categories { get; } = new();
    public IReadOnlyList<string> RecentFolders => Services.AppServices.Settings.RecentFolders;

    public ICommand OpenCardCommand { get; }
    public ICommand SortInboxCommand { get; }
    public ICommand PickRecentCommand { get; }

    public override void OnNavigatedTo() => OnPropertyChanged(nameof(RecentFolders));

    public override void OnReportChanged()
    {
        Cards.Clear();
        Stats.Clear();
        Categories.Clear();
        var r = Shell.Report;
        if (r == null) return;
        var loose = r.Scan.Root.Files.Count;

        Cards.Add(new StatCard("Хранилище", Format.Size(r.TotalSize) + " занято", $"{Format.Number(r.FileCount)} файлов в {Format.Number(r.FolderCount)} папках", Shell.Space));
        Cards.Add(new StatCard("Дубликаты", Format.Size(r.DuplicateBytes), $"{Format.Number(r.DuplicateFileCount)} лишних копий", Shell.Duplicates));
        Cards.Add(new StatCard("Давно не использовались", Format.Size(r.OldFilesBytes), $"{Format.Number(r.OldFiles.Count)} файлов старше {(int)r.OldThreshold.TotalDays} дн.", Shell.OldFiles));
        Cards.Add(new StatCard("Временные файлы", Format.Size(r.TempFilesBytes), $"{Format.Number(r.TempFiles.Count)} файлов", Shell.TempFiles));
        Cards.Add(new StatCard("Неорганизованные", $"{Format.Number(loose)} файлов", "лежат прямо в выбранной папке", Shell.Structure));
        Cards.Add(new StatCard("Потенциально избыточные данные", Format.Size(r.PotentiallyRedundantBytes),
            "копии и временные файлы; решение — за вами", Shell.Duplicates));

        Stats.Add(new StatCard("Файлов", Format.Number(r.FileCount), "", null));
        Stats.Add(new StatCard("Папок", Format.Number(r.FolderCount), "", null));
        Stats.Add(new StatCard("Общий размер", Format.Size(r.TotalSize), "", Shell.Space));
        Stats.Add(new StatCard("Типов файлов", Format.Number(r.FileTypeCount), "", Shell.Space));
        Stats.Add(new StatCard("Дубликатов", Format.Number(r.DuplicateFileCount), "", Shell.Duplicates));
        Stats.Add(new StatCard("Пустых папок", Format.Number(r.EmptyFolders.Count), "", Shell.EmptyFolders));
        Stats.Add(new StatCard($"Старше {(int)r.OldThreshold.TotalDays} дн.", Format.Number(r.OldFiles.Count), "", Shell.OldFiles));
        Stats.Add(new StatCard($"Файлов > {Format.Size(Services.AppServices.Settings.LargeFileMb * 1024L * 1024)}", Format.Number(r.LargeFileThresholdCount), "", Shell.Space));
        Stats.Add(new StatCard("Плохие названия", Format.Number(r.BadNames.Count), "", Shell.Names));
        Stats.Add(new StatCard("Похожие папки", Format.Number(r.SimilarFolders.Count), "", Shell.SimilarFolders));
        Stats.Add(new StatCard("Возможно лежат не там", Format.Number(r.Misplaced.Count), "", Shell.Misplaced));
        Stats.Add(new StatCard("Проектные группы", Format.Number(r.Projects.Count), "", Shell.Projects));

        long max = r.CategoryUsage.Count > 0 ? r.CategoryUsage.Max(c => c.TotalBytes) : 1;
        foreach (var c in r.CategoryUsage)
            Categories.Add(new CategoryBar(c.Category.DisplayName(), Format.Size(c.TotalBytes), c.Count, max == 0 ? 0 : (double)c.TotalBytes / max));
    }
}
