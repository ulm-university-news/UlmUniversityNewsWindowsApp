using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche ein Objekt vom Typ DateTimeOffset auf einen Datums- und Zeitstring
    /// abbildet. Dieser wird für die Anzeige in den Ressourcen-Details Ansichten verwendet.
    /// </summary>
    public class ResourceDetailsDateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTimeOffset dateTime = (DateTimeOffset)value;
            string dateString = string.Empty;

            // Zeige Datum und Zeit an.
            dateString = dateString + String.Format("{0:d} - ", dateTime);
            dateString = dateString + String.Format("{0:t}", dateTime);

            return dateString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
