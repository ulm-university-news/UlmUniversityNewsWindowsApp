using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{

    /// <summary>
    /// Wandelt ein übergebenes DateTimeOffset Objekt in einen formatierten String um.
    /// </summary>
    public class ReminderDateTimeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            DateTimeOffset dateTime = (DateTimeOffset)value;
            string dateString = string.Empty;

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
