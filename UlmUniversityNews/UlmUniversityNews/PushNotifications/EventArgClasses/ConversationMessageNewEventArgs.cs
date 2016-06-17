using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class ConversationMessageNewEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id der Gruppe, zu der die Konversation gehört.
        /// </summary>
        public int GroupId { get; set; }

        /// <summary>
        /// Die Id der Konversation, für die eine neue Nachricht empfangen wurde.
        /// </summary>
        public int ConversationId { get; set; }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationMessageNewEventArgs.
        /// </summary>
        public ConversationMessageNewEventArgs()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationMessageNewEventArgs.
        /// </summary>
        /// <param name="groupId">Die Id der betroffenen Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, in die die neue Nachricht geschickt wurde.</param>
        public ConversationMessageNewEventArgs(int groupId, int conversationId)
        {
            this.GroupId = groupId;
            this.ConversationId = conversationId;
        }
    }
}
