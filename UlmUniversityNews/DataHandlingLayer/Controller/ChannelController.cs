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
            } // Ende Fehlerbehandlung fehlgeschlagener Subscribe Request.

            try 
            {
                // Frage die verantwortlichen Moderatoren für diesen Kanal ab und speichere sie in der Datenbank.
                List<Moderator> responsibleModerators = await GetResponsibleModeratorsAsync(channelId);
                StoreResponsibleModeratorsForChannel(channelId, responsibleModerators);

                // Frage die Nachrichten zum Kanal ab und speichere Sie in der Datenbank.
                List<Announcement> announcements = await GetAnnouncementsOfChannelAsync(channelId, 0);
                await StoreReceivedAnnouncementsAsync(announcements);
            }
            catch (ClientException ex)
            {
                // Keine weitere Aktion. Moderatoren und Announcements können im weiteren Verlauf erneut abgerufen werden.
                // Es ist hier also nicht weiter dramatisch, wenn die Speicherung nicht erfolgreich war.
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
            try
            {
                // Setze Request zum Deabonnieren des Kanals an den Server ab.
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
                    // Lösche die Einträge für die verantwortlichen Moderatoren.
                    channelDatabaseManager.RemoveAllModeratorsFromChannel(channelId);
                    // Lösche die Announcements des Kanals.
                    channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channelId);
                    // TODO - Entferne Reminder
                    return;
                }

                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "Error occurred during API call.");
            }

            // Nehme den Kanal aus der Menge der abonnierten Kanäle raus.
            channelDatabaseManager.UnsubscribeChannel(channelId);
            // Lösche die Einträge für die verantwortlichen Moderatoren.
            channelDatabaseManager.RemoveAllModeratorsFromChannel(channelId);
            // Lösche die Announcements des Kanals.
            channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channelId);
            // TODO - Entferne Reminder.
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
        /// Frage die Announcements zu dem Kanal mit der angegebenen Id vom Server ab.
        /// Die Abfrage kann durch die Angabe der Nachrichtennummer beeinflusst werden. Es
        /// werden nur die Announcements mit einer höheren Nachrichtennummer als der angegebenen 
        /// abgefragt.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die Announcements abgefragt werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, ab der die Announcements abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Announcement Objekten. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn der Abfruf der Nachrichten fehlgeschlagen ist.</exception>
        public async Task<List<Announcement>> GetAnnouncementsOfChannelAsync(int channelId, int messageNr)
        {
            List<Announcement> announcements = new List<Announcement>();
            try
            {
                // Frage alle Announcements zu dem gegebenen Kanal ab, beginnend bei der angegebenen Nachrichtennummer.
                Dictionary<string, string> parameters = new Dictionary<string, string>();
                parameters.Add("messageNr", messageNr.ToString());
                string serverResponse =
                    await api.SendHttpGetRequestAsync(getLocalUser().ServerAccessToken, "/channel/" + channelId + "/announcement", parameters);

                // Extrahiere Announcements aus JSON-Dokument.
                announcements = parseAnnouncementListFromJson(serverResponse);
            }
            catch(APIException ex)
            {
                Debug.WriteLine("Couldn't retrieve announcements of channel. " +
                    "Error code is: {0} and status code was {1}.", ex.ErrorCode, ex.ResponseStatusCode);
                throw new ClientException(ex.ErrorCode, "API call failed.");
            }
            return announcements;
        }

        /// <summary>
        /// Speichere eine empfangen Announcement ab.
        /// </summary>
        /// <param name="announcement">Das Announcement Objekt der empfangenen Announcement</param>
        public async Task StoreReceivedAnnouncementAsync(Announcement announcement)
        {
            if(announcement == null)
            {
                return;
            }

            Debug.WriteLine("Trying to store the announcement with id {0} and messageNr {1}.", 
                announcement.Id, announcement.MessageNumber);
            try
            {
                // Prüfe ob der Moderator in der Datenbank existiert, der als Autor der Nachricht eingetragen ist.
                if (moderatorDatabaseManager.IsModeratorStored(announcement.AuthorId))
                {
                    Debug.WriteLine("Starting to store announcement.");
                    // Speichere die Announcement ab.
                    channelDatabaseManager.StoreAnnouncement(announcement);
                }
                else
                {
                    // Fehlerbehandlung.
                    Debug.WriteLine("We do not have the author of the announcement in the database. Cannot store announcement without author reference.");
                    Debug.WriteLine("The missing author has the id {0}.", announcement.AuthorId);

                    Debug.WriteLine("Try querying the responsible moderators from the channel again.");
                    try
                    {
                        List<Moderator> responsibleModerators = await GetResponsibleModeratorsAsync(announcement.ChannelId);
                        StoreResponsibleModeratorsForChannel(announcement.ChannelId, responsibleModerators);

                        // Prüfe erneut, ob Eintrag nun vorhanden ist.
                        if (!moderatorDatabaseManager.IsModeratorStored(announcement.AuthorId))
                        {
                             // Bilde auf Dummy-Moderator ab und füge die Announcement ein.
                            Debug.WriteLine("Could not recover from missing author. Set author for announcement with id {0} to the " +
                                "dummy moderator.", announcement.Id);
                            announcement.AuthorId = 0;
                        }

                        // Speichere die Announcement ab.
                        channelDatabaseManager.StoreAnnouncement(announcement);
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("Request to retrieve missing moderators has failed. Error code is {0}.", ex.ErrorCode);
                        // Bilde auf Dummy-Moderator ab und füge die Announcement ein.
                        Debug.WriteLine("Could not recover from missing author. Set author for announcement with id {0} to the " +
                            "dummy moderator.", announcement.Id);
                        announcement.AuthorId = 0;

                        // Speichere die Announcement ab.
                        channelDatabaseManager.StoreAnnouncement(announcement);
                    }
                } // Ende Fehlerbehandlung.
            }
            catch (DatabaseException ex)
            {
                // Fehler wird nicht weitergereicht, da es sich hierbei um eine Aktion handelt,
                // die normalerweise im Hintergrund ausgeführt wird und nicht aktiv durch den 
                // Nutzer ausgelöst wird.
                Debug.WriteLine("Couldn't store the received announcement of channel. Message was {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Speichere eine Menge von empfangenen Announcements in der Datenbank ab.
        /// </summary>
        /// <param name="announcements">Eine Liste von Announcement Objekten.</param>
        public async Task StoreReceivedAnnouncementsAsync(List<Announcement> announcements)
        {
            if(announcements == null || announcements.Count == 0)
            {
                Debug.WriteLine("No announcements passed to the StoreReceivedAnnouncements method.");
                return;
            }

            // Liste der Announcements, die dann in die Datenbank geschrieben werden.
            List<Announcement> announcementsToStore = new List<Announcement>();
            bool performQueryOnMissingModerators = true;

            // Lookup-Verzeichnis, um schnell herauszufinden, ob ein Moderator lokale in der Datenbank gespeichert ist, oder nicht?
            Dictionary<int, bool> moderatorStoredMap = new Dictionary<int, bool>();

            // Iteriere über Announcements in Liste und speichere diese ab.
            foreach(Announcement announcement in announcements)
            {
                // Prüfung, ob Autor (Moderator) in lokaler DB verfügbar.
                bool isStored = false;
                if(moderatorStoredMap.ContainsKey(announcement.AuthorId))
                {
                    isStored = moderatorStoredMap[announcement.AuthorId];
                }
                else
                {
                    isStored = moderatorDatabaseManager.IsModeratorStored(announcement.AuthorId);
                    moderatorStoredMap.Add(announcement.AuthorId, isStored);
                }

                // Prüfe, ob eine gültige Referenz auf einen Autor existiert.
                if(isStored)
                {
                    announcementsToStore.Add(announcement);
                }
                else
                {
                    // Fehlerbehandlung.
                    Debug.WriteLine("No local entry for the moderator which is author of that announcement. The author id is {0}.", 
                        announcement.AuthorId);
                    if(performQueryOnMissingModerators)
                    {
                        Debug.WriteLine("Try querying the responsible moderators from the channel again.");
                        try
                        {
                            List<Moderator> responsibleModerators = await GetResponsibleModeratorsAsync(announcement.ChannelId);
                            StoreResponsibleModeratorsForChannel(announcement.ChannelId, responsibleModerators);

                            // Prüfe erneut, ob Eintrag nun vorhanden ist.
                            if(moderatorDatabaseManager.IsModeratorStored(announcement.AuthorId))
                            {
                                announcementsToStore.Add(announcement);
                            }
                            else
                            {
                                // Bilde auf Dummy-Moderator ab und füge die Announcement ein.
                                Debug.WriteLine("Could not recover from missing author. Set author for announcement with id {0} to the " + 
                                    "dummy moderator.", announcement.Id);
                                announcement.AuthorId = 0;
                                announcementsToStore.Add(announcement);
                            }
                        }
                        catch(ClientException ex)
                        {
                            Debug.WriteLine("Request to retrieve missing moderators has failed. Error code is {0}.", ex.ErrorCode);
                            // Bilde auf Dummy-Moderator ab und füge die Announcement ein.
                            Debug.WriteLine("Could not recover from missing author. Set author for announcement with id {0} to the " +
                                "dummy moderator.", announcement.Id);
                            announcement.AuthorId = 0;
                            announcementsToStore.Add(announcement);
                        }
                        // Führe im weiteren Verlauf keine weitere Abfrage an den Server mehr durch. Wenn der Fehler bei der ersten Abfrage nicht behoben wurde,
                        // dann wird er auch bei weiteren Abfragen nicht behoben.
                        performQueryOnMissingModerators = false;
                    }
                    else
                    {
                        // Abfragen der fehlenden Moderatoren wurde schon versucht. Bilde stattdessen direkt auf den Dummy-Moderator ab.
                        Debug.WriteLine("Could not recover from missing author. Set author for announcement with id {0} to the " +
                            "dummy moderator.", announcement.Id);
                        announcement.AuthorId = 0;
                        announcementsToStore.Add(announcement);
                    }
                } // Ende der Fehlerbehandlung von fehlendem Moderator-Eintrag.
            } // Ende der Foreach Schleife.

            try
            {
                Debug.WriteLine("Start bulk insert of announcements.");

                //Speichere die Announcements ab in der Datenbank.
                channelDatabaseManager.BulkInsertOfAnnouncements(announcementsToStore);
            }
            catch(DatabaseException ex)
            {
                // Fehler wird nicht weitergereicht, da es sich hierbei um eine Aktion handelt,
                // die normalerweise im Hintergrund ausgeführt wird und nicht aktiv durch den 
                // Nutzer ausgelöst wird.
                Debug.WriteLine("Couldn't store the received announcements of channel. Message was {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Ermittelt die Anzahl an ungelesenen Announcements für die vom lokalen Nutzer 
        /// abonnierten Kanäle. Gibt ein Verzeichnis zurück, indem mit der Kanal-Id
        /// als Schlüssel die Anzahl an ungelesenen Announcement-Nachrichten ermittelt werden kann.
        /// </summary>
        /// <returns>Verzeichnis, indem die Anzahl an ungelesenen Announcements für jeden Kanal 
        ///     gespeichert ist. Die Kanal-Id dient als Schlüssel.</returns>
        public Dictionary<int, int> GetAmountOfUnreadAnnouncementsForMyChannels()
        {
            Dictionary<int, int> channelIdOnUnreadMsgMap = null;
            try
            {
                channelIdOnUnreadMsgMap = channelDatabaseManager.DetermineAmountOfUnreadAnnouncementForMyChannels();
            }
            catch(DatabaseException ex)
            {
                // Gebe DatabaseException nicht an Aufrufer weiter.
                // Seite kann ohne diese Information angezeigt werden.
                Debug.WriteLine("Could not retrieve amount of unread announcements for my channels. Message is {0}.", ex.Message);
            }
            return channelIdOnUnreadMsgMap;
        }

        public List<Announcement> GetAllAnnouncementsOfChannel()
        {
            List<Announcement> announcements = null;
            // TODO
            return announcements;
        }

        /// <summary>
        /// Speichere für den Kanal mit der angegebenen Id die in der Liste definierten 
        /// Moderatoren als die für diesen Kanal verantwortlichen Moderatoren ab.
        /// Die Methode speichert die Datensätze der Moderatoren lokal in der Datenbank, falls sie dort
        /// noch nicht gespeichert sind und trägt die Moderatoren als Verantwortliche für den Kanal ein.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Moderatoren als Verantworliche eingetragen werden sollen. </param>
        /// <param name="responsibleModerators">Die Liste an Moderatoren, die als die Verantwortlichen eingetragen werden sollen.</param>
        public void StoreResponsibleModeratorsForChannel(int channelId, List<Moderator> responsibleModerators)
        {
            try
            {
                foreach (Moderator moderator in responsibleModerators)
                {
                    if (!moderatorDatabaseManager.IsModeratorStored(moderator.Id))
                    {
                        Debug.WriteLine("Need to store the moderator with id {0} locally.", moderator.Id);
                        moderatorDatabaseManager.StoreModerator(moderator);
                    }
                    // Füge Moderator noch als aktiven Verantwortlichen zum Kanal hinzu in der Datenbank.
                    channelDatabaseManager.AddModeratorToChannel(channelId, moderator.Id, moderator.IsActive);
                }
            }
            catch(DatabaseException ex)
            {
                // Fehler wird nicht weitergereicht, da es sich hierbei um eine Aktion handelt,
                // die normalerweise im Hintergrund ausgeführt wird und nicht aktiv durch den 
                // Nutzer ausgelöst wird.
                Debug.WriteLine("Could not store responsible moderators for the channel with id {0}." + 
                    "The message is {1}.", channelId, ex.Message);
            }
        }

        /// <summary>
        /// Extrahiere eine Liste von Announcement Objekten aus einem gegebenen JSON-Dokument.
        /// </summary>
        /// <param name="jsonString">Das JSON-Dokument.</param>
        /// <returns>Liste von Announcement-Objekten.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn keine Liste von
        ///     Announcements aus dem JSON-Dokument extrahiert werden konnte.</exception>
        private List<Announcement> parseAnnouncementListFromJson(string jsonString)
        {
            List<Announcement> announcements;
            try
            {
                announcements = JsonConvert.DeserializeObject<List<Announcement>>(jsonString);
            }
            catch(JsonException ex)
            {
                Debug.WriteLine("Error during deserialization. Exception is: " + ex.Message);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of JSON object has failed.");
            }
            return announcements;
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


    }
}
