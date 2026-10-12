using System.IO;
using System.Windows;
using FileOrganizer.Core.Analysis;
using FileOrganizer.Core.Classification;
using FileOrganizer.Core.Infrastructure;

namespace FileOrganizer.App.Views;

/// <summary>Сравнение двух файлов: размер, даты, совпадение по SHA-256, различия в тексте документов.</summary>
public partial class CompareWindow : Window
{
    public CompareWindow(string left, string right)
    {
        InitializeComponent();
        LeftTitle.Text = left;
        RightTitle.Text = right;
        LeftInfo.Text = Describe(left);
        RightInfo.Text = Describe(right);
        Loaded += async (_, _) =>
        {
            try
            {
                var (sameHash, l, r) = await Task.Run(() =>
                {
                    bool same = new FileInfo(left).Length == new FileInfo(right).Length && DuplicateFinder.FullHash(left) == DuplicateFinder.FullHash(right);
                    return (same, ContentExtractor.TryExtract(left, 50_000), ContentExtractor.TryExtract(right, 50_000));
                });
                LeftText.Text = l ?? "(текст недоступен для этого типа файла)";
                RightText.Text = r ?? "(текст недоступен для этого типа файла)";
                Verdict.Text = sameHash ? "Файлы полностью идентичны (SHA-256 совпадает)." : Diff(l, r);
            }
            catch (Exception ex)
            {
                Verdict.Text = "Не удалось сравнить: " + ex.Message;
            }
        };
    }

    private static string Describe(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            return $"Размер: {Format.Size(fi.Length)}   Изменён: {Format.Date(fi.LastWriteTimeUtc)}   Создан: {Format.Date(fi.CreationTimeUtc)}";
        }
        catch (Exception ex) { return ex.Message; }
    }

    private static string Diff(string? a, string? b)
    {
        if (a == null || b == null) return "Файлы различаются. Текст для сравнения недоступен.";
        var la = a.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var lb = b.Split(new[] { '.', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        var sb = new HashSet<string>(lb);
        var sa = new HashSet<string>(la);
        int common = la.Count(sb.Contains);
        double similarity = Math.Max(la.Count, lb.Count) == 0 ? 1 : (double)common / Math.Max(la.Count, lb.Count);
        var onlyA = la.Where(x => !sb.Contains(x)).Take(3).ToList();
        var onlyB = lb.Where(x => !sa.Contains(x)).Take(3).ToList();
        var text = $"Файлы различаются. Совпадает примерно {Format.Percent(similarity)} фрагментов текста.";
        if (onlyA.Count > 0) text += "\nТолько слева: " + string.Join(" | ", onlyA.Select(Short));
        if (onlyB.Count > 0) text += "\nТолько справа: " + string.Join(" | ", onlyB.Select(Short));
        return text;
    }

    private static string Short(string s) => s.Length > 80 ? s[..80] + "…" : s;

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
