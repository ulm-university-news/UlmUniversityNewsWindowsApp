using DataHandlingLayer.Common;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller
{
    public class IncrementalAnnouncementLoaderController : IIncrementalSource<Announcement>
    {
        /// <summary>
        /// Referenz auf eine Instanz der ChannelDatabaseManager Klasse.
        /// </summary>
        ChannelDatabaseManager channelDatabaseManager;

        /// <summary>
        /// Erzeugt eine Instanz der Klasse IncrementalAnnouncementLoaderController.
        /// </summary>
        public IncrementalAnnouncementLoaderController()
        {
            channelDatabaseManager = new ChannelDatabaseManager();
        }

        /// <summary>
        /// Implementierung der GetPagedItems Methode. Diese Methode ruft abhängig von
        /// den übergebenen Parametern Elemente des Typs Announcement innerhalb eines
        /// bestimmten Bereichs aus der Datenbank ab. Der Bereich wird durch den aktuellen
        /// Seitenindex und die Seitengröße festgelegt.
        /// </summary>
        /// <param name="resourceId">Die Id der Resource, von der die Elemente abgefragt werden sollen.</param>
        /// <param name="pageIndex">Der Index der aktuellen Seite.</param>
        /// <param name="pageSize">Die Anzahl der Elemente pro Seite.</param>
        /// <returns>Eine Auflistung von Elementen des Typs Announcement.</returns>
        public async Task<IEnumerable<Announcement>> GetPagedItems(int resourceId, int pageIndex, int pageSize)
        {
            return await Task.Run<IEnumerable<Announcement>>(() =>
            {
                Debug.WriteLine("GetPagedItems in IncrementalAnnouncementLoaderController is called. Parameters are: resId {0}, pageIndex {1}, pageSize {2}.",
                    resourceId, pageIndex, pageSize);
                List<Announcement> retrievedAnnouncements = channelDatabaseManager.GetAllAnnouncementsOfChannel(resourceId);
                return retrievedAnnouncements;
            });
        }
    }
}
