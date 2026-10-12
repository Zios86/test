using System.IO.Compression;
using System.Text;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Rules;

namespace FileOrganizer.Core.Tests;

public class SmartClassifierTests
{
    [Fact]
    public void Commercial_offers_from_spec_are_one_topic_with_explanation()
    {
        using var t = new TempDir();
        t.File("КП_Компания1.pdf");
        t.File("Коммерческое_предложение_Компания2.docx");
        t.File("Сравнение_КП.xlsx");
        t.File("KP_Rostelecom.pdf");
        var scan = t.Scan();
        var smart = new SmartClassifier();

        foreach (var f in scan.Files)
        {
            var r = smart.Classify(f, scan.Root.Files);
            Assert.Equal("Коммерческие предложения", r.Topic);
            Assert.Equal(ClassificationSource.FileName, r.Source);
            Assert.True(r.Confidence >= 0.85, $"{f.Name}: {r.Confidence}");
        }
        var kp = smart.Classify(scan.Files.Single(f => f.Name == "KP_Rostelecom.pdf"), scan.Root.Files);
        Assert.Contains("КП", kp.Explanation);
        Assert.Contains("PDF", kp.Explanation);
        Assert.Contains("обнаружены ещё", kp.Explanation);
    }

    [Fact]
    public void Single_file_without_neighbors_has_medium_confidence()
    {
        using var t = new TempDir();
        t.File("Счет_123.pdf");
        var r = new SmartClassifier().Classify(t.Scan().Files.Single());
        Assert.Equal("Счета", r.Topic);
        Assert.Equal(0.8, r.Confidence, 2);
    }

    [Fact]
    public void Falls_back_to_type()
    {
        using var t = new TempDir();
        t.File("song.mp3");
        var r = new SmartClassifier().Classify(t.Scan().Files.Single());
        Assert.Equal(ClassificationSource.FileType, r.Source);
        Assert.Equal("Аудио", r.TargetFolder);
        Assert.True(r.Confidence >= 0.9);
    }

    [Fact]
    public async Task Deep_analysis_reads_document_text()
    {
        using var t = new TempDir();
        var path = t.P("document1.docx");
        CreateDocx(path, "ДОГОВОР ПОСТАВКИ № 15. Стороны заключили настоящий договор. Срок действия договора — 2026 год.");
        var f = t.Scan().Files.Single();

        var r = await new SmartClassifier().ClassifyDeepAsync(f, null, null, 0.85, default);

        Assert.Equal("Договоры", r.Topic);
        Assert.Equal(ClassificationSource.Content, r.Source);
        Assert.Contains("в тексте документа", r.Explanation);
    }

    [Fact]
    public async Task AI_is_used_only_when_rules_are_not_enough()
    {
        using var t = new TempDir();
        t.File("Счет_42.pdf");
        t.File("document1.txt", "непонятный текст");
        var files = t.Scan().Files;
        var ai = new FakeAI();
        var smart = new SmartClassifier();

        var byName = await smart.ClassifyDeepAsync(files.Single(f => f.Name == "Счет_42.pdf"), null, ai, 0.75, default);
        Assert.Equal(ClassificationSource.FileName, byName.Source);
        Assert.Equal(0, ai.Calls);

        var unknown = await smart.ClassifyDeepAsync(files.Single(f => f.Name == "document1.txt"), null, ai, 0.85, default);
        Assert.Equal(1, ai.Calls);
        Assert.Equal(ClassificationSource.AI, unknown.Source);
        Assert.Equal("Договор/Проект ABC/2026", unknown.TargetFolder);
        Assert.True(ai.LastExcerpt!.Length <= 2000);
    }

    private sealed class FakeAI : IAIClassifier
    {
        public int Calls;
        public string? LastExcerpt;
        public string Name => "Fake";
        public bool IsRemote => false;
        public Task<AIClassification?> ClassifyAsync(string fileName, string? textExcerpt, CancellationToken ct)
        {
            Calls++;
            LastExcerpt = textExcerpt;
            return Task.FromResult<AIClassification?>(new AIClassification("Договор", "Проект ABC", null, null, 2026, 0.9));
        }
    }

    [Fact]
    public void Ollama_response_parsing()
    {
        var r = OllamaClassifier.Parse("{\"documentType\":\"Договор\",\"project\":\"ABC\",\"client\":null,\"year\":2026,\"confidence\":0.9}");
        Assert.NotNull(r);
        Assert.Equal("Договор", r!.DocumentType);
        Assert.Equal("ABC", r.Project);
        Assert.Equal(2026, r.Year);
        Assert.Null(OllamaClassifier.Parse("not json"));
    }

    internal static void CreateDocx(string path, string text)
    {
        using var zip = ZipFile.Open(path, ZipArchiveMode.Create);
        var e = zip.CreateEntry("word/document.xml");
        using var w = new StreamWriter(e.Open(), Encoding.UTF8);
        w.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:body><w:p><w:r><w:t>{text}</w:t></w:r></w:p></w:body></w:document>");
    }
}

public class ContentExtractorTests
{
    [Fact]
    public void Reads_windows_1251_text()
    {
        using var t = new TempDir();
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        t.Bytes("old.txt", Encoding.GetEncoding(1251).GetBytes("Протокол совещания"));
        Assert.Equal("Протокол совещания", ContentExtractor.TryExtract(t.P("old.txt")));
    }

    [Fact]
    public void Corrupted_document_returns_null()
    {
        using var t = new TempDir();
        t.File("broken.docx", "this is not a zip");
        Assert.Null(ContentExtractor.TryExtract(t.P("broken.docx")));
    }
}

public class RulesTests
{
    [Fact]
    public void Pdf_with_invoice_in_name_goes_to_invoices()
    {
        using var t = new TempDir();
        t.File("Счет_45.pdf");
        t.File("Счет_45.docx");
        t.File("Отчет.pdf");
        var files = t.Scan().Files;
        var engine = new RulesEngine(RulesEngine.Examples());

        Assert.Equal("Документы/Счета", engine.Match(files.Single(f => f.Name == "Счет_45.pdf"))!.TargetFolder);
        Assert.Null(engine.Match(files.Single(f => f.Name == "Счет_45.docx")));
        Assert.Null(engine.Match(files.Single(f => f.Name == "Отчет.pdf")));
    }

    [Fact]
    public void Jpg_older_than_2024_goes_to_archive_by_year()
    {
        using var t = new TempDir();
        t.File("old.jpg", "x", new DateTime(2022, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        t.File("new.jpg", "x", new DateTime(2025, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        var files = t.Scan().Files;
        var engine = new RulesEngine(RulesEngine.Examples());

        Assert.Equal("Фото/Архив/2022", engine.Match(files.Single(f => f.Name == "old.jpg"))!.TargetFolder);
        Assert.Null(engine.Match(files.Single(f => f.Name == "new.jpg")));
    }

    [Theory]
    [InlineData(RuleField.Size, RuleOperator.GreaterThan, "0.5", true)]
    [InlineData(RuleField.Size, RuleOperator.LessThan, "0.5", false)]
    [InlineData(RuleField.Name, RuleOperator.Matches, "*.bin", true)]
    [InlineData(RuleField.Name, RuleOperator.Matches, "^data\\d+", true)]
    [InlineData(RuleField.Extension, RuleOperator.InList, "zip, bin, rar", true)]
    [InlineData(RuleField.Category, RuleOperator.Equals, "Прочее", true)]
    [InlineData(RuleField.Modified, RuleOperator.NewerThanDays, "1", true)]
    [InlineData(RuleField.Modified, RuleOperator.OlderThanDays, "1", false)]
    [InlineData(RuleField.Name, RuleOperator.Matches, "([invalid", false)]
    public void Condition_operators(RuleField field, RuleOperator op, string value, bool expected)
    {
        using var t = new TempDir();
        t.Bytes("data1.bin", new byte[1024 * 1024]);
        var f = t.Scan().Files.Single();
        var rule = new Rule { Conditions = { new RuleCondition { Field = field, Operator = op, Value = value } } };
        Assert.Equal(expected, rule.IsMatch(f));
    }
}
