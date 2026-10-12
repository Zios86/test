using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// «Возможно лежат не там»: файлы, тематически выбивающиеся из папки.
/// Например, «Договор_поставки.pdf» в папке «Фото отпуска», где 95% файлов — фотографии.
/// </summary>
public static class MisplacedFinder
{
    public static List<MisplacedFileInfo> Find(ScanResult scan, SmartClassifier classifier, int minFolderFiles = 5, double dominance = 0.8)
    {
        var result = new List<MisplacedFileInfo>();
        foreach (var folder in scan.Folders)
        {
            if (folder.Files.Count < minFolderFiles) continue;
            var dominant = folder.Files.GroupBy(f => f.Category).OrderByDescending(g => g.Count()).First();
            double share = (double)dominant.Count() / folder.Files.Count;
            if (share < dominance || dominant.Key == FileCategory.Other) continue;

            foreach (var f in folder.Files)
            {
                if (f.Category == dominant.Key || f.Category is FileCategory.Other or FileCategory.Temporary) continue;
                // Сопутствующие файлы (субтитры, сайдкары, плейлисты) не считаем «чужими».
                if (f.Extension is "srt" or "ass" or "xmp" or "aae" or "nfo" or "m3u" or "cue" or "ini" or "db") continue;
                var c = classifier.Classify(f);
                var reason = $"в папке «{folder.Name}» {(int)Math.Round(share * 100)}% файлов — {dominant.Key.DisplayName().ToLowerInvariant()}, а этот файл — {f.Category.DisplayName().ToLowerInvariant()}";
                if (c.Source == ClassificationSource.FileName) reason += $"; по названию похож на «{c.Topic}»";
                result.Add(new MisplacedFileInfo
                {
                    File = f,
                    Reason = reason,
                    SuggestedFolder = c.TargetFolder,
                    Confidence = c.Source == ClassificationSource.FileName ? Math.Min(0.8, c.Confidence) : 0.6,
                });
            }
        }
        return result;
    }
}
