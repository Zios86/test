using System.ComponentModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using FileOrganizer.App.ViewModels;

namespace FileOrganizer.App
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _vm;

        public MainWindow()
        {
            InitializeComponent();
            _vm = new MainViewModel();
            DataContext = _vm;
            Loaded += (_, _) => _vm.CheckInbox();
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (_vm.IsBusy && MessageBox.Show("Сейчас выполняется операция. Остановить её и закрыть программу?\n\nУже выполненные изменения сохранены в журнале и могут быть отменены.",
                    "Закрытие", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }
            _vm.StopCommand.Execute(null);
            Services.AppServices.Db.Flush();
            base.OnClosing(e);
        }
    }
}

namespace FileOrganizer.App.Views
{
    /// <summary>true, если оба значения — один и тот же объект (подсветка текущей страницы в меню).</summary>
    public sealed class ReferenceEqualsConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
            values.Length == 2 && ReferenceEquals(values[0], values[1]);
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => new[] { Binding.DoNothing, Binding.DoNothing };
    }
}
