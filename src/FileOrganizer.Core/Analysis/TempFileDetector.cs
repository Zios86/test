using System.Text.RegularExpressions;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Временные и остаточные файлы: .tmp, недокачанные загрузки, старые установщики, архивы,
/// которые уже распакованы рядом, повторные загрузки «file (1).ext». Ничего не удаляется автоматически.
/// </summary>
public static partial class TempFileDetector
{
    [GeneratedRegex(@"^(?<base>.+?)\s*\((?<n>\d+)\)$")]
    private static partial Regex RepeatedDownload();

    public static List<TempFileInfo> Find(ScanResult scan, int installerAgeDays = 30, DateTime? nowUtc = null)
    {
        var now = nowUtc ?? DateTime.UtcNow;
        var result = new List<TempFileInfo>();
        foreach (var folder in scan.Folders)
        {
            var subNames = new HashSet<string>(folder.SubFolders.Select(s => s.Name), StringComparer.OrdinalIgnoreCase);
            var fileIndex = folder.Files.GroupBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
            foreach (var f in folder.Files)
            {
                var ext = f.Extension;
                if (f.Name.StartsWith("~$", StringComparison.Ordinal))
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.OfficeLockFile, Reason = "служебный файл блокировки Office (остаётся после сбоя)" });
                else if (ext is "tmp" or "temp" || f.Name.StartsWith("~", StringComparison.Ordinal))
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.TempFile, Reason = "временный файл" });
                else if (ext is "crdownload" or "part" or "partial" or "download" or "opdownload")
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.PartialDownload, Reason = "недокачанная загрузка" });
                else if (ext is "bak" or "old")
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.Backup, Reason = "резервная копия" });
                else if (f.Category == FileCategory.Installers && (now - f.LastWriteTimeUtc).TotalDays > installerAgeDays)
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.OldInstaller, Reason = $"установщик старше {installerAgeDays} дней — программа, скорее всего, уже установлена" });
                else if (f.Category == FileCategory.Archives && subNames.Contains(f.NameWithoutExtension))
                    result.Add(new TempFileInfo { File = f, Kind = TempFileKind.LeftoverArchive, Reason = $"рядом есть папка «{f.NameWithoutExtension}» — архив, вероятно, уже распакован" });
                else if (RepeatedDownload().Match(f.NameWithoutExtension) is { Success: true } m)
                {
                    var originalName = m.Groups["base"].Value + (ext.Length > 0 ? "." + ext : "");
                    if (fileIndex.TryGetValue(originalName, out var original) && original.Size == f.Size)
                        result.Add(new TempFileInfo { File = f, Kind = TempFileKind.RepeatedDownload, Reason = $"повторная загрузка файла «{originalName}» (тот же размер)" });
                }
            }
        }
        return result;
    }
}
