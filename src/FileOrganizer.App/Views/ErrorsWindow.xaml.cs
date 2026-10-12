using System.Windows;
using FileOrganizer.Core.Models;

namespace FileOrganizer.App.Views;

public partial class ErrorsWindow : Window
{
    public ErrorsWindow(IEnumerable<ScanError> errors)
    {
        InitializeComponent();
        var list = errors.Select(e => new
        {
            e.Path,
            Kind = e.Kind switch
            {
                ScanErrorKind.AccessDenied => "Нет доступа",
                ScanErrorKind.PathTooLong => "Слишком длинный путь",
                ScanErrorKind.NotFound => "Не найден",
                ScanErrorKind.IoError => "Ошибка чтения (файл занят?)",
                _ => "Ошибка",
            },
            e.Message,
        }).ToList();
        Header.Text = $"Не удалось прочитать: {list.Count}. Остальные файлы проанализированы.";
        List.ItemsSource = list;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
