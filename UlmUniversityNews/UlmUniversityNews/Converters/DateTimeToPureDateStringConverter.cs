using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, die ein DateTime Objekt auf einen String abbildet. Dabei wird nur 
    /// das Datum ausgegeben, jedoch nicht die Uhrzeit.
    /// </summary>
    public class DateTimeToPureDateStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime dateTime = (DateTime)value;
            string dateString = String.Format("{0:d}", dateTime);
            return dateString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
