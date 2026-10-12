namespace FileOrganizer.Core.Execution;

/// <summary>Безопасное перемещение файла: никогда не перезаписывает существующий файл.</summary>
public static class FileMover
{
    public static void Move(string source, string destination)
    {
        if (File.Exists(destination) && !Infrastructure.PathUtil.SamePath(source, destination))
            throw new IOException($"Файл назначения уже существует: {destination}");

        var srcRoot = Path.GetPathRoot(Path.GetFullPath(source));
        var dstRoot = Path.GetPathRoot(Path.GetFullPath(destination));
        bool crossVolume = OperatingSystem.IsWindows() && !string.Equals(srcRoot, dstRoot, StringComparison.OrdinalIgnoreCase);
        if (!crossVolume)
        {
            File.Move(source, destination, overwrite: false);
            return;
        }

        // Между дисками: копия во временный файл → проверка размера → переименование → удаление исходника.
        var temp = destination + ".fo-partial";
        try
        {
            File.Copy(source, temp, overwrite: false);
            var srcInfo = new FileInfo(source);
            if (new FileInfo(temp).Length != srcInfo.Length) throw new IOException("Размер копии не совпадает с исходным файлом");
            File.SetLastWriteTimeUtc(temp, srcInfo.LastWriteTimeUtc);
            File.Move(temp, destination, overwrite: false);
        }
        catch
        {
            TryDelete(temp);
            throw;
        }
        try
        {
            var attrs = File.GetAttributes(source);
            if ((attrs & FileAttributes.ReadOnly) != 0) File.SetAttributes(source, attrs & ~FileAttributes.ReadOnly);
            File.Delete(source);
        }
        catch
        {
            // Исходник удалить не удалось (занят другим приложением) — убираем копию, чтобы не было двух файлов.
            TryDelete(destination);
            throw;
        }
    }

    private static void TryDelete(string path)
    {
        try { if (File.Exists(path)) File.Delete(path); } catch { /* ignore */ }
    }

    public static bool IsDirectoryEmpty(string path) => !Directory.EnumerateFileSystemEntries(path).Any();
}

/// <summary>Отправка в Корзину Windows. Реализуется в приложении (WPF), в ядре — не поддерживается.</summary>
public interface IRecycleBin
{
    bool IsSupported { get; }
    void SendToRecycleBin(string path);
}
