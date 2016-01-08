using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Die Converter Klasse MessageReadPropertyToBackgroundColorConverter bildet einen Wert des
    /// Boolean-Flag IsRead einer Message auf eine Farbe ab. Mittels diesem Converter kann man einfach
    /// den Hintergrund einer ungelesenen Nachricht ändern und dadurch die Nachricht hervorheben.
    /// </summary>
    public class MessageReadPropertyToBackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool isRead = (bool)value;
            if(isRead)
            {
                return App.Current.Resources["UniUlmAccentColorListItemBackground"];
            }
            return App.Current.Resources["UnreadMessageBackgroundColor"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
