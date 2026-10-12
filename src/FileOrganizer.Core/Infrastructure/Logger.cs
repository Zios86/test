using System.Text;

namespace FileOrganizer.Core.Infrastructure;

/// <summary>
/// Технический журнал: операция, путь, результат, ошибка, время.
/// Содержимое документов пользователя в журнал не пишется никогда.
/// </summary>
public sealed class Logger
{
    private readonly string _dir;
    private readonly object _lock = new();

    public static Logger Null { get; } = new(null);

    public Logger(string? directory) { _dir = directory ?? ""; }

    public string? CurrentFile => _dir.Length == 0 ? null : Path.Combine(_dir, $"{DateTime.Now:yyyy-MM-dd}.log");

    public void Info(string operation, string? path = null, string result = "OK") => Write("INFO", operation, path, result, null);
    public void Warn(string operation, string? path, string error) => Write("WARN", operation, path, "WARN", error);
    public void Error(string operation, string? path, Exception ex) => Write("ERROR", operation, path, "FAILED", $"{ex.GetType().Name}: {ex.Message}");
    public void Error(string operation, string? path, string error) => Write("ERROR", operation, path, "FAILED", error);

    private void Write(string level, string operation, string? path, string result, string? error)
    {
        var file = CurrentFile;
        if (file == null) return;
        var sb = new StringBuilder();
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")).Append('\t')
          .Append(level).Append('\t').Append(operation).Append('\t')
          .Append(path ?? "-").Append('\t').Append(result);
        if (error != null) sb.Append('\t').Append(error.Replace('\n', ' ').Replace('\r', ' '));
        sb.Append(Environment.NewLine);
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(_dir);
                File.AppendAllText(file, sb.ToString(), Encoding.UTF8);
            }
        }
        catch
        {
            // Журнал не должен ломать работу программы.
        }
    }
}
