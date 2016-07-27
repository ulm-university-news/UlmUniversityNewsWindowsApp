using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class ConversationRelatedEventArgs : GroupRelatedEventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id der vom Event betroffenen Konversation.
        /// </summary>
        public int ConversationId { get; set; }
        #endregion

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationRelatedEventArgs.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId"></param>
        public ConversationRelatedEventArgs(int groupId, int conversationId)
            : base (groupId)
        {
            this.ConversationId = conversationId;
        }
    }
}
