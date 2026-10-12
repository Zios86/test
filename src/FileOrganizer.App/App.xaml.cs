using System.Windows;
using System.Windows.Threading;
using FileOrganizer.App.Services;
using FileOrganizer.Core.Execution;

namespace FileOrganizer.App;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
            new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage(System.Globalization.CultureInfo.CurrentCulture.IetfLanguageTag)));
        DispatcherUnhandledException += OnUnhandled;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            AppServices.Log.Error("Unhandled", null, args.ExceptionObject as Exception ?? new Exception("Неизвестная ошибка"));
        TaskScheduler.UnobservedTaskException += (_, args) => { AppServices.Log.Error("UnobservedTask", null, args.Exception); args.SetObserved(); };

        try
        {
            AppServices.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show("Не удалось открыть базу данных программы:\n" + ex.Message, "Умный файловый организатор", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }
        ThemeManager.Apply(AppServices.Settings.Theme);

        // Восстановление после аварийного завершения: незавершённые операции сверяются с диском.
        try
        {
            var fixedOps = new RollbackManager(AppServices.Db, AppServices.Log).RecoverInterrupted();
            if (fixedOps > 0)
                MessageBox.Show($"Программа была закрыта во время выполнения изменений. Состояние {fixedOps} операций проверено по диску.\n\n" +
                                "Прерванный пакет отмечен в «Истории» — его можно отменить.", "Восстановление после сбоя", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppServices.Log.Error("RecoverInterrupted", null, ex);
        }

        var window = new MainWindow();
        MainWindow = window;
        window.Show();
    }

    private void OnUnhandled(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppServices.Log.Error("Dispatcher", null, e.Exception);
        MessageBox.Show("Произошла ошибка: " + e.Exception.Message + "\n\nПрограмма продолжит работу. Подробности — в журнале.",
            "Умный файловый организатор", MessageBoxButton.OK, MessageBoxImage.Warning);
        e.Handled = true;
    }
}
