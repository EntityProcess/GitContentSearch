using Avalonia.Data.Converters;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace GitContentSearch.UI.Converters;

public class OperationButtonTextConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count >= 2 && values[0] is bool isSearching && values[1] is bool isLocateOperation)
        {
            if (!isSearching)
            {
                return "Start Search";
            }
            else
            {
                return isLocateOperation ? "Stop Locate" : "Stop Search";
            }
        }
        
        return "Start Search";
    }
} 