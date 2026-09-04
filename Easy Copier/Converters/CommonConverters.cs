using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Easy_Copier.Converters
{
    public class GameCategoryToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is LibraryCategory category
                ? category == LibraryCategory.Game ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed
                : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class GameSizeToPriceConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is long bytes)
            {
                double gb = bytes / (1024.0 * 1024.0 * 1024.0);

                try
                {
                    // Access settings synchronously to prevent UI thread blocking or deadlocking
                    AppSettings settings = new();

                    if (Application.Current is App app)
                    {
                        ISettingsService settingsService = app.Services.GetRequiredService<ISettingsService>();
                        settings = settingsService.LoadSettingsSync();
                    }

                    return $"Rs. {Easy_Copier.Infrastructure.FormattingHelpers.CalculatePrice(bytes, settings)}";
                }
                catch
                {
                    // Fallback to default prices if service unavailable
                    return gb <= 5.0 ? "Rs. 100" : gb <= 10.0 ? "Rs. 200" : gb < 16.0 ? "Rs. 300" : "Rs. 400";
                }
            }
            return "Rs. -";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BytesToSizeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is long bytes ? Infrastructure.FormattingHelpers.FormatBytes(bytes) : "0 B";
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
            {
                boolValue = !boolValue;
            }

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
                    if (System.IO.File.Exists(path) && Uri.TryCreate(path, UriKind.Absolute, out Uri? uri))
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
            return value is ValidationSeverity severity
                ? severity switch
                {
                    ValidationSeverity.Error => InfoBarSeverity.Error,
                    ValidationSeverity.Warning => InfoBarSeverity.Warning,
                    ValidationSeverity.Info => InfoBarSeverity.Informational,
                    _ => InfoBarSeverity.Informational
                }
                : InfoBarSeverity.Informational;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
