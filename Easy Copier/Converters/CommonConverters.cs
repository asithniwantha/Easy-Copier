using Easy_Copier.Models;
using Easy_Copier.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;

namespace Easy_Copier.Converters
{
    /// <summary>
    /// Converts a <see cref="LibraryCategory"/> enum value to a UI <see cref="Visibility"/> status,
    /// returning <see cref="Visibility.Visible"/> if the category is <see cref="LibraryCategory.Game"/>.
    /// </summary>
    public class GameCategoryToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is LibraryCategory category
                ? category == LibraryCategory.Game ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed
                : Microsoft.UI.Xaml.Visibility.Collapsed;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a byte size into a formatted price string based on user settings or size brackets.
    /// </summary>
    public class GameSizeToPriceConverter : IValueConverter
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a byte count (<see cref="long"/>) into a human-readable file size string (e.g., "1.5 GB").
    /// </summary>
    public class BytesToSizeConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is long bytes ? Infrastructure.FormattingHelpers.FormatBytes(bytes) : "0 B";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts an object reference (null vs non-null) to a UI <see cref="Visibility"/> status, supporting optional inversion via the converter parameter ("Invert").
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isNull = value == null;
            if (parameter is string param && param.Equals("Invert", StringComparison.OrdinalIgnoreCase))
            {
                isNull = !isNull;
            }
            return isNull ? Visibility.Collapsed : Visibility.Visible;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean value to a formatted string using a pipe-separated string parameter ("TrueText|FalseText").
    /// </summary>
    public class BoolToStringConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && parameter is string param)
            {
                string[] parts = param.Split('|');
                if (parts.Length == 2)
                {
                    return b ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a local file system path string to a <see cref="Microsoft.UI.Xaml.Media.Imaging.BitmapImage"/> for UI image bindings.
    /// </summary>
    public class PathToImageSourceConverter : IValueConverter
    {
        /// <inheritdoc />
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

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a domain <see cref="ValidationSeverity"/> enum value to a WinUI <see cref="InfoBarSeverity"/> value.
    /// </summary>
    public class SeverityToInfoBarSeverityConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is ValidationSeverity severity
                ? severity switch
                {
                    ValidationSeverity.Success => InfoBarSeverity.Success,
                    ValidationSeverity.Error => InfoBarSeverity.Error,
                    ValidationSeverity.Warning => InfoBarSeverity.Warning,
                    ValidationSeverity.Info => InfoBarSeverity.Informational,
                    _ => InfoBarSeverity.Informational
                }
                : InfoBarSeverity.Informational;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean indicating ascending sort direction to a Segoe MDL2 Assets glyph character string for sort direction indicators.
    /// </summary>
    public class SortDirectionGlyphConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // UpArrow =  (Ascending), DownArrow =  (Descending)
            return value is bool isAscending && isAscending ? "" : "";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean indicating ascending sort direction to a localized or descriptive tooltip string ("Ascending" or "Descending").
    /// </summary>
    public class SortDirectionToolTipConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is bool isAscending && isAscending ? "Ascending" : "Descending";
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
