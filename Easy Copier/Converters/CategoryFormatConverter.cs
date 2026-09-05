using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using Easy_Copier.Models;

namespace Easy_Copier.Converters
{
    public class CategoryFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is IReadOnlyList<GameCategory> categories && categories.Count > 0)
            {
                return string.Join(", ", categories.Select(c => c.ToString()));
            }
            return "Uncategorized";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
