using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    /// <summary>
    /// Die Klasse AnnouncementReceivedEventArgs repräsentiert die EventArgumente, die mit dem
    /// AnnouncementEvent verschickt werden.
    /// </summary>
    public class AnnouncementReceivedEventArgs : EventArgs
    {
        #region Properties
        private int channelId;
        /// <summary>
        /// Die Id des Kanals, für den die empfangene Announcement bestimmt ist.
        /// </summary>
        public int ChannelId
        {
            get { return channelId; }
            set { channelId = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der AnnouncementReceivedEventArgs Klasse.
        /// </summary>
        public AnnouncementReceivedEventArgs()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der AnnouncementReceivedEventArgs Klasse.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die empfangene Announcement bestimmt ist.</param>
        public AnnouncementReceivedEventArgs(int channelId)
        {
            this.channelId = channelId;
        }
    }
}
