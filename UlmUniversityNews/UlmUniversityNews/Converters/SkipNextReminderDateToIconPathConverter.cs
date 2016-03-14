using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche den Boolean-Wert des SkipNextReminder Properties auf 
    /// einen Pfad zum entsprechenden Icon abbildet.
    /// </summary>
    public class SkipNextReminderDateToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string iconPath = string.Empty;
            if (value != null)
            {
                bool input = (bool)value;
                if (input)
                {
                    iconPath = "/Assets/ResourceDetails/ic_notifications_off_black_36dp.png";
                }
                else
                {
                    iconPath = "/Assets/ResourceDetails/ic_notifications_black_36dp.png";
                }
            }
            return iconPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
