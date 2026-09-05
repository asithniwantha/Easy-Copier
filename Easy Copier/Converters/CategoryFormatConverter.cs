using Easy_Copier.Models;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Easy_Copier.Converters
{
    public class CategoryFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return value is IReadOnlyList<GameCategory> categories && categories.Count > 0
                ? string.Join(", ", categories.Select(c => c.ToString()))
                : "Uncategorized";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
