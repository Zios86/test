using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Tests;

public class DuplicateFinderTests
{
    [Fact]
    public void Finds_exact_duplicates_by_sha256_and_ignores_same_size_different_content()
    {
        using var t = new TempDir();
        t.File("a/report.docx", "same content");
        t.File("Downloads/report (1).docx", "same content");
        t.File("b/other.docx", "diff content"); // тот же размер, другое содержимое
        t.File("empty1.txt", "");
        t.File("empty2.txt", "");               // пустые файлы дубликатами не считаем

        var groups = new DuplicateFinder().Find(t.Scan().Files);

        var g = Assert.Single(groups);
        Assert.Equal(2, g.Files.Count);
        Assert.Equal(12, g.Size);
        Assert.Equal(12, g.RedundantBytes);
        Assert.Equal("report.docx", g.Recommended!.Name);
        Assert.Contains("без признаков копии", g.RecommendationReason);
    }

    [Fact]
    public void Large_files_use_partial_hash_then_full_hash()
    {
        using var t = new TempDir();
        var data = new byte[300_000];
        new Random(1).NextBytes(data);
        t.Bytes("x/big.bin", data);
        t.Bytes("y/big copy.bin", data);
        var changed = (byte[])data.Clone();
        changed[150_000] ^= 0xFF; // отличие в середине: быстрый хеш совпадёт, полный — нет
        t.Bytes("z/big2.bin", changed);

        var groups = new DuplicateFinder().Find(t.Scan().Files);

        var g = Assert.Single(groups);
        Assert.DoesNotContain(g.Files, f => f.Name == "big2.bin");
    }

    [Fact]
    public void Uses_hash_cache()
    {
        using var t = new TempDir();
        t.File("a.txt", "dup");
        t.File("b.txt", "dup");
        using var dbDir = new TempDir();
        var db = new Data.Database(Path.Combine(dbDir.Path, "h.db"));
        var scan = t.Scan();
        new DuplicateFinder(db).Find(scan.Files);
        db.Flush();
        Assert.True(db.TryGet(scan.Files[0].FullPath, scan.Files[0].Size, scan.Files[0].LastWriteTimeUtc, out var h));
        Assert.Equal(64, h.Length);
    }
}

public class FolderAnalysisTests
{
    [Fact]
    public void Empty_folders_including_chains_and_junk_only()
    {
        using var t = new TempDir();
        t.Dir("Пустая");
        t.Dir("Цепочка/a/b/c");
        t.File("ТолькоМусор/Thumbs.db");
        t.File("ТолькоМусор/desktop.ini");
        t.File("Непустая/file.txt");
        t.Dir("Непустая/ВложеннаяПустая");
        t.File("СоСкрытым/.hidden");

        var scan = t.Scan();
        var empty = EmptyFolderFinder.Find(scan);
        var names = empty.Select(e => Path.GetRelativePath(t.Path, e.Path)).ToList();

        Assert.Contains("Пустая", names);
        Assert.Contains("Цепочка", names);
        Assert.DoesNotContain(Path.Combine("Цепочка", "a"), names); // только верх цепочки
        Assert.Equal(3, empty.Single(e => e.Path.EndsWith("Цепочка")).NestedEmptyCount);
        Assert.Contains("ТолькоМусор", names);
        Assert.True(empty.Single(e => e.Path.EndsWith("ТолькоМусор")).OnlySystemFiles);
        Assert.Contains(Path.Combine("Непустая", "ВложеннаяПустая"), names);
        Assert.DoesNotContain("Непустая", names);
        Assert.DoesNotContain("СоСкрытым", names); // скрытый файл — папка не пустая
    }

    [Fact]
    public void Similar_folders_from_spec()
    {
        using var t = new TempDir();
        foreach (var n in new[] { "Отчеты", "Отчёты", "Отчеты новые", "Отчеты2", "Старые отчеты", "Отчеты 2026" })
            t.File($"{n}/{n}.txt", n);
        t.File("Отчеты/common.txt", "1");
        t.File("Отчёты/common.txt", "2");
        t.Dir("Фото");

        var groups = SimilarFolderFinder.Find(t.Scan());

        var g = Assert.Single(groups);
        Assert.Equal(6, g.Folders.Count);
        Assert.Contains("common.txt", g.ConflictingNames);
    }

    [Fact]
    public void Similar_versions_are_grouped_but_not_duplicates()
    {
        using var t = new TempDir();
        t.File("Отчёт.docx", "1");
        t.File("Отчёт (1).docx", "22");
        t.File("Отчёт_final.docx", "333");
        t.File("Отчёт_final2.docx", "4444");
        t.File("План.docx", "5");

        var groups = SimilarVersionFinder.Find(t.Scan().Files);

        var g = Assert.Single(groups);
        Assert.Equal(4, g.Files.Count);
        Assert.Equal("отчет", g.BaseName);
    }

    [Fact]
    public void Old_and_large_files()
    {
        using var t = new TempDir();
        var now = DateTime.UtcNow;
        t.File("old.txt", "o", now.AddDays(-800));
        t.File("new.txt", "n", now.AddDays(-10));
        var scan = t.Scan();

        Assert.Equal("old.txt", Assert.Single(OldFileFinder.Find(scan.Files, TimeSpan.FromDays(730))).Name);
        Assert.Empty(OldFileFinder.Find(scan.Files, TimeSpan.FromDays(1095)));
        Assert.Equal(2, OldFileFinder.Find(scan.Files, TimeSpan.FromDays(5)).Count);

        var top = SpaceAnalyzer.TopLargest(scan.Files, 1);
        Assert.Single(top);
        Assert.Equal(5, SpaceAnalyzer.SizeBuckets(scan.Files).Count);
    }

    [Fact]
    public void Temp_files_detection()
    {
        using var t = new TempDir();
        t.File("a.tmp");
        t.File("~$Документ.docx");
        t.File("movie.mp4.crdownload");
        t.File("setup.exe", "x", DateTime.UtcNow.AddDays(-90));
        t.File("fresh_setup.exe", "x", DateTime.UtcNow.AddDays(-1));
        t.File("photos.zip");
        t.Dir("photos");
        t.File("doc.pdf", "abc");
        t.File("doc (1).pdf", "abc");

        var scan = new Scanning.Scanner().Scan(t.Path, new ScanOptions { Exclusions = new ExclusionSet() });
        var temp = TempFileDetector.Find(scan);
        var kinds = temp.ToDictionary(x => x.File.Name, x => x.Kind);

        Assert.Equal(TempFileKind.TempFile, kinds["a.tmp"]);
        Assert.Equal(TempFileKind.OfficeLockFile, kinds["~$Документ.docx"]);
        Assert.Equal(TempFileKind.PartialDownload, kinds["movie.mp4.crdownload"]);
        Assert.Equal(TempFileKind.OldInstaller, kinds["setup.exe"]);
        Assert.False(kinds.ContainsKey("fresh_setup.exe"));
        Assert.Equal(TempFileKind.LeftoverArchive, kinds["photos.zip"]);
        Assert.Equal(TempFileKind.RepeatedDownload, kinds["doc (1).pdf"]);
    }

    [Fact]
    public void Misplaced_file_in_photo_folder()
    {
        using var t = new TempDir();
        for (int i = 0; i < 9; i++) t.File($"Фото отпуска/IMG_{i:0000}.jpg", "p" + i);
        t.File("Фото отпуска/Договор_поставки.pdf", "d");

        var m = Assert.Single(MisplacedFinder.Find(t.Scan(), new SmartClassifier()));
        Assert.Equal("Договор_поставки.pdf", m.File.Name);
        Assert.Equal("Работа/Договоры", m.SuggestedFolder);
        Assert.Contains("90%", m.Reason);
    }

    [Fact]
    public void Project_groups()
    {
        using var t = new TempDir();
        t.File("Альфа_ТЗ.docx");
        t.File("Альфа_смета.xlsx");
        t.File("Альфа_защита.pptx");
        t.File("Прочее.docx");

        var p = Assert.Single(ProjectDetector.Find(t.Scan().Files));
        Assert.Equal("Альфа", p.Name);
        Assert.Equal(3, p.Files.Count);
    }

    [Fact]
    public void Full_analysis_report()
    {
        using var t = new TempDir();
        t.File("a.txt", "dup");
        t.File("b/a.txt", "dup");
        t.Dir("empty");
        t.File("x.tmp", "tmp");

        var report = new Analyzer().Analyze(new Scanning.Scanner().Scan(t.Path, new ScanOptions { Exclusions = new ExclusionSet() }), AnalysisOptions.Full());

        Assert.Equal(3, report.FileCount);
        Assert.Equal(1, report.DuplicateFileCount);
        Assert.Single(report.EmptyFolders);
        Assert.Equal(3 + 3, report.PotentiallyRedundantBytes); // копия + временный файл
    }
}

public class NameAnalyzerTests
{
    [Theory]
    [InlineData("Новый документ (17).docx")]
    [InlineData("123.pdf")]
    [InlineData("Документ1.docx")]
    [InlineData("Безымянный.png")]
    [InlineData("IMG_5678.jpg")]
    [InlineData("final_final2_new.docx")]
    [InlineData("копия копия.xlsx")]
    [InlineData("Untitled.txt")]
    public void Detects_bad_names_from_spec(string name)
    {
        using var t = new TempDir();
        t.File(name);
        var f = t.Scan().Files.Single();
        Assert.NotNull(NameAnalyzer.DetectProblem(f));
        var suggested = NameAnalyzer.SuggestName(f);
        Assert.NotEqual(name, suggested);
        Assert.EndsWith(Path.GetExtension(name), suggested);
        Assert.Matches(@"^\d{4}-\d{2}-\d{2}_", suggested);
    }

    [Theory]
    [InlineData("2026-10-12_Протокол_совещания.docx")]
    [InlineData("Договор поставки ООО Ромашка.pdf")]
    [InlineData("КП_Компания1.pdf")]
    public void Good_names_are_not_flagged(string name)
    {
        using var t = new TempDir();
        t.File(name);
        Assert.Null(NameAnalyzer.DetectProblem(t.Scan().Files.Single()));
    }

    [Fact]
    public void Suggestion_uses_folder_when_name_is_meaningless()
    {
        using var t = new TempDir();
        t.File("Поездка в Казань/IMG_5678.jpg", "x", new DateTime(2026, 7, 3, 12, 0, 0, DateTimeKind.Utc));
        var f = t.Scan().Files.Single();
        Assert.Equal("2026-07-03_Поездка_в_Казань_5678.jpg", NameAnalyzer.SuggestName(f));
    }
}
