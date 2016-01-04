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
        /// Eine Referenz auf den DatabaseManager, der Funktionalität bezüglich der Moderator-Ressourcen beinhaltet.
        /// </summary>
        private ModeratorDatabaseManager moderatorDatabaseManager;

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
            moderatorDatabaseManager = new ModeratorDatabaseManager();
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
            moderatorDatabaseManager = new ModeratorDatabaseManager();
            api = new API.API();
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
        /// Liefert den Kanal mit der angegebenen Id zurück, sofern dieser in den lokalen
        /// Datensätzen der Anwendung gespeichert ist.
        /// </summary>
        /// <param name="channelId">Die Id des abzurufenden Kanals.</param>
        /// <returns>Das Kanalobjekt bzw. ein Objekt vom Typ einer Unterklasse von Kanal.</returns>
        public Channel GetChannel(int channelId)
        {
            try
            {
                return channelDatabaseManager.GetChannel(channelId);
            }
            catch (DatabaseException ex)
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
        /// Der lokale Nutzer abonniert den Kanal mit der angegebenen Id. Es wird die Kommunikation
        /// mit dem Server realisiert und der Kanal in der lokalen Datenbank entsprechend als abonnierter
        /// Kanal eingetragen.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der abonniert werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Abonnementvorgang fehlschlägt.</exception>
        public async Task SubscribeChannelAsync(int channelId)
        {
            // Trage den Kanal in der lokalen Datenbank als abonnierten Kanal ein.
            try
            {
                channelDatabaseManager.SubscribeChannel(channelId);
            }
            catch(DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }

            User localUser = getLocalUser();
            try
            {
                // Führe Request an den Server durch, um den Kanal zu abonnieren.
                string serverResponse = 
                    await api.SendHttpPostRequestWithJsonBodyAsync(localUser.ServerAccessToken, string.Empty, "/channel/" + channelId + "/user", null);
            }
            catch(APIException ex)
            {
                // Request fehlgeschlagen. Nehme Kanal wieder aus der Menge an abonnierten Kanälen raus.
                channelDatabaseManager.UnsubscribeChannel(channelId);

                // Wenn der Kanal auf Serverseite gar nicht mehr existiert.
                if(ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("User tried to subscribe to a channel that doesn't exist anymore. Remove the channel from the local database.");
                    try
                    {
                        channelDatabaseManager.DeleteChannel(channelId);
                    }
                    catch(DatabaseException dEx)
                    {
                        Debug.WriteLine("Channel with id {0} couldn't be deleted. Message is: {1}.", channelId, dEx.Message);
                    }
                }

                Debug.WriteLine("Couldn't subscribe channel. Server returned status code {0} and error code {1}.", ex.ResponseStatusCode, ex.ErrorCode);
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "Error occurred during API call.");
            }

            try 
            {
                // Frage die verantwortlichen Moderatoren für diesen Kanal ab und speichere sie in der Datenbank.
                List<Moderator> responsibleModerators = await GetResponsibleModeratorsAsync(channelId);
                foreach(Moderator moderator in responsibleModerators)
                {
                    if(!moderatorDatabaseManager.IsModeratorStored(moderator.Id))
                    {
                        moderatorDatabaseManager.StoreModerator(moderator);

                        // TODO füge Moderator noch als Verantwortlichen zum Kanal hinzu in der Datenbank.

                    }
                }

                // TODO Frage die Nachrichten zum Kanal ab und speichere Sie in der Datenbank. 
            }
            catch (DatabaseException dbEx)
            {
                // Keine weitere Aktion. Moderatoren und Announcements werden im weiteren Verlauf erneut abgerufen.
                // Es ist hier also nicht weiter dramatisch, wenn die Speicherung nicht erfolgreich war.
                Debug.WriteLine("Exception occurred during storage of the responsible moderators or the announcements.");
                Debug.WriteLine("Message is {0}.", dbEx.Message);
            }
            catch (ClientException ex)
            {
                // Keine weitere Aktion.
                Debug.WriteLine("Exception occurred during request of the responsible moderators or the announcements.");
                Debug.WriteLine("Message is: {0}, and ErrorCode is {1}.", ex.Message, ex.ErrorCode);
            }

        }

        /// <summary>
        /// Der lokale Nutzer deabonniert den Kanal mit der angegebenen Id. Es wird die Kommunikation
        /// mit dem Server realisiert und der Kanal in der lokalen Datenbank aus der Menge der abonnierten
        /// Kanäle ausgetragen.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der deabonniert werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Deabonnementvorgang fehlschlägt.</exception>
        public async Task UnsubscribeChannelAsync(int channelId)
        {
            // Setze Request zum Deabonnieren des Kanals an den Server ab.
            try
            {
                User localUser = getLocalUser();
                await api.SendHttpDeleteRequestAsync(localUser.ServerAccessToken, "/channel/" + channelId + "/user");
            }
            catch(APIException ex)
            {
                Debug.WriteLine("Couldn't unsubscribe channel. Server returned status code {0} and error code {1}.", ex.ResponseStatusCode, ex.ErrorCode);

                // Wenn der Kanal auf dem Server gelöscht wurde, dann ist der Nutzer auch nicht mehr Abonnent.
                if(ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("Channel seems to be deleted from the server. Perform unsubscribe on local database.");

                    // Nehme den Kanal aus der Menge der abonnierten Kanäle raus.
                    channelDatabaseManager.UnsubscribeChannel(channelId);
                    return;
                }

                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "Error occurred during API call.");
            }

            // Nehme den Kanal aus der Menge der abonnierten Kanäle raus.
            channelDatabaseManager.UnsubscribeChannel(channelId);
        }

        /// <summary>
        /// Frage die Moderatoren-Ressourcen vom Server ab, die für den Kanal mit der angegebenen Id verantwortlich sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals zu dem die verantwortlichen Moderatoren abgefragt werden.</param>
        /// <returns>Eine Liste von Moderator Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn die Moderatoren-Ressourcen nicht erfolgreich abgefragt werden konnten.</exception>
        public async Task<List<Moderator>> GetResponsibleModeratorsAsync(int channelId)
        {
            List<Moderator> responsibleModerators = new List<Moderator>();
            try
            {
                // Frage die verantwortlichen Moderatoren für den Kanal ab. 
                string serverResponse =
                    await api.SendHttpGetRequestAsync(getLocalUser().ServerAccessToken, "/channel/" + channelId + "/moderator", null);

                // Extrahiere Moderatoren-Objekte aus der Antwort.
                // Parse JSON List in eine JArray Repräsentation. JArray repräsentiert ein JSON Array. 
                Newtonsoft.Json.Linq.JArray jsonArray = Newtonsoft.Json.Linq.JArray.Parse(serverResponse);
                foreach(var item in jsonArray)
                {
                    Moderator moderator = parseModeratorObjectFromJSON(item.ToString());
                    responsibleModerators.Add(moderator);
                }
            }
            catch (JsonException jsonEx)
            {
                Debug.WriteLine("Error during deserialization. Exception is: " + jsonEx.Message);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of JSON object has failed.");
            }
            catch (APIException ex)
            {
                Debug.WriteLine("Couldn't retrieve responsible moderators. " + 
                "Error code is: {0} and status code was {1}.", ex.ErrorCode, ex.ResponseStatusCode);
                throw new ClientException(ex.ErrorCode, "API call failed.");
            }
            return responsibleModerators;
        }

        /// <summary>
        /// Erzeugt eine Liste von Objekten vom Typ Kanal aus dem übergebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Kanal-Objekten.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException wenn keine
        ///     Liste von Kanal-Objekten aus dem JSON String extrahiert werden kann.</exception>
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
