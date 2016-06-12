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
    /// Konverter-Klasse, welche die Daten einer Konversation auf einen Pfad für ein
    /// entsprechendes Icon abbildet. Benötigt das Nutzerobjekt des aktuellen lokalen Nutzers, um prüfen zu können,
    /// ob es sich beim lokalen Nutzer um den Administrator handelt.
    /// </summary>
    public class ConversationToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Conversation conversation = value as Conversation;
            string iconPath = string.Empty;

            if (conversation != null)
            {
                // Prüfe, ob der lokale Nutzer Administrator dieser Konversation ist.
                User currentLocalUser = LocalUser.GetInstance().GetCachedLocalUserObject();
                bool isAdmin = false;
                if (conversation.AdminId == currentLocalUser.Id)
                    isAdmin = true;

                bool closedFieldHasValue = conversation.IsClosed.HasValue;
                if (closedFieldHasValue && conversation.IsClosed == false && !isAdmin)
                {
                    iconPath = "/Assets/conversationIcons/conversation.png";
                }
                else if (closedFieldHasValue && conversation.IsClosed == false && isAdmin)
                {
                    iconPath = "/Assets/conversationIcons/conversation_admin.png";
                }
                else if (closedFieldHasValue && conversation.IsClosed == true && !isAdmin)
                {
                    iconPath = "/Assets/conversationIcons/conversation_closed_white.png";
                }
                else if (closedFieldHasValue && conversation.IsClosed == true && isAdmin)
                {
                    iconPath = "/Assets/conversationIcons/conversation_closed_admin_white.png";
                }
                else
                {
                    iconPath = "/Assets/conversationIcons/conversation.png";
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
