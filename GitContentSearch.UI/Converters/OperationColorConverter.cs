using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace GitContentSearch.UI.Converters;

public class OperationColorConverter : IValueConverter
{
    private const string LOCATE_COLOR = "#7E57C2";
    private const string SEARCH_COLOR = "#00A3D9";

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isLocateOperation)
        {
            return isLocateOperation ? LOCATE_COLOR : SEARCH_COLOR;
        }
        return SEARCH_COLOR;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
} 