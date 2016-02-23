using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse AppSettings repräsentiert die für die Anwendung geltenden Einstellungen.
    /// </summary>
    public class AppSettings
    {
        #region Properties
        private OrderOption channelOrderSetting;
        /// <summary>
        /// Die Einstellung für die Anordung der Kanäle in der "Meine Kanäle" Ansicht.
        /// </summary>
        public OrderOption ChannelOderSetting
        {
            get { return channelOrderSetting; }
            set { channelOrderSetting = value; }
        }

        private OrderOption conversationOrderSetting;
        /// <summary>
        /// Die Einstellung für die Anordnung der Konversationen in der "Konversation" Ansicht 
        /// innerhalb einer Gruppe.
        /// </summary>
        public OrderOption ConversationOrderSetting
        {
            get { return conversationOrderSetting; }
            set { conversationOrderSetting = value; }
        }

        private OrderOption groupOrderSetting;
        /// <summary>
        /// Die Einstellung für die Anordnung der Gruppen in der "Meine Gruppen" Ansicht.
        /// </summary>
        public OrderOption GroupOrderSetting
        {
            get { return groupOrderSetting; }
            set { groupOrderSetting = value; }
        }

        private OrderOption ballotOrderSetting;
        /// <summary>
        /// Die Einstellung für die Anordnung der Abstimmungen in der "Abstimmungen" Ansicht 
        /// innerhalb einer Gruppe.
        /// </summary>
        public OrderOption BallotOrderSetting
        {
            get { return ballotOrderSetting; }
            set { ballotOrderSetting = value; }
        }

        private OrderOption announcementOrderSetting;
        /// <summary>
        /// Die Einstellung für die Anordnung der Announcement Nachrichten in einer Auflistung von 
        /// Nachrichten. Der Nutzer hat die Wahl zwischen neuster Nachricht oben, wobei die nachfolgenden
        /// Nachrichten dann unterhalb aufgelistet werden, oder neuster Nachricht oben, wobei dann die
        /// nachfolgenden Nachrichten oberhalb aufgelistet werden und der Nutzer nach oben scrollen kann.
        /// </summary>
        public OrderOption AnnouncementOrderSetting
        {
            get { return announcementOrderSetting; }
            set { announcementOrderSetting = value; }
        }

        private OrderOption generalListOrderSetting;
        /// <summary>
        /// Gibt allgemein für Listen in der Anwendung an, ob die Sortierung der Einträge nach dem
        /// gewählten Kriterium aufsteigend oder absteigend erfolgen soll.
        /// </summary>
        public OrderOption GeneralListOrderSetting
        {
            get { return generalListOrderSetting; }
            set { generalListOrderSetting = value; }
        }

        private Language languageSetting;
        /// <summary>
        /// Gibt die Sprache an, die der Nutzer aktuell gewählt hat.
        /// </summary>
        public Language LanguageSetting
        {
            get { return languageSetting; }
            set { languageSetting = value; }
        }

        private NotificationSetting msgNotificationSetting;
        /// <summary>
        /// Die Einstellung, wie eingehende Nachrichten angekündigt werden sollen.
        /// Diese Einstellung gilt global für die Anwendung. Sie kann jedoch von 
        /// spezifischeren Einstellungen pro Kanal und Gruppe überschrieben werden.
        /// </summary>
        public NotificationSetting MsgNotificationSetting
        {
            get { return msgNotificationSetting; }
            set { msgNotificationSetting = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AppSettings.
        /// </summary>
        public AppSettings()
        {

        }
    }
}
