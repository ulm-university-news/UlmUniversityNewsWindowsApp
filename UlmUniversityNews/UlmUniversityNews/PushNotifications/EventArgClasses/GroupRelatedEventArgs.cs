using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class GroupRelatedEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id der Gruppe, für die das Event gesendet wurde.
        /// </summary>
        public int GroupId { get; set; }
        #endregion

        /// <summary>
        /// Erzeugt eine neue Instanz der Klasse GroupRelatedEventArgs.
        /// </summary>
        /// <param name="groupId">Die Id der betroffenen Gruppe.</param>
        public GroupRelatedEventArgs(int groupId)
        {
            this.GroupId = groupId;
        }
    }
}
