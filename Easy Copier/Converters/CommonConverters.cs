using Easy_Copier.Models;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Easy_Copier.Converters
{
    public class BytesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = bytes;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len = len / 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
            return "0 B";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool invert = parameter?.ToString() == "Invert";
            bool boolValue = value is bool b && b;

            if (invert)
                boolValue = !boolValue;

            return boolValue ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class PathToImageSourceConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string path && !string.IsNullOrEmpty(path))
            {
                try
                {
                    if (System.IO.File.Exists(path) && Uri.TryCreate(path, UriKind.Absolute, out var uri))
                    {
                        return new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(uri);
                    }
                }
                catch
                {
                    // Malformed path/URI - fall through and return no value so no image is shown.
                }
            }
            // x:Bind generated code casts the converter's return value directly to the
            // target property type (ImageSource), so DependencyProperty.UnsetValue cannot
            // be used here as it can be with classic {Binding}. Return null instead.
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class SeverityToInfoBarSeverityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is ValidationSeverity severity)
            {
                return severity switch
                {
                    ValidationSeverity.Error => InfoBarSeverity.Error,
                    ValidationSeverity.Warning => InfoBarSeverity.Warning,
                    ValidationSeverity.Info => InfoBarSeverity.Informational,
                    _ => InfoBarSeverity.Informational
                };
            }
            return InfoBarSeverity.Informational;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
