using Avalonia.Data.Converters;
using System;
using System.Globalization;
using System.Linq;

namespace GitContentSearch.UI.Converters
{
    public class FileNameValidationConverter : IValueConverter
    {
        private static readonly char[] InvalidCharacters = { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string fileName)
            {
                return !fileName.Any(c => InvalidCharacters.Contains(c));
            }
            return true;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 