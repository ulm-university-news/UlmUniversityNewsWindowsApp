using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Die Klasse StringToVisibilityConverter ist eine Konverter-Klasse, die 
    /// einen String auf eine Sichtbarkeit abbildet. Die Sichtbarkeit ist vom Typ
    /// Windows.UI.Xaml.Visibility. Es wird Collapsed nur in den Fällen zurückgegeben, 
    /// wenn entweder null oder ein leerer String übergeben wurde. Alle anderen Strings 
    /// liefern Visible als Ergebnis. Der Konverter kann verwendet werden, um Textfelder 
    /// für Validierungsfehler auszublenden, wenn kein Validierungsfehler vorliegt.
    /// </summary>
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string input = value as string;

            if (input == null || input == string.Empty)
            {
                return Windows.UI.Xaml.Visibility.Collapsed;
            }

            return Windows.UI.Xaml.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
