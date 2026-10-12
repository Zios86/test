using System.Globalization;

namespace FileOrganizer.Core.Infrastructure;

public static class Format
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    public static string Size(long bytes)
    {
        string[] units = { "Б", "КБ", "МБ", "ГБ", "ТБ" };
        double v = bytes;
        int u = 0;
        while (v >= 1024 && u < units.Length - 1) { v /= 1024; u++; }
        return u == 0 ? $"{bytes} {units[0]}" : v.ToString(v >= 100 ? "0" : "0.#", Ru) + " " + units[u];
    }

    public static string Number(long n) => n.ToString("#,0", Ru).Replace(' ', ' ');

    public static string Date(DateTime utc) => utc.ToLocalTime().ToString("dd.MM.yyyy HH:mm", Ru);

    public static string Percent(double v) => ((int)Math.Round(v * 100)).ToString(Ru) + "%";
}
