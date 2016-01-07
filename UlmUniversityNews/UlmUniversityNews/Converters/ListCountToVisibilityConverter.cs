using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Der Converter ListCountToVisibilityConverter bildet die Menge an Elementen einer Liste
    /// auf einen Sichtbarkeitswert ab. Genauer bildet der Converter auf einen Wert vom Typ 
    /// Windows.UI.Xaml.Visibility ab. Der Converter wird genutzt, um einen Text anzuzeigen,
    /// wenn eine Liste keine Elemente besitzt. Bei einer leeren Liste wird also der Wert
    /// "Visible" zurückgeliefert.
    /// </summary>
    public class ListCountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value != null)
            {
                int listCount;
                bool successful = int.TryParse(value.ToString(), out listCount);
                if(successful && listCount != 0)
                {
                    return Windows.UI.Xaml.Visibility.Collapsed;
                }
            }
            return Windows.UI.Xaml.Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
