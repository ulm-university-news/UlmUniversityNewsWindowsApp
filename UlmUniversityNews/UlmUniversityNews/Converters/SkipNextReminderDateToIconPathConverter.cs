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
                    iconPath = "/Assets/ResourceDetailsIcons/NotificationFalseInfo.png";
                }
                else
                {
                    iconPath = "/Assets/ResourceDetailsIcons/NotificationTrueInfo.png";
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
