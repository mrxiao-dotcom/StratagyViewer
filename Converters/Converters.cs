using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using StrategyViewer.Models;

namespace StrategyViewer.Converters;

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is Visibility v && v == Visibility.Visible;
    }
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class InverseNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class DirectionColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string direction)
        {
            return direction.Contains("多") || direction.Contains("买")
                ? new SolidColorBrush(Color.FromRgb(166, 227, 161))
                : new SolidColorBrush(Color.FromRgb(243, 139, 168));
        }
        return new SolidColorBrush(Color.FromRgb(69, 71, 90));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class PnLColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is decimal pnl)
        {
            return pnl > 0
                ? new SolidColorBrush(Color.FromRgb(166, 227, 161))
                : pnl < 0
                    ? new SolidColorBrush(Color.FromRgb(243, 139, 168))
                    : new SolidColorBrush(Color.FromRgb(205, 214, 244));
        }
        return new SolidColorBrush(Color.FromRgb(205, 214, 244));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class ResultColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is SignalResult result)
        {
            return result switch
            {
                SignalResult.Excellent => new SolidColorBrush(Color.FromRgb(166, 227, 161)),
                SignalResult.Good => new SolidColorBrush(Color.FromRgb(137, 180, 250)),
                SignalResult.Neutral => new SolidColorBrush(Color.FromRgb(249, 226, 175)),
                SignalResult.Poor => new SolidColorBrush(Color.FromRgb(250, 179, 135)),
                SignalResult.Failed => new SolidColorBrush(Color.FromRgb(243, 139, 168)),
                _ => new SolidColorBrush(Color.FromRgb(69, 71, 90))
            };
        }
        return new SolidColorBrush(Color.FromRgb(69, 71, 90));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToHitConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b && b)
        {
            return new SolidColorBrush(Color.FromRgb(243, 139, 168));
        }
        return new SolidColorBrush(Color.FromRgb(69, 71, 90));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
