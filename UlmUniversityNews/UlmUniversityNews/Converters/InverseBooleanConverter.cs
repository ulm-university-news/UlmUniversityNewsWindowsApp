using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter, der den Wert einer Boolean Variable invertiert.
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool valueBool = (bool)value;
            return !valueBool;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            bool valueBool = (bool)value;
            return !valueBool;
        }
    }
}
