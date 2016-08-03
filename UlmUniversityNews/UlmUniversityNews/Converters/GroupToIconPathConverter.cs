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
    /// Benötigt das Nutzerobjekt des aktuellen lokalen Nutzers, um prüfen zu können, ob es sich beim lokalen 
    /// Nutzer um den Administrator handelt.
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

                // Prüfe, ob der lokale Nutzer Admin ist.
                User currentLocalUser = LocalUser.GetInstance().GetCachedLocalUserObject();
                if (currentLocalUser != null && group.GroupAdmin == currentLocalUser.Id)
                {
                    if (group.Type == GroupType.WORKING)
                    {
                        // Setze Administrator icon.
                        iconPath = "/Assets/groupIcons/group_working_admin.png";
                    }
                    else
                    {
                        // Setze Administrator icon.
                        iconPath = "/Assets/groupIcons/group_tutorial_admin.png";
                    }
                }
                else
                {
                    if (group.Type == GroupType.WORKING)
                    {
                        // Setze normales group icon.
                        iconPath = "/Assets/groupIcons/group_working.png";
                    }
                    else
                    {
                        // Setze normales group icon.
                        iconPath = "/Assets/groupIcons/group_tutorial.png";
                    }
                }

                if (group.Deleted)
                {
                    iconPath = "/Assets/channelIcons/ic_deleted.png";
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
