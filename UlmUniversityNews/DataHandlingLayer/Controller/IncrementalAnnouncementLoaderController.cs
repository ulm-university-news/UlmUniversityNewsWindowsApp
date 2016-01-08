using DataHandlingLayer.Common;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
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
                List<Announcement> retrievedAnnouncements = null;
                try
                {
                    // Rufe genau so viele Announcements ab, wie die PageSize angibt. 
                    // Der Offset gibt dabei an, ab welcher Stelle man die Announcements abrufen will.
                    retrievedAnnouncements = channelDatabaseManager.GetLatestAnnouncements(resourceId, pageSize, pageIndex * pageSize);
                }
                catch(DatabaseException ex)
                {
                    // Gebe Exception nicht an Aufrufer weiter, da diese Methode automatisch vom System aufgerufen wird.
                    Debug.WriteLine("Retrieving latest announcements in GetPagedItems has failed. Message is: {0}.", ex.Message);
                }
                
                return retrievedAnnouncements;
            });
        }
    }
}
