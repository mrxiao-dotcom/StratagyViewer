using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace StrategyViewer.Converters;

public class GroupHeaderConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StrategyViewer.Models.StrategyGroup group)
        {
            return group.IsExpanded ? "▼" : "▶";
        }
        return "▶";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GroupHeaderVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StrategyViewer.Models.StrategyGroup group && group.Key.Contains("_"))
        {
            return Visibility.Collapsed;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class StrategyItemVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StrategyViewer.Models.StrategyListItem item)
        {
            return item.Id < 0 ? Visibility.Collapsed : Visibility.Visible;
        }
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class GroupItemPaddingConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is StrategyViewer.Models.StrategyGroup group)
        {
            if (!group.Key.Contains("_"))
            {
                return new Thickness(8, 8, 8, 4);
            }
            return new Thickness(24, 4, 4, 2);
        }
        return new Thickness(4);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
