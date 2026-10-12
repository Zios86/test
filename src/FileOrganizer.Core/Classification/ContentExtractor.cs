using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace FileOrganizer.Core.Classification;

/// <summary>
/// Извлекает небольшой фрагмент текста из документа для анализа содержимого.
/// Текст живёт только в памяти, на диск и в журнал не пишется.
/// </summary>
public static partial class ContentExtractor
{
    public const int DefaultMaxChars = 8000;
    /// <summary>Файлы больше этого размера не читаются для анализа содержимого.</summary>
    public const long MaxFileSize = 50L * 1024 * 1024;

    private static readonly HashSet<string> PlainText = new(StringComparer.OrdinalIgnoreCase) { "txt", "md", "csv", "tsv", "log", "json", "xml", "html", "htm" };

    static ContentExtractor()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static bool CanExtract(string extension) =>
        PlainText.Contains(extension) || extension is "docx" or "xlsx" or "pptx" or "odt" or "ods" or "odp" or "rtf" or "pdf";

    public static string? TryExtract(string path, int maxChars = DefaultMaxChars)
    {
        try
        {
            var fi = new FileInfo(path);
            if (!fi.Exists || fi.Length == 0 || fi.Length > MaxFileSize) return null;
            var ext = Path.GetExtension(path).TrimStart('.').ToLowerInvariant();
            string? text = ext switch
            {
                _ when PlainText.Contains(ext) => ReadPlainText(path, maxChars),
                "docx" => ReadZipXml(path, e => e.FullName == "word/document.xml", maxChars),
                "xlsx" => ReadZipXml(path, e => e.FullName == "xl/sharedStrings.xml", maxChars),
                "pptx" => ReadZipXml(path, e => e.FullName.StartsWith("ppt/slides/slide", StringComparison.Ordinal), maxChars),
                "odt" or "ods" or "odp" => ReadZipXml(path, e => e.FullName == "content.xml", maxChars),
                "rtf" => ReadRtf(path, maxChars),
                "pdf" => ReadPdf(path, maxChars),
                _ => null,
            };
            if (text == null) return null;
            text = Spaces().Replace(text, " ").Trim();
            return text.Length > maxChars ? text[..maxChars] : text;
        }
        catch
        {
            return null; // Повреждённый или заблокированный файл — просто нет текста.
        }
    }

    private static string ReadPlainText(string path, int maxChars)
    {
        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var buf = new byte[Math.Min(maxChars * 4, (int)Math.Min(fs.Length, int.MaxValue))];
        int read = fs.Read(buf, 0, buf.Length);
        var span = buf.AsSpan(0, read);
        if (span.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) return Encoding.UTF8.GetString(span[3..]);
        if (span.StartsWith(new byte[] { 0xFF, 0xFE })) return Encoding.Unicode.GetString(span[2..]);
        try
        {
            return new UTF8Encoding(false, true).GetString(span);
        }
        catch (DecoderFallbackException)
        {
            // Не UTF-8 — скорее всего Windows-1251 (кириллица).
            return Encoding.GetEncoding(1251).GetString(span);
        }
    }

    private static string? ReadZipXml(string path, Func<ZipArchiveEntry, bool> filter, int maxChars)
    {
        using var zip = ZipFile.OpenRead(path);
        var sb = new StringBuilder();
        foreach (var entry in zip.Entries.Where(filter).OrderBy(e => e.FullName, StringComparer.Ordinal))
        {
            if (entry.Length > MaxFileSize) continue;
            using var s = entry.Open();
            using var reader = XmlReader.Create(s, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
            while (reader.Read())
            {
                if (reader.NodeType is XmlNodeType.Text or XmlNodeType.CDATA)
                {
                    sb.Append(reader.Value).Append(' ');
                    if (sb.Length >= maxChars) return sb.ToString();
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.LocalName is "p" or "si" or "row")
                {
                    sb.Append('\n');
                }
            }
        }
        return sb.ToString();
    }

    private static string ReadRtf(string path, int maxChars)
    {
        var raw = ReadPlainText(path, maxChars * 4);
        // Упрощённо: убираем управляющие слова и группы.
        var noHex = RtfHex().Replace(raw, m =>
        {
            var b = Convert.ToByte(m.Groups[1].Value, 16);
            return Encoding.GetEncoding(1251).GetString(new[] { b });
        });
        var noUnicode = RtfUnicode().Replace(noHex, m => ((char)short.Parse(m.Groups[1].Value)).ToString());
        var text = RtfControl().Replace(noUnicode, " ").Replace("{", "").Replace("}", "");
        return text;
    }

    private static string ReadPdf(string path, int maxChars)
    {
        using var doc = UglyToad.PdfPig.PdfDocument.Open(path);
        var sb = new StringBuilder();
        foreach (var page in doc.GetPages())
        {
            sb.Append(page.Text).Append('\n');
            if (sb.Length >= maxChars || page.Number >= 5) break;
        }
        return sb.ToString();
    }

    [GeneratedRegex(@"\s+")] private static partial Regex Spaces();
    [GeneratedRegex(@"\\'([0-9a-fA-F]{2})")] private static partial Regex RtfHex();
    [GeneratedRegex(@"\\u(-?\d+)\??")] private static partial Regex RtfUnicode();
    [GeneratedRegex(@"\\[a-zA-Z]+-?\d* ?|\\[^a-zA-Z]")] private static partial Regex RtfControl();
}
