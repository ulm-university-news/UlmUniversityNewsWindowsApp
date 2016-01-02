using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Ein Konverter, der den übergebenen Wert auf einen Sichtbarkeitswert vom Typ Windows.UI.Xaml.Visibility abbildet.
    /// Ist der übergebene Wert ein Null-Wert, so wird die Sichtbarkeit auf Collapsed gestellt, ansonsten auf Visible.
    /// </summary>
    public class NullValuetoVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Windows.UI.Xaml.Visibility visibility;
            if (value == null)
            {
                visibility = Windows.UI.Xaml.Visibility.Collapsed;
            }
            else
            {
                visibility = Windows.UI.Xaml.Visibility.Visible;
            }

            System.Diagnostics.Debug.WriteLine("NullValuetoVisibilityConverter returns: " + visibility);
            return visibility;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
