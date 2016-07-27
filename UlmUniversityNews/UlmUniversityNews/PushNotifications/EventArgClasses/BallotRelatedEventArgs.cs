using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class BallotRelatedEventArgs : GroupRelatedEventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id der vom Event betroffenen Abstimmung.
        /// </summary>
        public int BallotId { get; set; }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse BallotRelatedEventArgs.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der vom Event betroffenen Abstimmung.</param>
        public BallotRelatedEventArgs(int groupId, int ballotId)
            : base(groupId)
        {
            this.BallotId = ballotId;
        }
    }
}
