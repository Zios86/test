using System.Text;
using FileOrganizer.Core.Data;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Scanning;

namespace FileOrganizer.Core.Tests;

/// <summary>Временная папка, удаляемая после теста.</summary>
public sealed class TempDir : IDisposable
{
    public string Path { get; }

    public TempDir()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "fo-tests", Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(Path);
    }

    public string File(string relative, string content = "x", DateTime? modified = null)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllText(full, content, new UTF8Encoding(false));
        if (modified != null) System.IO.File.SetLastWriteTimeUtc(full, modified.Value);
        return full;
    }

    public string Bytes(string relative, byte[] content)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full)!);
        System.IO.File.WriteAllBytes(full, content);
        return full;
    }

    public string Dir(string relative)
    {
        var full = System.IO.Path.Combine(Path, relative);
        Directory.CreateDirectory(full);
        return full;
    }

    public string P(string relative) => System.IO.Path.Combine(Path, relative);

    public ScanResult Scan(ScanOptions? options = null) => new Scanner().Scan(Path, options ?? new ScanOptions());

    public Database Db() => new(System.IO.Path.Combine(Path + "-db", "test.db"));

    public void Dispose()
    {
        foreach (var p in new[] { Path, Path + "-db" })
        {
            try
            {
                if (!Directory.Exists(p)) continue;
                foreach (var f in Directory.EnumerateFiles(p, "*", SearchOption.AllDirectories))
                    System.IO.File.SetAttributes(f, FileAttributes.Normal);
                Directory.Delete(p, true);
            }
            catch { /* ignore */ }
        }
    }
}

public static class Snapshot
{
    /// <summary>Снимок дерева: относительный путь → содержимое файла (папки — с пометкой).</summary>
    public static SortedDictionary<string, string> Take(string root)
    {
        var d = new SortedDictionary<string, string>(StringComparer.Ordinal);
        foreach (var dir in Directory.EnumerateDirectories(root, "*", SearchOption.AllDirectories))
            d[System.IO.Path.GetRelativePath(root, dir) + "/"] = "<dir>";
        foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            d[System.IO.Path.GetRelativePath(root, f)] = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(f)));
        return d;
    }
}
