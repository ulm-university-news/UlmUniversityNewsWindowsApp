using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Ein Converter, der einen Integer-Wert auf eine Sichtbarkeit abbildet. Insbesondere
    /// bildet der Converter auf einen Typ von Windows.UI.Xaml.Visibility ab. Es wird die
    /// Sichtbarkeit "Collapsed" zurückgegeben, wenn der übergebene Integer-Wert 0 ist. 
    /// Ansonsten liefert der Converter den Wert "Visible" zurück.
    /// </summary>
    public class ZeroIntegerToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if(value != null)
            {
                int integerNumber;
                bool successful = int.TryParse(value.ToString(), out integerNumber);
                
                if(successful && integerNumber == 0)
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
