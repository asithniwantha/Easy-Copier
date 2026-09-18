using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;

namespace Easy_Copier.Converters
{
    /// <summary>
    /// Converts a boolean indicating negative value status to a red text foreground brush.
    /// </summary>
    public class NegativeToForegroundConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isNegative && isNegative)
            {
                // A strong red for foreground text
                return new SolidColorBrush(Colors.Red);
            }
            return DependencyProperty.UnsetValue;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a boolean indicating negative value status to a subtle red background brush adapted to light/dark theme.
    /// </summary>
    public class NegativeToBackgroundConverter : IValueConverter
    {
        /// <inheritdoc />
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isNegative && isNegative)
            {
                bool isDarkMode = Application.Current.RequestedTheme == ApplicationTheme.Dark;

                // Using a transparent/light red for the background that works well on both themes
                if (isDarkMode)
                {
                    return new SolidColorBrush(ColorHelper.FromArgb(50, 255, 0, 0)); // Very dim red for dark mode
                }
                else
                {
                    return new SolidColorBrush(ColorHelper.FromArgb(30, 255, 0, 0)); // Very light red for light mode
                }
            }
            return DependencyProperty.UnsetValue;
        }

        /// <inheritdoc />
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
