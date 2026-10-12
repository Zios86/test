using System.Text.RegularExpressions;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Models;

namespace FileOrganizer.Core.Analysis;

/// <summary>
/// Похожие папки: «Отчеты», «Отчёты», «Отчеты новые», «Отчеты2», «Старые отчеты», «Отчеты 2026».
/// Предлагается объединение в папку с наибольшим числом файлов; конфликты имён показываются заранее.
/// </summary>
public static partial class SimilarFolderFinder
{
    [GeneratedRegex(@"\b(нов\w*|стар\w*|new|old|копия|copy|backup|бэкап|резерв\w*|tmp|temp|\d+)\b", RegexOptions.IgnoreCase)]
    private static partial Regex Noise();

    public static string NormalizeName(string name)
    {
        var s = TextNormalizer.Normalize(TextNormalizer.SplitWords(name));
        s = Noise().Replace(s, " ");
        s = TextNormalizer.Normalize(s);
        return s;
    }

    public static List<SimilarFolderGroup> Find(ScanResult scan)
    {
        var groups = new List<SimilarFolderGroup>();
        // Сравниваем папки, лежащие внутри одного родителя — объединение «соседей» понятно пользователю.
        foreach (var parent in scan.Folders)
        {
            if (parent.SubFolders.Count < 2) continue;
            foreach (var g in parent.SubFolders
                         .Select(f => (f, key: NormalizeName(f.Name)))
                         .Where(x => x.key.Length >= 3)
                         .GroupBy(x => x.key)
                         .Where(g => g.Count() > 1))
            {
                var folders = g.Select(x => x.f).ToList();
                var target = folders.OrderByDescending(f => f.TotalFileCount).ThenBy(f => f.Name.Length).First();
                var conflicts = folders.SelectMany(f => AllFiles(f).Select(file => Path.GetRelativePath(f.FullPath, file.FullPath)).Distinct(StringComparer.OrdinalIgnoreCase))
                    .GroupBy(n => n, StringComparer.OrdinalIgnoreCase)
                    .Where(x => x.Count() > 1)
                    .Select(x => x.Key)
                    .ToList();
                groups.Add(new SimilarFolderGroup { NormalizedName = g.Key, Folders = folders, Target = target, ConflictingNames = conflicts });
            }
        }
        return groups;
    }

    public static IEnumerable<FileEntry> AllFiles(FolderEntry folder)
    {
        var stack = new Stack<FolderEntry>();
        stack.Push(folder);
        while (stack.Count > 0)
        {
            var f = stack.Pop();
            foreach (var file in f.Files) yield return file;
            foreach (var s in f.SubFolders) stack.Push(s);
        }
    }
}
