using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.Core.Tests;

public class PlanningTests
{
    [Fact]
    public void Sort_by_type_moves_only_top_level_files_and_creates_folders()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        t.File("b.jpg");
        t.File("c.mp3");
        t.File("unknown.xyz");
        t.File("Проект/inner.pdf");
        var before = Snapshot.Take(t.Path);

        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.ByType });
        var s = b.Plan.Summary();

        Assert.Equal(3, s.Moved);
        Assert.Equal(3, s.FoldersCreated);
        Assert.DoesNotContain(b.Plan.Operations, o => o.Source.EndsWith("inner.pdf"));
        Assert.DoesNotContain(b.Plan.Operations, o => o.Source.EndsWith("unknown.xyz"));
        Assert.Contains(b.Plan.Operations, o => o.Destination == t.P("Документы/a.pdf"));
        Assert.All(b.Plan.Operations, o => Assert.False(string.IsNullOrWhiteSpace(o.Reason)));
        Assert.Equal(before, Snapshot.Take(t.Path)); // планирование ничего не меняет
    }

    [Fact]
    public void Files_already_in_place_are_left_alone()
    {
        using var t = new TempDir();
        t.File("Документы/a.pdf");
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.ByType, TopLevelOnly = false });
        Assert.Empty(b.Plan.Operations);
    }

    [Fact]
    public void Conflict_keep_both_skip_replace()
    {
        using var t = new TempDir();
        t.File("a.pdf", "new");
        t.File("Документы/a.pdf", "existing");
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.ByType });
        var op = b.Plan.Operations.Single(o => o.Kind == OperationKind.Move);

        Assert.True(op.HasConflict);
        Assert.Equal(t.P("Документы/a (2).pdf"), op.Destination);
        b.SetResolution(op, ConflictResolution.Skip);
        Assert.False(op.Selected);
        b.SetResolution(op, ConflictResolution.Replace);
        Assert.Equal(t.P("Документы/a.pdf"), op.Destination);
    }

    [Fact]
    public void Two_files_with_same_name_into_one_folder_get_unique_names()
    {
        using var t = new TempDir();
        t.File("x/report.pdf", "1");
        t.File("y/report.pdf", "2");
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.ByType, TopLevelOnly = false });
        var dests = b.Plan.Operations.Where(o => o.Kind == OperationKind.Move).Select(o => o.Destination).ToList();
        Assert.Equal(2, dests.Distinct().Count());
        Assert.Contains(t.P("Документы/report (2).pdf"), dests);
    }

    [Fact]
    public void Low_confidence_suggestions_are_not_preselected()
    {
        using var t = new TempDir();
        t.File("Счет_1.pdf");       // 0.8 — средняя уверенность: не отмечено
        t.File("song.mp3");         // тип: 0.95 — отмечено
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.Smart });
        Assert.False(b.Plan.Operations.Single(o => o.Source.EndsWith("Счет_1.pdf")).Selected);
        Assert.True(b.Plan.Operations.Single(o => o.Source.EndsWith("song.mp3")).Selected);
    }

    [Fact]
    public void Unselecting_folder_unselects_its_moves_and_selecting_move_selects_folder()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, t.Scan(), new SortOptions { Strategy = SortStrategy.ByType });
        var folder = b.Plan.Operations.Single(o => o.Kind == OperationKind.CreateFolder);
        var move = b.Plan.Operations.Single(o => o.Kind == OperationKind.Move);

        b.SetSelected(folder, false);
        Assert.False(move.Selected);
        b.SetSelected(move, true);
        Assert.True(folder.Selected);
    }

    [Fact]
    public void Duplicates_decisions()
    {
        using var t = new TempDir();
        t.File("orig.txt", "same", DateTime.UtcNow.AddDays(-10));
        t.File("Downloads/orig (1).txt", "same", DateTime.UtcNow.AddDays(-1));
        var g = new DuplicateFinder().Find(t.Scan().Files).Single();

        var b1 = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b1, g, DuplicateDecision.KeepOldest);
        var op = b1.Plan.Operations.Single(o => o.Kind == OperationKind.MoveToRecovery);
        Assert.EndsWith("orig (1).txt", op.Source);
        Assert.Contains("FileOrganizer_Recovery", op.Destination);

        var b2 = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b2, g, DuplicateDecision.KeepNewest);
        Assert.EndsWith("orig.txt", b2.Plan.Operations.Single(o => o.Kind == OperationKind.MoveToRecovery).Source);

        var b3 = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b3, g, DuplicateDecision.MoveCopiesToDuplicatesFolder);
        Assert.Contains(b3.Plan.Operations, o => o.Kind == OperationKind.Move && o.Destination.Contains("Дубликаты"));

        var b4 = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b4, g, DuplicateDecision.KeepAll);
        Assert.Empty(b4.Plan.Operations);

        var b5 = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b5, g, DuplicateDecision.KeepSelected, Array.Empty<string>());
        Assert.Empty(b5.Plan.Operations); // нельзя удалить все экземпляры
    }

    [Fact]
    public void Rename_preview_was_becomes_with_conflicts()
    {
        using var t = new TempDir();
        var d = new DateTime(2026, 10, 12, 10, 0, 0, DateTimeKind.Utc);
        t.File("Протокол совещания (1).docx", "1", d);
        t.File("Протокол совещания_final.docx", "2", d);
        var files = t.Scan().Files.OrderBy(f => f.Name).ToList();

        var preview = new RenameEngine { Template = "{date}_{clean}" }.Preview(files);

        Assert.Equal("2026-10-12_Протокол_совещания.docx", preview[0].NewName);
        Assert.Equal("2026-10-12_Протокол_совещания (2).docx", preview[1].NewName);
        Assert.NotNull(preview[1].Problem);
    }

    [Fact]
    public void Rename_templates_with_counter_and_fields()
    {
        using var t = new TempDir();
        var d = new DateTime(2026, 1, 5, 10, 0, 0, DateTimeKind.Utc);
        t.File("a.pdf", "1", d);
        t.File("b.pdf", "2", d);
        var files = t.Scan().Files.OrderBy(f => f.Name).ToList();
        var e = new RenameEngine { Template = "{client}_{doc}_{year}_{counter:00}", Client = "Ромашка", DocType = "Договор" };
        Assert.Equal(new[] { "Ромашка_Договор_2026_01.pdf", "Ромашка_Договор_2026_02.pdf" }, e.Preview(files).Select(p => p.NewName).ToArray());
    }

    [Fact]
    public void Preview_trees_show_changes()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        t.File("b.jpg");
        var scan = t.Scan();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, scan, new SortOptions { Strategy = SortStrategy.ByType });

        var current = PreviewTreeBuilder.BuildCurrent(scan, b.Plan);
        Assert.All(current.Children, c => Assert.Equal(ChangeMark.Move, c.Mark));

        var proposed = PreviewTreeBuilder.BuildProposed(scan, b.Plan);
        Assert.Equal(new[] { "Документы", "Изображения" }, proposed.Children.Select(c => c.Name).ToArray());
        Assert.All(proposed.Children, c => Assert.Equal(ChangeMark.Create, c.Mark));
        Assert.Equal(2, proposed.FileCount);
    }

    [Fact]
    public void Merge_similar_folders_plan()
    {
        using var t = new TempDir();
        t.File("Отчеты/1.txt");
        t.File("Отчеты/2.txt");
        t.File("Отчёты новые/3.txt");
        t.File("Отчёты новые/sub/4.txt");
        var g = SimilarFolderFinder.Find(t.Scan()).Single();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanMergeFolders(b, g);
        Assert.Contains(b.Plan.Operations, o => o.Destination == t.P("Отчеты/3.txt"));
        Assert.Contains(b.Plan.Operations, o => o.Destination == t.P("Отчеты/sub/4.txt"));
    }

    [Fact]
    public void Removed_operation_frees_source_for_a_new_decision()
    {
        using var t = new TempDir();
        t.File("a.txt", "same");
        t.File("b.txt", "same");
        var g = new DuplicateFinder().Find(t.Scan().Files).Single();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanDuplicates(b, g, DuplicateDecision.KeepOldest);
        foreach (var op in b.Plan.Operations.ToList()) b.Remove(op);
        Assert.Empty(b.Plan.Operations);
        OperationPlanner.PlanDuplicates(b, g, DuplicateDecision.MoveCopiesToDuplicatesFolder);
        Assert.Contains(b.Plan.Operations, o => o.Kind == OperationKind.Move);
    }
}
