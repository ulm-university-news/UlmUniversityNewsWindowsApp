using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class ChannelDeletedEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id des gelöschten Kanals.
        /// </summary>
        public int ChannelId { get; set; }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelDeletedEventArgs.
        /// </summary>
        public ChannelDeletedEventArgs()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelDeletedEventArgs.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        public ChannelDeletedEventArgs(int channelId)
        {
            this.ChannelId = channelId;
        }
    }
}
