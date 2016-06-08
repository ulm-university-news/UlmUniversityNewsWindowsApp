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
    class GroupToGroupAdminStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string adminName = string.Empty;
            Group affectedGroup = value as Group;

            if (affectedGroup != null)
            {
                int adminId = affectedGroup.GroupAdmin;

                if (affectedGroup.Participants != null)
                {
                    foreach (User participant in affectedGroup.Participants)
                    {
                        if (participant.Id == adminId)
                        {
                            adminName = participant.Name;
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
