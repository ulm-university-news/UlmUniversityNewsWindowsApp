using DataHandlingLayer.Controller.ValidationErrorReportInterface;
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
    public class ChannelController : MainController
    {
        /// <summary>
        /// Eine Referenz auf den DatabaseManager, der Funktionalität bezüglich der Kanal-Ressourcen und den zugehörigen Subressourcen beinhaltet.
        /// </summary>
        private ChannelDatabaseManager channelDatabaseManager;

        /// <summary>
        /// Erzeugt eine Instanz der ChannelController Klasse.
        /// </summary>
        public ChannelController()
            : base()
        {
            channelDatabaseManager = new ChannelDatabaseManager();
        }

        /// <summary>
        /// Erzeugt eine Instanz der ChannelController Klasse.
        /// </summary>
        /// <param name="validationErrorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public ChannelController(IValidationErrorReport validationErrorReporter)
            : base(validationErrorReporter)
        {
            channelDatabaseManager = new ChannelDatabaseManager();
        }

        /// <summary>
        /// Liefert eine Liste von Kanälen zurück, die vom lokalen Nutzer abonniert wurden.
        /// </summary>
        /// <returns>Eine Liste von Objekten der Klasse Kanal oder einer ihrer Subklassen.</returns>
        /// <exception cref="ClientException">Wirft eine Client Exception, wenn ein Fehler bei der Ausführung aufgetreten ist.</exception>
        public List<Channel> GetMyChannels()
        {
            try
            {
                return channelDatabaseManager.GetSubscribedChannels();
            }
            catch(DatabaseException e)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", e.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        /// <summary>
        /// Liefert eine Liste aller aktuell in der Datenbank gespeicherten Kanäle zurück.
        /// </summary>
        /// <returns>Eine Liste von Objekten der Klasse Kanal oder einer ihrer Subklassen.</returns>
        /// <exception cref="ClientException">Wirft eine Client Exception, wenn ein Fehler bei der Ausführung aufgetreten ist.</exception>
        public List<Channel> GetAllChannels()
        {
            try
            {
                return channelDatabaseManager.GetChannels();
            }
            catch(DatabaseException e)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", e.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        // TODO - Remove after testing
        // Rein für Testzwecke
        public void storeTestChannel(Channel channel)
        {
            // Speichere Kanal in Datenbank.
            channelDatabaseManager.StoreChannel(channel);

            // Trage Kanal als subscribed ein.
            channelDatabaseManager.SubscribeChannel(channel.Id);
        }
    }
}
