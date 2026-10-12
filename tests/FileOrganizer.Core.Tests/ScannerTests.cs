using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Scanning;

namespace FileOrganizer.Core.Tests;

public class ScannerTests
{
    [Fact]
    public void Scans_files_and_folders_with_cyrillic_names()
    {
        using var t = new TempDir();
        t.File("Документы/Отчёт за 2026 год.docx", "a");
        t.File("Фото/Отпуск/IMG_0001.jpg", "bb");
        t.File("README", "без расширения");
        t.Dir("Пустая");

        var r = t.Scan();

        Assert.Equal(3, r.Files.Count);
        Assert.Equal(5, r.Folders.Count); // корень + Документы + Фото + Отпуск + Пустая
        Assert.Contains(r.Files, f => f.Name == "Отчёт за 2026 год.docx" && f.Category == FileCategory.Documents);
        var readme = r.Files.Single(f => f.Name == "README");
        Assert.Equal("", readme.Extension);
        Assert.Equal(FileCategory.Other, readme.Category);
        Assert.Equal(r.TotalSize, r.Root.TotalSize);
        Assert.Equal(3, r.Root.TotalFileCount);
    }

    [Fact]
    public void Skips_hidden_files_by_default_and_includes_on_request()
    {
        using var t = new TempDir();
        t.File("visible.txt");
        var hidden = t.File(".hidden.txt");
        if (OperatingSystem.IsWindows()) File.SetAttributes(hidden, FileAttributes.Hidden);

        Assert.Single(t.Scan().Files);
        Assert.Equal(2, t.Scan(new ScanOptions { IncludeHidden = true }).Files.Count);
    }

    [Fact]
    public void Applies_user_exclusions_and_skips_recovery_folder()
    {
        using var t = new TempDir();
        t.File("keep.pdf");
        t.File("debug.log");
        t.File("temp.tmp");
        t.File("node_modules/lib/index.js");
        t.File("Личное/secret.docx");
        t.File("FileOrganizer_Recovery/2026/old.txt");

        var opts = new ScanOptions
        {
            Exclusions = new ExclusionSet
            {
                Items =
                {
                    new(ExclusionKind.Mask, "*.log"),
                    new(ExclusionKind.Extension, "tmp"),
                    new(ExclusionKind.Folder, "node_modules"),
                    new(ExclusionKind.Folder, t.P("Личное")),
                },
            },
        };
        var r = t.Scan(opts);

        Assert.Equal(new[] { "keep.pdf" }, r.Files.Select(f => f.Name).ToArray());
    }

    [Fact]
    public void Handles_long_paths()
    {
        using var t = new TempDir();
        var deep = string.Join("/", Enumerable.Range(0, 12).Select(i => $"очень_длинное_имя_папки_номер_{i:00}"));
        var path = t.File(deep + "/файл.txt", "data");
        Assert.True(path.Length > 260);

        var r = t.Scan();
        Assert.Single(r.Files);
        Assert.Empty(r.Errors);
    }

    [Fact]
    public void Cancellation_stops_scan()
    {
        using var t = new TempDir();
        for (int i = 0; i < 2000; i++) t.File($"d{i % 20}/f{i}.txt");
        using var cts = new CancellationTokenSource();
        var progress = new SyncProgress<ScanProgress>(_ => cts.Cancel());

        var r = new Scanner().Scan(t.Path, new ScanOptions { ProgressEvery = 100 }, progress, cts.Token);

        Assert.True(r.Cancelled);
        Assert.True(r.Files.Count < 2000);
    }

    [Fact]
    public void Access_errors_do_not_stop_scan()
    {
        if (OperatingSystem.IsWindows() || Environment.UserName == "root") return; // root читает всё
        using var t = new TempDir();
        t.File("ok/a.txt");
        var locked = t.Dir("locked");
        t.File("locked/b.txt");
        File.SetUnixFileMode(locked, UnixFileMode.None);
        try
        {
            var r = t.Scan();
            Assert.Single(r.Files);
            Assert.Single(r.Errors);
            Assert.Equal(ScanErrorKind.AccessDenied, r.Errors[0].Kind);
        }
        finally
        {
            File.SetUnixFileMode(locked, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
        }
    }

    [Fact]
    public void Does_not_follow_symlinks()
    {
        using var t = new TempDir();
        t.File("real/a.txt");
        try { Directory.CreateSymbolicLink(t.P("link"), t.P("real")); }
        catch (Exception) { return; } // нет прав на создание ссылок
        var r = t.Scan();
        Assert.Single(r.Files);
        Assert.Contains(r.SkippedPaths, p => p.EndsWith("link"));
    }

    [Theory]
    [InlineData("report.PDF", "pdf")]
    [InlineData("archive.tar.gz", "gz")]
    [InlineData(".gitignore", "")]
    [InlineData("README", "")]
    [InlineData("Отчёт v1.2 final", "")]
    [InlineData("name.", "")]
    public void Extension_parsing(string name, string expected) => Assert.Equal(expected, FileClassifier.GetExtension(name));

    [Theory]
    [InlineData("docx", FileCategory.Documents)]
    [InlineData("XLSX", FileCategory.Spreadsheets)]
    [InlineData("pptx", FileCategory.Presentations)]
    [InlineData("heic", FileCategory.Images)]
    [InlineData("mkv", FileCategory.Video)]
    [InlineData("flac", FileCategory.Audio)]
    [InlineData("7z", FileCategory.Archives)]
    [InlineData("msi", FileCategory.Installers)]
    [InlineData("ps1", FileCategory.Code)]
    [InlineData("xyz", FileCategory.Other)]
    public void Classifies_by_extension(string ext, FileCategory expected) => Assert.Equal(expected, new FileClassifier().Classify(ext));

    [Fact]
    public void Custom_category_table()
    {
        var c = new FileClassifier(new Dictionary<FileCategory, string[]> { [FileCategory.Documents] = new[] { "xyz" } });
        Assert.Equal(FileCategory.Documents, c.Classify("xyz"));
        Assert.Equal(FileCategory.Other, c.Classify("pdf"));
    }
}

/// <summary>IProgress, вызывающий обработчик синхронно (Progress&lt;T&gt; использует пул потоков).</summary>
public sealed class SyncProgress<T>(Action<T> handler) : IProgress<T>
{
    public void Report(T value) => handler(value);
}
