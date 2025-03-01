using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace GitContentSearch.UI.Converters;

public class BoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isTrue && parameter is string text)
        {
            return isTrue ? text : "Start Search";
        }
        return "Start Search";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 