using System.Text.RegularExpressions;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Classification;

/// <summary>Тематическое правило умной сортировки: ключевые слова → папка.</summary>
public sealed class TopicRule
{
    public required string Topic { get; init; }
    /// <summary>Относительный путь целевой папки, например «Работа/Счета».</summary>
    public required string Folder { get; init; }
    /// <summary>Регулярные выражения (по нормализованному имени: нижний регистр, ё→е, разделители — пробелы).</summary>
    public required Regex[] NamePatterns { get; init; }
    /// <summary>Какие типы файлов могут относиться к теме (пусто — любые).</summary>
    public FileCategory[] Categories { get; init; } = Array.Empty<FileCategory>();
    /// <summary>Человекочитаемые ключевые слова для объяснения.</summary>
    public string Keywords { get; init; } = "";

    private static Regex R(string pattern) => new(pattern, RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly FileCategory[] Docs = { FileCategory.Documents, FileCategory.Spreadsheets, FileCategory.Images };

    public static List<TopicRule> Defaults() => new()
    {
        new() { Topic = "Коммерческие предложения", Folder = "Работа/Коммерческие предложения", Keywords = "КП, коммерческое предложение, offer",
            NamePatterns = new[] { R(@"(^| )(кп|kp)( |$)"), R(@"коммерческ\w* предлож"), R(@"(commercial )?(offer|proposal)s?( |$)") },
            Categories = new[] { FileCategory.Documents, FileCategory.Spreadsheets, FileCategory.Presentations } },
        new() { Topic = "Договоры", Folder = "Работа/Договоры", Keywords = "договор, контракт, соглашение, contract",
            NamePatterns = new[] { R(@"(^| )(договор|контракт|соглашени|допсоглаш|доп соглаш|contract|agreement)") }, Categories = Docs },
        new() { Topic = "Счета", Folder = "Финансы/Счета", Keywords = "счёт, счет-фактура, invoice",
            NamePatterns = new[] { R(@"(^| )(сч|счет|счета|счет фактура|invoice|invoices|bill)( |$)"), R(@"счет\w* фактур") }, Categories = Docs },
        new() { Topic = "Акты", Folder = "Финансы/Акты", Keywords = "акт, УПД, накладная",
            NamePatterns = new[] { R(@"(^| )(акт|акты|упд|накладн\w*|торг 12|сверк\w*)( |$)") }, Categories = Docs },
        new() { Topic = "Чеки и квитанции", Folder = "Финансы/Чеки", Keywords = "чек, квитанция, receipt",
            NamePatterns = new[] { R(@"(^| )(чек|квитанци|receipt|оплат|payment)") }, Categories = Docs },
        new() { Topic = "Отчёты", Folder = "Работа/Отчёты", Keywords = "отчёт, report",
            NamePatterns = new[] { R(@"(^| )(отчет|отчеты|report)") },
            Categories = new[] { FileCategory.Documents, FileCategory.Spreadsheets, FileCategory.Presentations } },
        new() { Topic = "Протоколы и встречи", Folder = "Работа/Встречи", Keywords = "протокол, совещание, встреча, meeting",
            NamePatterns = new[] { R(@"(^| )(протокол|совещани|встреч|планерк)"), R(@"(^| )(meeting|meetings|minutes)( |$)") }, Categories = new[] { FileCategory.Documents } },
        new() { Topic = "Инструкции", Folder = "Работа/Инструкции", Keywords = "инструкция, руководство, manual, guide",
            NamePatterns = new[] { R(@"(^| )(инструкц|руководств|регламент|manual|guide|howto|how to|памятк)") }, Categories = new[] { FileCategory.Documents } },
        new() { Topic = "Резюме", Folder = "Личное/Резюме", Keywords = "резюме, CV",
            NamePatterns = new[] { R(@"(^| )(резюме|cv|resume)( |$)") }, Categories = new[] { FileCategory.Documents } },
        new() { Topic = "Личные документы", Folder = "Личное/Документы", Keywords = "паспорт, СНИЛС, ИНН, полис, свидетельство",
            NamePatterns = new[] { R(@"(^| )(паспорт\w*|снилс|инн|полис\w*|свидетельств\w*|passport|загран\w*)( |$)") }, Categories = Docs },
        new() { Topic = "Скриншоты", Folder = "Изображения/Скриншоты", Keywords = "screenshot, снимок экрана",
            NamePatterns = new[] { R(@"(^| )(screenshot|screen shot|снимок экрана|скриншот|скрин)"), R(@"^(scr|ss) \d") }, Categories = new[] { FileCategory.Images } },
        new() { Topic = "Сканы", Folder = "Документы/Сканы", Keywords = "скан, scan",
            NamePatterns = new[] { R(@"(^| )(скан|scan)") }, Categories = new[] { FileCategory.Documents, FileCategory.Images } },
        new() { Topic = "Фото с телефона", Folder = "Изображения/Фото", Keywords = "IMG_, DSC, PXL_, WhatsApp Image",
            NamePatterns = new[] { R(@"^(img|dsc|dscn|dcim|pxl|photo|фото)( |$|\d)"), R(@"whatsapp image"), R(@"^\d{8} \d{6}") }, Categories = new[] { FileCategory.Images } },
        new() { Topic = "Презентации", Folder = "Работа/Презентации", Keywords = "презентация, presentation",
            NamePatterns = new[] { R(@"(^| )(презентац|presentation|deck|pitch)") } },
    };
}
