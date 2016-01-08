using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Wandelt ein übergebenes DateTime Objekt in einen formatierten String um.
    /// </summary>
    public class DateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTime dateTime = (DateTime)value;
            string dateString = string.Empty;
            
            if(dateTime.Date != DateTime.Now.Date)
            {
                // Zeige Datum nur an, wenn das übergebene Datum nicht das aktuelle Datum ist.
                dateString = dateString + String.Format("{0:d} \n", dateTime);
            }
            dateString = dateString + String.Format("{0:t}", dateTime);

            return dateString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
