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
using DataHandlingLayer.DataModel.Enums;

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
            catch(DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
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
            catch(DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
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
        /// Aktualisiert die Datensätze der Kanäle, die aktuell von der Anwendung verwaltet werden
        /// basierend auf der übergebenen Liste an Kanaldaten. Die Liste kann neue Kanäle enthalten,
        /// die dann in die lokalen Datensätze übernommen werden. Die Liste kann aber auch bestehende
        /// Datensätze mit geänderten Datenwerten beinhalten, dann werden die lokalen Datensätze aktualisiert.
        /// </summary>
        /// <param name="channels">Die Liste mit neuen oder geänderten Kanaldaten.</param>
        public void UpdateChannels(List<Channel> channels)
        {
            Channel currentChannel;
            Channel channelDB;

            // Iteriere über Liste:
            for (int i = 0; i < channels.Count; i++)
            {
                currentChannel = channels[i];

                try
                {
                    // Prüfe zunächst, ob lokaler Datensatz existiert für den Kanal.
                    channelDB = channelDatabaseManager.GetChannel(currentChannel.Id);
                    if (channelDB != null)
                    {
                        // Führe Aktualisierung durch.
                        channelDatabaseManager.UpdateChannelWithSubclass(currentChannel);
                    }
                    else
                    {
                        // Führe Einfügeoperation durch.
                        channelDatabaseManager.StoreChannel(currentChannel);
                    }
                }
                catch(DatabaseException ex)
                {
                    Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                    // Abbilden auf ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
                }
            }
        }

        /// <summary>
        /// Speichere das übergebene Datum als das letzte Aktualisierungsdatum der lokalen Kanalressourcen.
        /// </summary>
        /// <param name="lastUpdate">Das Datum der letzten Aktualisierung als DateTime Objekt.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn ein lokaler Datenbankfehler die Ausführung verhindert hat.</exception>
        public void SetDateOfLastChannelListUpdate(DateTime lastUpdate)
        {
            try
            {
                channelDatabaseManager.SetDateOfLastChannelListUpdate(lastUpdate);
            }
            catch(DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        /// <summary>
        /// Prüft, ob der lokale Nutzer den Kanal mit der angegebenen Id abonniert hat.
        /// </summary>
        /// <param name="channeId">Die Id des zu prüfenden Kanals.</param>
        /// <returns>Liefert true, wenn der lokale Nutzer den Kanal abonniert hat, ansonsten false.</returns>
        public bool IsChannelSubscribed(int channeId)
        {
            try
            {
                return channelDatabaseManager.IsChannelSubscribed(channeId);
            }
            catch(DatabaseException ex)
            {
                // Keine Abbildung auf ClientException.
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Erzeugt eine Liste von Objekten vom Typ Kanal aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Kanal-Objekten.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException wenn keine Liste von Kanal-Objekten aus dem JSON String extrahiert werden kann.</exception>
        private List<Channel> parseChannelListFromJson(string jsonString)
        {
            List<Channel> channels = new List<Channel>();
            try
            {
                //channels = JsonConvert.DeserializeObject <List<Channel>>(jsonString);
                
                // Parse JSON List in eine JArray Repräsentation. JArray repräsentiert ein JSON Array. 
                Newtonsoft.Json.Linq.JArray jsonArray = Newtonsoft.Json.Linq.JArray.Parse(jsonString);
                foreach (var item in jsonArray)
                {
                    if(item.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                    {
                        // Frage den Wert des Attributs "type" ab.
                        string typeValue = item.Value<string>("type");

                        ChannelType type;
                        if (Enum.TryParse(typeValue.ToString(), false, out type))
                        {
                            // Führe weiteres Parsen abhängig von dem Typ des Kanals durch.
                            switch(type)
                            {
                                case ChannelType.LECTURE:
                                    Lecture lecture = JsonConvert.DeserializeObject<Lecture>(item.ToString());
                                    channels.Add(lecture);
                                    break;
                                case ChannelType.EVENT:
                                    Event eventObj = JsonConvert.DeserializeObject<Event>(item.ToString());
                                    channels.Add(eventObj);
                                    break;
                                case ChannelType.SPORTS:
                                    Sports sportsObj = JsonConvert.DeserializeObject<Sports>(item.ToString());
                                    channels.Add(sportsObj);
                                    break;
                                default:
                                    // Für Student-Group und Other gibt es keine eigene Klasse.
                                    Channel channel = JsonConvert.DeserializeObject<Channel>(item.ToString());
                                    channels.Add(channel);
                                    break;
                            }
                        }
                    }
                }
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
        // Testzweck
        public async void TestJsonParsing()
        {
            List<Channel> channels = await RetrieveUpdatedChannelsFromServerAsync();

            foreach(var entry in channels){
                Debug.WriteLine(channels.ToString());
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
