using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konvertiert einen Boolean Eingabewert auf einen Wert vom Typ Windows.UI.Xaml.Visibility. 
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool input = false;
            if(value != null)
            {
                input = (bool)value;
            }
            // Gebe die Visibility an abhängig vom Eingabewert.
            if (input)
            {
                return Windows.UI.Xaml.Visibility.Visible;
            }
            return Windows.UI.Xaml.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Windows.UI.Xaml.Visibility visibility = (Windows.UI.Xaml.Visibility) value;
            if(visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return true;
            }
            return false;
        }
    }
}
