using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FileOrganizer.Core.Infrastructure;
using FileOrganizer.Core.Models;
using FileOrganizer.Core.Planning;

namespace FileOrganizer.App.Converters;

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public bool Invert { get; set; }
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool b = value switch { bool x => x, int i => i != 0, long l => l != 0, string s => !string.IsNullOrEmpty(s), Enum e => System.Convert.ToInt32(e) != 0, null => false, _ => true };
        if (Invert) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : true;
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b ? !b : false;
}

public sealed class SizeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is long l ? Format.Size(l) : value is int i ? Format.Size(i) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class NumberConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is int i ? Format.Number(i) : value is long l ? Format.Number(l) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class DateConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is DateTime d ? Format.Date(d) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class PercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is double d ? Format.Percent(d) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Цвет пометки изменения (переместить / переименовать / объединить / удалить / создать / оставить).</summary>
public sealed class ChangeMarkToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var key = value switch
        {
            ChangeMark.Move => "MarkMove",
            ChangeMark.Rename => "MarkRename",
            ChangeMark.Merge => "MarkMerge",
            ChangeMark.Delete => "MarkDelete",
            ChangeMark.Create => "MarkCreate",
            OperationKind.Move => "MarkMove",
            OperationKind.Rename => "MarkRename",
            OperationKind.MoveToRecovery => "MarkDelete",
            OperationKind.DeleteEmptyFolder => "MarkDelete",
            OperationKind.CreateFolder => "MarkCreate",
            _ => "TextPrimary",
        };
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class ChangeMarkToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is ChangeMark m && m != ChangeMark.Keep ? PreviewNode.MarkName(m) : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class ConfidenceToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var level = value is ConfidenceLevel l ? l : value is double d ? (d >= 0.85 ? ConfidenceLevel.High : d >= 0.6 ? ConfidenceLevel.Medium : ConfidenceLevel.Low) : ConfidenceLevel.Low;
        var key = level switch { ConfidenceLevel.High => "SuccessBrush", ConfidenceLevel.Medium => "WarningBrush", _ => "DangerBrush" };
        return Application.Current.TryFindResource(key) as Brush ?? Brushes.Gray;
    }
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class OperationKindToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        OperationKind.Move => "Переместить",
        OperationKind.Rename => "Переименовать",
        OperationKind.CreateFolder => "Создать папку",
        OperationKind.MoveToRecovery => "В папку восстановления",
        OperationKind.DeleteEmptyFolder => "Удалить пустую папку",
        _ => value?.ToString() ?? "",
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class OperationStatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        OperationStatus.Done => "Выполнено",
        OperationStatus.Failed => "Ошибка",
        OperationStatus.Skipped => "Пропущено",
        OperationStatus.Pending => "Не завершено",
        OperationStatus.RolledBack => "Отменено",
        OperationStatus.RollbackFailed => "Не удалось отменить",
        OperationStatus.Purged => "Удалено окончательно",
        BatchStatus.Completed => "Выполнено",
        BatchStatus.CompletedWithErrors => "Выполнено с ошибками",
        BatchStatus.Cancelled => "Остановлено",
        BatchStatus.Interrupted => "Прервано (сбой)",
        BatchStatus.RolledBack => "Отменено",
        BatchStatus.PartiallyRolledBack => "Частично отменено",
        BatchStatus.Running => "Выполняется",
        _ => value?.ToString() ?? "",
    };
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

public sealed class CategoryToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is FileCategory c ? c.DisplayName() : "";
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Путь относительно выбранной папки — короче и понятнее.</summary>
public sealed class RelativePathConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not string path || values[1] is not string root || root.Length == 0) return values.Length > 0 ? values[0] ?? "" : "";
        if (path.StartsWith(OperationsExtensions.RecycleBinPrefix, StringComparison.Ordinal)) return "Корзина Windows";
        return PathUtil.IsUnder(path, root) ? System.IO.Path.GetRelativePath(root, path) : path;
    }
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => Array.Empty<object>();
}

internal static class OperationsExtensions
{
    public const string RecycleBinPrefix = FileOrganizer.Core.Execution.OperationExecutor.RecycleBinPrefix;
}

/// <summary>Показывает «пусто», только если анализ выполнен и список пуст.</summary>
public sealed class EmptyStateVisibilityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) =>
        values.Length == 2 && values[0] is true && values[1] is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) => Array.Empty<object>();
}
