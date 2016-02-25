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
    /// Sonderbehandlung über den ConverterParameter. Wenn ConverterParameter gleich 'ScrollBar' wird
    /// eine Sichtbarkeit des Typs Windows.UI.Xaml.Controls.ScrollBarVisibility zurück gegeben.
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

            // Sonderbehandlung. Wenn Parameter übergeben wurde und dieser anzeigt, dass ein
            // anderer Sichtbarkeitstyp zurückgegeben werden soll.
            if (parameter != null && parameter.ToString() == "ScrollBar")
            {
                if (input)
                {
                    return Windows.UI.Xaml.Controls.ScrollBarVisibility.Visible;
                }
                return Windows.UI.Xaml.Controls.ScrollBarVisibility.Hidden;
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
            if (parameter != null && parameter.ToString() == "ScrollBar")
            {
                Windows.UI.Xaml.Controls.ScrollBarVisibility scrollBarVisibility = (Windows.UI.Xaml.Controls.ScrollBarVisibility)value;
                if(scrollBarVisibility == Windows.UI.Xaml.Controls.ScrollBarVisibility.Visible)
                {
                    return true;
                }
                return false;
            }

            Windows.UI.Xaml.Visibility visibility = (Windows.UI.Xaml.Visibility) value;
            if (visibility == Windows.UI.Xaml.Visibility.Visible)
            {
                return true;
            }
            return false;
        }
    }
}
