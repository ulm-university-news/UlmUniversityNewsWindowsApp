using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.PushNotifications.EventArgClasses
{
    public class ChannelChangedEventArgs : EventArgs
    {
        #region Properties
        /// <summary>
        /// Die Id des geänderten Kanals.
        /// </summary>
        public int ChannelId { get; set; }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelChangedEventArgs.
        /// </summary>
        public ChannelChangedEventArgs()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelChangedEventArgs.
        /// </summary>
        /// <param name="channelId">Die Id des geänderten Kanals.</param>
        public ChannelChangedEventArgs(int channelId)
        {
            this.ChannelId = channelId;
        }
    }
}
