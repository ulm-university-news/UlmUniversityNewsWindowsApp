using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, die ein DateTimeOffset Objekt auf einen String abbildet. Dabei wird nur 
    /// die Uhrzeit ausgegeben, jedoch nicht das Datum.
    /// </summary>
    public class DateTimeToPureTimeStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTimeOffset dateTime = (DateTimeOffset)value;
            string timeString = String.Format("{0:t}", dateTime);
            return timeString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
