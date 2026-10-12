namespace FileOrganizer.Core.Models;

public enum FileCategory
{
    Other = 0,
    Documents,
    Spreadsheets,
    Presentations,
    Images,
    Video,
    Audio,
    Archives,
    Installers,
    Code,
    Temporary,
}

public static class FileCategoryNames
{
    /// <summary>Человекочитаемое имя категории, оно же имя папки при сортировке по типам.</summary>
    public static string DisplayName(this FileCategory c) => c switch
    {
        FileCategory.Documents => "Документы",
        FileCategory.Spreadsheets => "Таблицы",
        FileCategory.Presentations => "Презентации",
        FileCategory.Images => "Изображения",
        FileCategory.Video => "Видео",
        FileCategory.Audio => "Аудио",
        FileCategory.Archives => "Архивы",
        FileCategory.Installers => "Установщики",
        FileCategory.Code => "Код",
        FileCategory.Temporary => "Временные файлы",
        _ => "Прочее",
    };
}
