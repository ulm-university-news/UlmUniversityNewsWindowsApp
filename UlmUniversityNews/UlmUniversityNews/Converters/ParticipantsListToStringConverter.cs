using DataHandlingLayer.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konvertierung der Teilnehmerliste in einen String.
    /// </summary>
    public class ParticipantsListToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            StringBuilder sb = new StringBuilder();
            List<User> participants = value as List<User>;

            if (participants != null)
            {
                foreach (User participant in participants)
                {
                    sb.AppendLine(participant.Name);
                }
            }

            return sb.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
