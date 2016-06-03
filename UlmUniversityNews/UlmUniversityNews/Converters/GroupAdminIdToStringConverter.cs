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
    /// Konvertierung einer Admin-Id in den Namen des entsprechenden Teilnehmers der Gruppe.
    /// Hierfür ist es notwendig die Teilnehmerliste als Parameter zu übergeben.
    /// </summary>
    class GroupAdminIdToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string adminName = string.Empty;

            // Extrahiere Teilnehmer aus Converter-Parameter:
            List<User> participants = parameter as List<User>;
            
            if (participants != null)
            {
                int adminId = -1;
                bool parsedSuccessful = int.TryParse(value as string, out adminId);

                if (parsedSuccessful)
                {
                    for (int i = 0; i < participants.Count; i++)
                    {
                        if (participants[i].Id == adminId)
                        {
                            adminName = participants[i].Name;
                        }
                    }
                }
            }

            return adminName;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
