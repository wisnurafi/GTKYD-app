using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace GetToKnowYourDevice.App.Converters;

/// <summary>true -> Visible, false -> Collapsed. Invert with parameter "invert".</summary>
public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var b = value is bool x && x;
        if (parameter is string s && s.Equals("invert", StringComparison.OrdinalIgnoreCase)) b = !b;
        return b ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => value is Visibility v && v == Visibility.Visible;
}

/// <summary>null/empty -> Collapsed, otherwise Visible.</summary>
public sealed class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        var has = value switch
        {
            null => false,
            string s => !string.IsNullOrWhiteSpace(s),
            _ => true
        };
        return has ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}

/// <summary>Renders any value, substituting "Unavailable" for null/empty.</summary>
public sealed class UnavailableConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
        => value switch
        {
            null => "Unavailable",
            string s when string.IsNullOrWhiteSpace(s) => "Unavailable",
            _ => value
        };

    public object ConvertBack(object value, Type targetType, object parameter, string language)
        => throw new NotSupportedException();
}
