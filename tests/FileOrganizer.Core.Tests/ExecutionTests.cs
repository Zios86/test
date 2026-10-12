using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Execution;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.Core.Tests;

public class ExecutionTests
{
    private static (PlanBuilder b, ScanResult scan) SortPlan(TempDir t, bool topLevelOnly = true)
    {
        var scan = t.Scan();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, scan, new SortOptions { Strategy = SortStrategy.ByType, TopLevelOnly = topLevelOnly });
        return (b, scan);
    }

    [Fact]
    public void Simulation_mode_is_default_and_blocks_changes()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        var before = Snapshot.Take(t.Path);
        var (b, _) = SortPlan(t);
        var exec = new OperationExecutor(t.Db());

        Assert.True(exec.SimulationMode);
        Assert.True(new AppSettings().SimulationMode);
        Assert.Throws<InvalidOperationException>(() => exec.Execute(b.Plan));
        Assert.Equal(before, Snapshot.Take(t.Path));
    }

    [Fact]
    public void Execute_then_rollback_restores_exact_state()
    {
        using var t = new TempDir();
        t.File("Отчёт.pdf", "1");
        t.File("photo.jpg", "2");
        t.File("music.mp3", "3");
        t.File("dup.txt", "same");
        t.File("Downloads/dup (1).txt", "same");
        t.Dir("Пустая/Вложенная");
        t.File("ТолькоМусор/Thumbs.db", "junk");
        var before = Snapshot.Take(t.Path);
        var db = t.Db();

        var report = new Analyzer(db).Analyze(new Scanning.Scanner().Scan(t.Path, new ScanOptions()), AnalysisOptions.Full());
        var plan = OperationPlanner.PlanTidyUp(report, new SortOptions { Strategy = SortStrategy.ByType });
        foreach (var op in plan.Operations) op.Selected = true; // пользователь отметил всё, включая дубликаты

        var result = new OperationExecutor(db) { SimulationMode = false }.Execute(plan);

        Assert.Equal(0, result.Failed);
        Assert.True(File.Exists(t.P("Документы/Отчёт.pdf")));
        Assert.True(File.Exists(t.P("Изображения/photo.jpg")));
        Assert.False(Directory.Exists(t.P("Пустая")));
        Assert.False(Directory.Exists(t.P("ТолькоМусор")));
        Assert.Single(Directory.GetFiles(t.Path, "dup*.txt", SearchOption.AllDirectories), p => !p.Contains("FileOrganizer_Recovery"));
        Assert.True(Directory.Exists(plan.RecoveryFolder));

        var rb = new RollbackManager(db).RollbackBatch(result.BatchId);

        Assert.Equal(0, rb.Failed);
        Assert.Equal(before, Snapshot.Take(t.Path));
        Assert.Equal(BatchStatus.RolledBack, db.GetBatch(result.BatchId)!.Status);
    }

    [Fact]
    public void Never_overwrites_existing_file_even_if_it_appears_after_planning()
    {
        using var t = new TempDir();
        t.File("a.pdf", "new");
        var (b, _) = SortPlan(t);
        t.File("Документы/a.pdf", "appeared later"); // файл появился после анализа
        var db = t.Db();

        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);

        Assert.Equal(0, r.Failed);
        Assert.Equal("appeared later", File.ReadAllText(t.P("Документы/a.pdf")));
        Assert.Equal("new", File.ReadAllText(t.P("Документы/a (2).pdf")));
    }

    [Fact]
    public void Replace_moves_existing_file_to_recovery_and_is_reversible()
    {
        using var t = new TempDir();
        t.File("a.pdf", "new");
        t.File("Документы/a.pdf", "old");
        var before = Snapshot.Take(t.Path);
        var (b, _) = SortPlan(t);
        var op = b.Plan.Operations.Single(o => o.Kind == OperationKind.Move);
        b.SetResolution(op, ConflictResolution.Replace);
        op.Selected = true;
        var db = t.Db();

        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal("new", File.ReadAllText(t.P("Документы/a.pdf")));
        Assert.Contains(Directory.GetFiles(b.Plan.RecoveryFolder, "*", SearchOption.AllDirectories), p => File.ReadAllText(p) == "old");

        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(before, Snapshot.Take(t.Path));
    }

    [Fact]
    public void Skip_resolution_leaves_file()
    {
        using var t = new TempDir();
        t.File("a.pdf", "new");
        t.File("Документы/a.pdf", "old");
        var (b, _) = SortPlan(t);
        var op = b.Plan.Operations.Single(o => o.Kind == OperationKind.Move);
        b.SetResolution(op, ConflictResolution.Skip);
        op.Selected = true;
        var r = new OperationExecutor(t.Db()) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(1, r.Skipped);
        Assert.True(File.Exists(t.P("a.pdf")));
    }

    [Fact]
    public void One_failed_file_does_not_stop_the_rest()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        t.File("b.pdf");
        t.File("c.pdf");
        var (b, _) = SortPlan(t);
        File.Delete(t.P("b.pdf")); // исчез после анализа
        var db = t.Db();

        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);

        Assert.Equal(1, r.Failed);
        Assert.True(File.Exists(t.P("Документы/a.pdf")));
        Assert.True(File.Exists(t.P("Документы/c.pdf")));
        Assert.Equal(BatchStatus.CompletedWithErrors, db.GetBatch(r.BatchId)!.Status);
    }

    [Fact]
    public void Read_only_and_cyrillic_and_no_extension_files_are_moved_and_restored()
    {
        using var t = new TempDir();
        var ro = t.File("Только чтение.pdf", "ro");
        File.SetAttributes(ro, FileAttributes.ReadOnly);
        t.File("Без расширения", "x");
        var before = Snapshot.Take(t.Path);
        var scan = t.Scan();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanSort(b, scan, new SortOptions { Strategy = SortStrategy.ByType, SkipUnknown = false });
        foreach (var op in b.Plan.Operations) b.SetSelected(op, true); // «Прочее» — низкая уверенность, пользователь отметил вручную
        var db = t.Db();

        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(0, r.Failed);
        Assert.True(File.Exists(t.P("Документы/Только чтение.pdf")));
        Assert.True(File.Exists(t.P("Прочее/Без расширения")));

        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(before, Snapshot.Take(t.Path));
    }

    [Fact]
    public void Long_paths_move_and_rollback()
    {
        using var t = new TempDir();
        var deep = string.Join("/", Enumerable.Range(0, 12).Select(i => $"очень_длинное_имя_папки_номер_{i:00}"));
        t.File(deep + "/файл.pdf", "data");
        var before = Snapshot.Take(t.Path);
        var (b, _) = SortPlan(t, topLevelOnly: false);
        var db = t.Db();
        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(0, r.Failed);
        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(before, Snapshot.Take(t.Path));
    }

    [Fact]
    public void Cancellation_mid_execution_keeps_journal_consistent_and_reversible()
    {
        using var t = new TempDir();
        for (int i = 0; i < 50; i++) t.File($"f{i:00}.pdf", i.ToString());
        var before = Snapshot.Take(t.Path);
        var (b, _) = SortPlan(t);
        var db = t.Db();
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<ExecutionProgress>(p => { if (p.Done >= 10) cts.Cancel(); });

        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan, progress, cts.Token);

        Assert.True(r.Cancelled);
        Assert.Equal(BatchStatus.Cancelled, db.GetBatch(r.BatchId)!.Status);
        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(before, Snapshot.Take(t.Path));
    }

    [Fact]
    public void Crash_recovery_marks_pending_operations_by_actual_state()
    {
        using var t = new TempDir();
        t.File("moved.pdf", "1");
        t.File("notmoved.pdf", "2");
        var db = t.Db();
        // Имитируем сбой: операции записаны как Pending, одна из них успела выполниться, другая — нет.
        var batch = db.CreateBatch("Сбой", t.Path, t.P("FileOrganizer_Recovery/x"), 2);
        Directory.CreateDirectory(t.P("Документы"));
        var op1 = new OperationRecord { BatchId = batch, Seq = 1, Kind = OperationKind.Move, Source = t.P("moved.pdf"), Destination = t.P("Документы/moved.pdf"), Status = OperationStatus.Pending };
        var op2 = new OperationRecord { BatchId = batch, Seq = 2, Kind = OperationKind.Move, Source = t.P("notmoved.pdf"), Destination = t.P("Документы/notmoved.pdf"), Status = OperationStatus.Pending };
        db.AddOperation(op1);
        db.AddOperation(op2);
        File.Move(t.P("moved.pdf"), t.P("Документы/moved.pdf"));

        // «Перезапуск программы»: новая база на том же файле.
        var db2 = new Database(db.Path);
        var fixedCount = new RollbackManager(db2).RecoverInterrupted();

        Assert.Equal(2, fixedCount);
        Assert.Equal(BatchStatus.Interrupted, db2.GetBatch(batch)!.Status);
        var ops = db2.GetOperations(batch);
        Assert.Equal(OperationStatus.Done, ops[0].Status);
        Assert.Equal(OperationStatus.Failed, ops[1].Status);

        new RollbackManager(db2).RollbackBatch(batch);
        Assert.True(File.Exists(t.P("moved.pdf")));
    }

    [Fact]
    public void History_survives_restart_and_restore_to_point()
    {
        using var t = new TempDir();
        t.File("a.pdf", "a");
        var s0 = Snapshot.Take(t.Path);
        var db = t.Db();
        var exec = new OperationExecutor(db) { SimulationMode = false };
        var (b1, _) = SortPlan(t);
        exec.Execute(b1.Plan);

        t.File("b.jpg", "b");
        var (b2, _) = SortPlan(t);
        var point = DateTime.UtcNow.AddSeconds(-30);
        exec.Execute(b2.Plan);

        var db2 = new Database(db.Path);
        Assert.Equal(2, db2.GetBatches().Count);
        Assert.All(db2.GetBatches(), x => Assert.True(x.DoneCount > 0));

        new RollbackManager(db2).RestoreTo(point);
        Assert.Equal(s0.Keys.Append("b.jpg").OrderBy(x => x, StringComparer.Ordinal), Snapshot.Take(t.Path).Keys);
    }

    [Fact]
    public void Undo_last_and_rollback_conflict_keeps_both()
    {
        using var t = new TempDir();
        t.File("a.pdf", "original");
        var (b, _) = SortPlan(t);
        var db = t.Db();
        new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        t.File("a.pdf", "someone created new file here"); // исходное место снова занято

        var r = new RollbackManager(db).UndoLast();

        Assert.Equal(0, r.Failed);
        Assert.Equal("someone created new file here", File.ReadAllText(t.P("a.pdf")));
        Assert.Equal("original", File.ReadAllText(t.P("a (2).pdf")));
        Assert.Contains(r.Messages, m => m.Contains("восстановлен как"));
    }

    [Fact]
    public void Purge_deletes_only_recovery_files_of_batch()
    {
        using var t = new TempDir();
        t.File("x.tmp", "tmp");
        t.File("keep.txt", "keep");
        var scan = new Scanning.Scanner().Scan(t.Path, new ScanOptions());
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanTempFiles(b, TempFileDetector.Find(scan));
        var db = t.Db();
        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.False(File.Exists(t.P("x.tmp")));

        var purged = new RollbackManager(db).PurgeRecovery(r.BatchId);

        Assert.Equal(1, purged);
        Assert.False(Directory.Exists(t.P("FileOrganizer_Recovery")));
        Assert.True(File.Exists(t.P("keep.txt")));
        var rb = new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Equal(1, rb.Failed); // удалённое окончательно восстановить нельзя — и программа об этом говорит
    }

    [Fact]
    public void Empty_folder_that_got_files_is_not_deleted()
    {
        using var t = new TempDir();
        t.Dir("Пустая");
        var scan = t.Scan();
        var b = new PlanBuilder(t.Path);
        OperationPlanner.PlanEmptyFolders(b, EmptyFolderFinder.Find(scan));
        t.File("Пустая/new.txt", "появился");
        var r = new OperationExecutor(t.Db()) { SimulationMode = false }.Execute(b.Plan);
        Assert.Equal(1, r.Skipped);
        Assert.True(File.Exists(t.P("Пустая/new.txt")));
    }

    [Fact]
    public void System_paths_are_protected()
    {
        Assert.False(Infrastructure.SystemPathGuard.IsSafeToModify(OperatingSystem.IsWindows() ? @"C:\Windows\System32\kernel32.dll" : "/usr/bin/ls"));
        Assert.False(Infrastructure.SystemPathGuard.IsSafeToModify(OperatingSystem.IsWindows() ? @"C:\Users\me\AppData\Local\x.txt" : "/home/me/AppData/x.txt"));
        Assert.True(Infrastructure.SystemPathGuard.IsSafeToModify(Path.Combine(Path.GetTempPath(), "Recovery", "x.txt")));
        using var t = new TempDir();
        var b = new PlanBuilder(t.Path);
        var fake = new FileEntry { FullPath = OperatingSystem.IsWindows() ? @"C:\Windows\notepad.exe" : "/usr/bin/env", Name = "env", Extension = "", DirectoryPath = "/usr/bin" };
        Assert.Null(b.AddMove(fake, t.P("x"), "test", 1, "g"));
    }

    [Fact]
    public void Tags_follow_moved_file()
    {
        using var t = new TempDir();
        t.File("a.pdf");
        var db = t.Db();
        db.TagFile(t.P("a.pdf"), "Важно");
        var (b, _) = SortPlan(t);
        var r = new OperationExecutor(db) { SimulationMode = false }.Execute(b.Plan);
        Assert.Contains("Важно", db.GetFileTags()[t.P("Документы/a.pdf")]);
        new RollbackManager(db).RollbackBatch(r.BatchId);
        Assert.Contains("Важно", db.GetFileTags()[t.P("a.pdf")]);
    }
}

public class DatabaseTests
{
    [Fact]
    public void Settings_rules_profiles_exclusions_persist()
    {
        using var t = new TempDir();
        var db = t.Db();
        var s = db.LoadSettings();
        Assert.True(s.SimulationMode);
        s.SimulationMode = false;
        s.OldFilesDays = 365;
        db.SaveSettings(s);

        var profile = db.GetActiveProfile();
        Assert.Equal("Загрузки", profile.Name);
        foreach (var rule in Rules.RulesEngine.Examples()) db.SaveRule(profile.Id, rule);
        var excl = db.GetExclusions(profile.Id);
        excl.Items.Add(new Exclusion(ExclusionKind.Mask, "*.log"));
        db.SaveExclusions(profile.Id, excl);
        var newId = db.CreateProfile("Проекты");
        db.SetActiveProfile(newId);

        var db2 = new Database(db.Path);
        Assert.False(db2.LoadSettings().SimulationMode);
        Assert.Equal(365, db2.LoadSettings().OldFilesDays);
        Assert.Equal(2, db2.GetRules(profile.Id).Count);
        Assert.Equal("Документы/Счета", db2.GetRules(profile.Id)[0].Target);
        Assert.Contains(db2.GetExclusions(profile.Id).Items, e => e.Pattern == "*.log");
        Assert.Equal("Проекты", db2.GetActiveProfile().Name);
        Assert.Equal(6, db2.GetProfiles().Count);
    }
}
