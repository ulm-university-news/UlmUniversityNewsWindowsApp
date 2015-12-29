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
using DataHandlingLayer.API;
using Newtonsoft.Json;

namespace DataHandlingLayer.Controller
{
    public class ChannelController : MainController
    {
        /// <summary>
        /// Eine Referenz auf den DatabaseManager, der Funktionalität bezüglich der Kanal-Ressourcen und den zugehörigen Subressourcen beinhaltet.
        /// </summary>
        private ChannelDatabaseManager channelDatabaseManager;

        /// <summary>
        /// Eine Referenz auf eine Instanz der API Klasse mittels der Requests an den Server abgesetzt werden können.
        /// </summary>
        private API.API api;

        /// <summary>
        /// Erzeugt eine Instanz der ChannelController Klasse.
        /// </summary>
        public ChannelController()
            : base()
        {
            channelDatabaseManager = new ChannelDatabaseManager();
            api = new API.API();
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

        /// <summary>
        /// Gibt eine Liste von Kanal-Objekten zurück, die seit der letzten Aktualisierung
        /// der im System verwalteten Kanäle geändert wurden.
        /// </summary>
        /// <returns>Liste von Kanal-Objekten.</returns>
        public async Task<List<Channel>> RetrieveUpdatedChannelsFromServerAsync()
        {
            List<Channel> channels = null;
            Dictionary<string, string> parameters = null;

            // Hole als erstes das Datum der letzten Aktualisierung.
            DateTime lastUpdate = channelDatabaseManager.GetDateOfLastChannelListUpdate();
            if(lastUpdate != DateTime.MinValue)
            {
                // Erzeuge Parameter für lastUpdate;
                parameters = new Dictionary<string, string>();
                parameters.Add("lastUpdated", api.ParseDateTimeToUTCFormat(lastUpdate));
            }

            // Setze Request an den Server ab.
            string serverResponse;
            try
            {
                serverResponse = await api.SendHttpGetRequestAsync(getLocalUser().ServerAccessToken, "/channel", parameters);
            }
            catch(APIException ex)
            {
                Debug.WriteLine("API request has failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "API request to Server has failed.");
            }

            // Versuche Response zu parsen.
            channels = parseChannelListFromJson(serverResponse);

            return channels;
        }

        /// <summary>
        /// Erzeugt eine Liste von Objekten vom Typ Kanal aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Kanal-Objekten.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException wenn keine Liste von Kanal-Objekten aus dem JSON String extrahiert werden kann.</exception>
        private List<Channel> parseChannelListFromJson(string jsonString)
        {
            List<Channel> channels = null;
            try
            {
                channels = JsonConvert.DeserializeObject <List<Channel>>(jsonString);
            }
            catch(JsonException ex)
            {
                Debug.WriteLine("Error during deserialization. Exception is: " + ex.Message);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of JSON object has failed.");
            }

            return channels;
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
