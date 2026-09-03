using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using System;
using Windows.UI;

namespace Easy_Copier.Converters
{
    public class NegativeToForegroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isNegative && isNegative)
            {
                // A strong red for foreground text
                return new SolidColorBrush(Colors.Red);
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class NegativeToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool isNegative && isNegative)
            {
                bool isDarkMode = Application.Current.RequestedTheme == ApplicationTheme.Dark;

                // Using a transparent/light red for the background that works well on both themes
                if (isDarkMode)
                {
                    return new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)); // Very dim red for dark mode
                }
                else
                {
                    return new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)); // Very light red for light mode
                }
            }
            return DependencyProperty.UnsetValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
