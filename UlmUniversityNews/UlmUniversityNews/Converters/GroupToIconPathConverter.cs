using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Bildet eine Instanz der Klasse Group auf einen Icon Pfad ab, der für diese Gruppe relevant ist.
    /// Benötigt über den Parameter noch das Nutzerobjekt des aktuellen lokalen Nutzers.
    /// </summary>
    public class GroupToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Group group = value as Group;
            string iconPath = string.Empty;

            if (group != null)
            {
               // GroupType type = group.GroupType;

                User currentLocalUser = parameter as User;
                if (currentLocalUser != null && group.GroupAdmin == currentLocalUser.Id)
                {
                    // Set administrator icon.
                    iconPath = "/Assets/groupIcons/g_admin.png";
                }
                else
                {
                    // Set normal group icon.
                    iconPath = "/Assets/groupIcons/g.png";
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
