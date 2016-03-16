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
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.Controller
{
    public class ChannelController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf den DatabaseManager, der Funktionalität bezüglich der Kanal-Ressourcen und den zugehörigen Subressourcen beinhaltet.
        /// </summary>
        private ChannelDatabaseManager channelDatabaseManager;

        /// <summary>
        /// Eine Referenz auf den DatabaseManager, der Funktionalität bezüglich der Moderator-Ressourcen beinhaltet.
        /// </summary>
        private ModeratorDatabaseManager moderatorDatabaseManager;

        /// <summary>
        /// Eine Referenz auf eine Instanz der ChannelAPI Klasse.
        /// </summary>
        private ChannelAPI channelApi;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der ChannelController Klasse.
        /// </summary>
        public ChannelController()
            : base()
        {
            channelDatabaseManager = new ChannelDatabaseManager();
            moderatorDatabaseManager = new ModeratorDatabaseManager();
            channelApi = new ChannelAPI();
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
            channelApi = new ChannelAPI();
        }

        #region LocalChannelFunctions
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
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        /// <summary>
        /// Liefert eine Liste aller aktuell in der lokalen Datenbank gespeicherten Kanäle zurück.
        /// </summary>
        /// <returns>Eine Liste von Objekten der Klasse Kanal oder einer ihrer Subklassen.</returns>
        /// <exception cref="ClientException">Wirft eine Client Exception, wenn ein Fehler bei der Ausführung aufgetreten ist.</exception>
        public List<Channel> GetAllChannels()
        {
            try
            {
                return channelDatabaseManager.GetChannels();
            }
            catch (DatabaseException ex)
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
        /// <exception cref="ClientException">Wirft ClientException, wenn das Abrufen fehlschlägt.</exception>
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
        /// Aktualisiert die Datensätze der Kanäle, die aktuell von der Anwendung verwaltet werden
        /// basierend auf der übergebenen Liste an Kanaldaten. Die Liste kann neue Kanäle enthalten,
        /// die dann in die lokalen Datensätze übernommen werden. Die Liste kann aber auch bestehende
        /// Datensätze mit geänderten Datenwerten beinhalten, dann werden die lokalen Datensätze durch die 
        /// neueren ersetzt.
        /// </summary>
        /// <param name="updatedChannelList">Die Liste mit neuen oder geänderten Kanaldaten.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Update fehlschlägt.</exception>
        public void AddOrReplaceLocalChannels(List<Channel> updatedChannelList)
        {
            Channel currentChannel;

            // Iteriere über Liste:
            for (int i = 0; i < updatedChannelList.Count; i++)
            {
                currentChannel = updatedChannelList[i];

                try
                {
                    // Prüfe zunächst, ob lokaler Datensatz existiert für den Kanal.
                    bool isContained = channelDatabaseManager.IsChannelContained(currentChannel.Id);
                    if (isContained)
                    {
                        // Führe Aktualisierung durch.
                        ReplaceLocalChannelWhileKeepingNotificationSettings(currentChannel);
                    }
                    else
                    {
                        // Führe Einfügeoperation durch.
                        channelDatabaseManager.StoreChannel(currentChannel);
                    }
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                    // Abbilden auf ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
                }
            }
        }

        /// <summary>
        /// Aktualisiert für den Kanal mit der übergebenen Id die Benachrichtigungseinstellungen.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der aktualisiert werden soll.</param>
        /// <param name="newNotificationSetting">Die neue Benachrichtigungseinstellung für diesen Kanal.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn die Aktualisierung fehlschlägt.</exception>
        public void UpdateNotificationSettingsForChannel(int channelId, NotificationSetting newNotificationSetting)
        {
            try
            {
                Channel channel = channelDatabaseManager.GetChannel(channelId);

                if (channel == null)
                {
                    Debug.WriteLine("No valid channel object could be retrieved from the given id.");
                    return;
                }

                // Führe Aktualisierung aus.
                channel.AnnouncementNotificationSetting = newNotificationSetting;
                channelDatabaseManager.UpdateChannel(channel);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        /// <summary>
        /// Speichere das übergebene Datum als das letzte Aktualisierungsdatum der lokalen Kanalressourcen.
        /// </summary>
        /// <param name="lastUpdate">Das Datum der letzten Aktualisierung als DateTimeOffset Objekt.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn ein lokaler Datenbankfehler die Ausführung verhindert hat.</exception>
        public void SetDateOfLastChannelListUpdate(DateTimeOffset lastUpdate)
        {
            try
            {
                channelDatabaseManager.SetDateOfLastChannelListUpdate(lastUpdate);
            }
            catch (DatabaseException ex)
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
            catch (DatabaseException ex)
            {
                // Keine Abbildung auf ClientException.
                Debug.WriteLine("DatabaseException with message {0} occurred.", ex.Message);
            }
            return false;
        }

        /// <summary>
        /// Ersetzt die lokal verwaltete Version des Kanals durch eine neue
        /// Version desselben Kanals.
        /// </summary>
        /// <param name="newChannel">Die neue Version des Kanals.</param>
        ///<exception cref="ClientException">Wirft ClientException, wenn Aktion aufgrund eines 
        ///     Fehlers nicht ausgeführt werden kann.</exception>
        public void ReplaceLocalChannel(Channel newChannel)
        {
            try
            {
                channelDatabaseManager.UpdateChannelWithSubclass(newChannel);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Couldn't perform the replace local channel functionality.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Ersetzt einen lokalen Kanal unter Beibehaltung der vom Nutzer eingestellten Anwendungseinstellungen.
        /// </summary>
        /// <param name="newChannel">Die neue Kanalressource, welche die alte Ressource lokal ersetzt.</param>
        public void ReplaceLocalChannelWhileKeepingNotificationSettings(Channel newChannel)
        {
            try
            {
                Channel oldChannel = channelDatabaseManager.GetChannel(newChannel.Id);

                // Übernehme NotificationSettings von lokalem Kanal.
                newChannel.AnnouncementNotificationSetting = oldChannel.AnnouncementNotificationSetting;
                // Ersetze lokalen Datensatz durch neuen Datensatz.
                ReplaceLocalChannel(newChannel);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("ReplaceLocalChannelWhileKeepingNotificationSettings: Error occurred!");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Fügt einen Kanal den lokal verwalteten Kanaldatensätzen hinzu.
        /// </summary>
        /// <param name="newChannel">Der hinzuzufügende Kanal.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Hinzufügen fehlschlägt.</exception>
        public void AddToLocalChannels(Channel newChannel)
        {
            try
            {
                channelDatabaseManager.StoreChannel(newChannel);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Couldn't store channel.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Liefert die Moderatoren, die lokal als Verantwortlicher für den Kanal mit
        /// der angegebenen Id eingetragen sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die verantwortlichen Moderatoren 
        ///     abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Objekten der Klasse Moderator.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Moderatoren nicht 
        ///     extrahiert werden konnten.</exception>
        public List<Moderator> GetModeratorsOfChannel(int channelId)
        {
            List<Moderator> moderators = null;
            try
            {
                moderators = channelDatabaseManager.GetResponsibleModeratorsForChannel(channelId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetModeratorsOfChannel: Couldn't extract moderators of channel.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return moderators;
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
            catch (DatabaseException ex)
            {
                // Fehler wird nicht weitergereicht, da es sich hierbei um eine Aktion handelt,
                // die normalerweise im Hintergrund ausgeführt wird und nicht aktiv durch den 
                // Nutzer ausgelöst wird.
                Debug.WriteLine("Could not store responsible moderators for the channel with id {0}." +
                    "The message is {1}.", channelId, ex.Message);
            }
        }

        /// <summary>
        /// Setzt den Moderator mit der angegebnen Id als inaktiv bezüglich des
        /// Kanals mit der angegebenen Id. Der Moderator ist dann kein aktiver Verantwortlicher
        /// des Kanals mehr.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den der Moderator als Verantwortlicher
        ///     ausgetragen werden soll.</param>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Operation fehlschlägt.</exception>
        public void RemoveModeratorFromChannel(int channelId, int moderatorId)
        {
            try
            {
                // Ändere Status zu inaktiv für den Moderator.
                channelDatabaseManager.AddModeratorToChannel(channelId, moderatorId, false);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("RemoveModeratorFromChannel: Failed to remove moderator.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Führt eine Synchronisierung der lokalen Datensätze bezüglich der übergebenen Liste an 
        /// verantwortlichen Moderatoren für einen Kanal aus. Fügt Moderatoren als Verantwortliche dem
        /// Kanal hinzu, wenn diese in der Refernzliste als solche geführt werden jedoch noch nicht in den 
        /// lokalen Datensätzen. Trägt andererseits Moderatoren als Verantwortliche des Kanals aus, wenn
        /// diese nicht mehr in der Referenzliste aufgeführt werden.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Moderatoren synchronisiert werden.</param>
        /// <param name="referenceList">Die Liste mit den aktuellen Moderatorenressourcen, die für
        ///     den Kanal als Verantwortliche eingetragen sind.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlschlägt.</exception>
        public void SynchronizeResponsibleModerators(int channelId, List<Moderator> referenceList)
        {
            List<Moderator> moderatorsToAdd = new List<Moderator>();
            try
            {
                // Frage die Moderatoren ab, die lokal aktuelle als Verantwortliche für den Kanal eingetragen sind.
                List<Moderator> localResponsibleModerators = GetModeratorsOfChannel(channelId);

                // Prüfe, ob in der Referenzliste ein Moderator ist, der noch nicht lokal als Verantwortlicher eingetragen ist.
                foreach (Moderator referenceModerator in referenceList)
                {
                    bool isContained = false;

                    foreach (Moderator localModerator in localResponsibleModerators)
                    {
                        if (referenceModerator.Id == localModerator.Id)
                        {
                            isContained = true;

                            // TODO - Aktualisierung der Moderator-Ressource ?
                        }
                    }

                    if (!isContained && referenceModerator.IsActive)
                    {
                        Debug.WriteLine("SynchronizeResponsibleModerators: About to add moderator with id {0}" +
                            " to channel with id {1}.", referenceModerator.Id, channelId);
                        moderatorsToAdd.Add(referenceModerator);
                    }
                }

                // Füge die fehlenden Moderatoren dem Kanal hinzu.
                StoreResponsibleModeratorsForChannel(channelId, moderatorsToAdd);

                // Prüfe, ob ein Moderator lokal als Verantworlicher eingetragen ist, der in der Referenzliste
                // nicht mehr als Verantwortlicher steht.
                foreach (Moderator localModerator in localResponsibleModerators)
                {
                    bool isContained = false;

                    foreach (Moderator referenceModerator in referenceList)
                    {
                        if (referenceModerator.Id == localModerator.Id)
                        {
                            isContained = true;
                            break;
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("SynchronizeResponsibleModerators: Need to set moderator with id {0} to " + 
                            "inactive for channel with id {1}.", localModerator.Id, channelId);
                        // Entferne Moderator als Verwantwortlichen des Kanals.
                        RemoveModeratorFromChannel(channelId, localModerator.Id);
                    }
                }
                
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("SynchronizeResponsibleModerators: Synchronization failed. Database failure.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Markiert den lokal gespeicherten Kanal mit der angegebenen Id als gelöscht.
        /// Ein als gelöscht markierter Kanal hat keinen Repräsentanten mehr auf dem Server.
        /// Die lokale Instanz bleibt jedoch zunächst noch erhalten.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        /// <exception cref="ClientException">Wirft DatabaseException, wenn markieren des Kanals
        ///     als gelöscht fehlschlägt.</exception>
        public void MarkChannelAsDeleted(int channelId)
        {
            try
            {
                channelDatabaseManager.MarkChannelAsDeleted(channelId);

                // Setze die Moderatoren des Kanals auf inaktiv.
                List<Moderator> moderators = GetModeratorsOfChannel(channelId);
                foreach (Moderator moderator in moderators)
                {
                    RemoveModeratorFromChannel(channelId, moderator.Id);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("MarkChannelAsDeleted: Error during execution.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Gibt an, ob für den Kanal mit der angegebenen Id eine Benachrichtigung
        /// bezüglich der Löschung des Kanals angezeigt werden soll.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        /// <returns>Liefert true, wenn Benachrichtigung erforderlich, sonst false.</returns>
        public bool IsNotificationAboutDeletionRequired(int channelId)
        {
            bool notificationRequired = true;
            try
            {
                bool deletionNoticed = channelDatabaseManager.IsChannelDeletionNoticedFlagSet(channelId);

                if (deletionNoticed)
                {
                    notificationRequired = false;
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("IsChannelDeletionNoticedFlagSet: Database failure. Msg is: {0}.",
                    ex.Message);
                // Gebe Fehler nicht an Aufrufer weiter in diesem Fall. Keine kritische Aktion.
            }
            return notificationRequired;
        }

        /// <summary>
        /// Deaktiviere für den Kanal mit der angegebenen Id die Benachrichtigung über die Löschung
        /// des Kanals. Der Nutzer wird im Fall einer Löschung des Kanals dann nicht weiter über diese
        /// Löschung informiert.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        public void DisableNotificationAboutDeletion(int channelId)
        {
            try
            {
                channelDatabaseManager.SetChannelDeletionNoticedFlag(channelId, true);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DisableNotificationAboutDeletion: Database failure. Msg is: {0}.",
                    ex.Message);
                // Gebe Fehler nicht an Aufrufer weiter in diesem Fall. Keine kritische Aktion.
            }
        }

        /// <summary>
        /// Löscht den Kanal mit der angegebenen Id aus den lokal verwalteten
        /// Datensätzen der Anwendung.
        /// </summary>
        /// <param name="channelId">Die Id des zu löschenden Kanals.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn löschen des Kanals fehlschlägt.</exception>
        public void DeleteLocalChannel(int channelId)
        {
            try
            {
                // Lösche alle Announcements des Kanals.
                RemoveAllAnnouncementsFromChannel(channelId);

                // Restliche zu diesem Kanal gehörenden Ressourcen sollten per ON DELETE CASCADE gelöscht werden.
                channelDatabaseManager.DeleteChannel(channelId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteLocalChannel: Couldn't delete local channel.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalChannelFunctions

        #region RemoteChannelFunctions
        /// <summary>
        /// Gibt eine Liste von Kanal-Objekten zurück, die seit der letzten Aktualisierung
        /// der im System verwalteten Kanäle geändert wurden.
        /// </summary>
        /// <returns>Liste von Kanal-Objekten.</returns>
        public async Task<List<Channel>> RetrieveUpdatedChannelsFromServerAsync()
        {
            List<Channel> channels = null;

            // Hole als erstes das Datum der letzten Aktualisierung.
            DateTimeOffset lastUpdate = channelDatabaseManager.GetDateOfLastChannelListUpdate();
            
            // Setze Request an den Server ab.
            string serverResponse;
            try
            {
                serverResponse = await channelApi.SendGetChannelsRequestAsync(
                    getLocalUser().ServerAccessToken,
                    lastUpdate);
            }
            catch(APIException ex)
            {
                Debug.WriteLine("API request has failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "API request to Server has failed.");
            }

            // Versuche Response zu parsen.
            channels = jsonParser.ParseChannelListFromJson(serverResponse);

            return channels;
        }
        
        /// <summary>
        /// Fragt Informationen zum Kanal mit der gegebenen Id vom Server ab. Gibt ein Objekt
        /// vom Typ Channel zurück, welches die abgefragten Informationen enthält.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        /// <returns>Ein Objekt vom Typ Channel, oder null, falls keine Informationen
        ///     extrahiert werden konnten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Server den
        ///     Request abgelehnt hat oder der Request aus einem anderen Grund nicht erfolgreich war.</exception>
        public async Task<Channel> GetChannelInfoAsync(int channelId)
        {
            User localUser = getLocalUser();
            Channel channel = null;

            string serverResponse = null;
            try
            {
                serverResponse = await channelApi.SendGetChannelRequestAsync(
                    localUser.ServerAccessToken,
                    channelId,
                    false);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("The get request to retrieve channel info has failed.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                channel = jsonParser.ParseChannelFromJson(serverResponse);
            }

            return channel;
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
                await channelApi.SendSubscribeChannelRequestAsync(
                    localUser.ServerAccessToken,
                    channelId);
            }
            catch(APIException ex)
            {
                // Request fehlgeschlagen. Nehme Kanal wieder aus der Menge an abonnierten Kanälen raus.
                channelDatabaseManager.UnsubscribeChannel(channelId);

                // Wenn der Kanal auf Serverseite gar nicht mehr existiert.
                if(ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("User tried to subscribe to a channel that doesn't exist anymore. Mark the channel as deleted.");
                    MarkChannelAsDeleted(channelId);
                }

                Debug.WriteLine("Couldn't subscribe channel. Server returned status code {0} and error code {1}.", ex.ResponseStatusCode, ex.ErrorCode);
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "Error occurred during API call.");
            } // Ende Fehlerbehandlung fehlgeschlagener Subscribe Request.

            try 
            {
                // Frage die verantwortlichen Moderatoren für diesen Kanal ab und speichere sie in der Datenbank.
                List<Moderator> responsibleModerators = await GetResponsibleModeratorsAsync(channelId);
                if (responsibleModerators != null)
                {
                    StoreResponsibleModeratorsForChannel(channelId, responsibleModerators);
                }

                // Frage die Nachrichten zum Kanal ab und speichere Sie in der Datenbank.
                List<Announcement> announcements = await GetAnnouncementsOfChannelAsync(channelId, 0, false);
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
                await channelApi.SendUnsubscribeChannelRequestAsync(
                    localUser.ServerAccessToken,
                    channelId
                    );
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
                    // Lösche die Announcements des Kanals.
                    channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channelId);

                    // Channel Objekt selbst bleibt in Datenbank. Setze jedoch Notification-Wert zurück.
                    resetNotificationSettingForChannel(channelId);

                    return;
                }

                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, "Error occurred during API call.");
            }

            // Nehme den Kanal aus der Menge der abonnierten Kanäle raus.
            channelDatabaseManager.UnsubscribeChannel(channelId);
            // Lösche die Announcements des Kanals.
            channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channelId);

            // Channel Objekt selbst bleibt in Datenbank. Setze jedoch Notification-Wert zurück.
            resetNotificationSettingForChannel(channelId);
        }

        /// <summary>
        /// Beim Deabonnieren eines Kanals bleibt der Datensatz des Kanals in der lokalen Datenbank vorhanden.
        /// Man kann mittels dieser Methode jedoch die kanalspezifischen Einstellungen für diesen Kanal zurücksetzen,
        /// so dass beim nächsten Abonnieren des Kanals nicht dieselben Einstellungen wieder übernommen werden.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Benachrichtigungseinstellungen wieder auf den 
        ///     Default Wert gesetzt werden sollen.</param>
        private void resetNotificationSettingForChannel(int channelId)
        {
            try
            {
                // Lade das betroffene Kanalobjekt.
                Channel channel = channelDatabaseManager.GetChannel(channelId);
                channel.AnnouncementNotificationSetting = NotificationSetting.APPLICATION_DEFAULT;

                // Aktualisiere Datensatz.
                channelDatabaseManager.UpdateChannel(channel);
                Debug.WriteLine("Reseted the notification settings for channel with id {0}.", channelId);
            }
            catch (DatabaseException ex)
            {
                // Führe keine weitere Aktion durch.
                Debug.WriteLine("Failed to reset the notification settings for the unsubscribed channel.");
                Debug.WriteLine("Message is: {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Legt einen neuen Kanal an. Die Daten des Kanals werden in Form eines
        /// Channel Objekts übergeben. Es wird ein Request an den Server übermittelt, um
        /// auf dem Server eine neue Kanalressource anzulegen.
        /// </summary>
        /// <param name="newChannel">Die Daten des neu anzulegenden Kanals in Form eines Objekts der Channel Klasse.</param>
        /// <returns>Liefert true, wenn der Kanal erfolgreich angelegt wurde, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn ein Fehler während des Erstellungsvorgangs auftritt.</exception>
        public async Task<bool> CreateChannelAsync(Channel newChannel)
        {
            if (newChannel == null)
                return false;

            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
                return false;

            // Führe Validierung der Kanaldaten durch. Abbruch bei aufgetretenen Validierungsfehlern.
            clearValidationErrors();
            bool validationSuccessful = validateChannelProperties(newChannel);

            // Breche ab, wenn Validierungsfehler aufgetreten sind.
            if (!validationSuccessful)
                return false;

            // Generiere JSON-Dokument aus Objekt.
            string jsonContent = jsonParser.ParseChannelToJsonString(newChannel);
            if (jsonContent == null)
            {
                Debug.WriteLine("Error during serialization from channel object to json string. Could " + 
                    "not create a channel. Execution is aborted.");
                return false;
            }

            string serverResponse = null;
            try
            {
                // Setzte Request zum Anlegen eines Kanals ab.
                serverResponse = await channelApi.SendCreateChannelRequestAsync(
                    activeModerator.ServerAccessToken,
                    jsonContent);
            }
            catch (APIException ex)
            {
                // Bilde ab auf ClientException.
                throw new ClientException(ex.ErrorCode, "Server rejected create channel request.");
            }

            // Extrahiere erhaltene Channel Resource aus JSON-Dokument.
            Channel createdChannel = jsonParser.ParseChannelFromJson(serverResponse);
            if (createdChannel != null)
            {
                try
                {
                    // Speichere Kanal lokal ab.
                    channelDatabaseManager.StoreChannel(createdChannel);

                    // Füge den Moderator als verwantwortlichen Moderator hinzu.
                    channelDatabaseManager.AddModeratorToChannel(
                        createdChannel.Id,
                        activeModerator.Id,
                        true);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Database Exception, message is: {0}.", ex.Message);
                    // Bilde ab auf ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Storing process of " +
                        "created channel object in local DB has failed");
                }
            }
            else
            {
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of server response has failed.");
            }

            return true;
        }

        /// <summary>
        /// Führt Aktualisierung des Kanals aus. Es wird ermittelt welche Properties eine Aktualisierung 
        /// erhalten haben. Die Aktualisierungen werden an den Server übermittelt, der die Aktualisierung auf
        /// dem Serverdatensatz ausführt und die Abonnenten über die Änderung informiert.
        /// </summary>
        /// <param name="oldChannel">Der Datensatz des Kanals vor der Aktualisierung.</param>
        /// <param name="newChannel">Der Datensatz mit neu eingegebenen Daten.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich war, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Fehler während des Aktualisierungsvorgangs auftritt.</exception>
        public async Task<bool> UpdateChannelAsync(Channel oldChannel, Channel newChannel)
        {
            if (oldChannel == null || newChannel == null)
                return false;

            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
                return false;

            // Validiere zunächst die neu eingegebenen Daten, bei Validierungsfehlern kann hier gleich abgebrochen werden.
            clearValidationErrors();
            bool validationSuccessful = validateChannelProperties(newChannel);

            if (!validationSuccessful)
                return false;

            // Erstelle ein Objekt für die Aktualisierung, welches die Daten enthält, die aktualisiert werden müssen.
            Channel updatableChannelObj = prepareUpdatableChannelInstance(oldChannel, newChannel);

            if (updatableChannelObj == null)
                return true;    // Keine Aktualisierung nötig.

            // Erstelle JSON-Dokument für die Aktualisierung.
            string jsonContent = jsonParser.ParseChannelToJsonString(updatableChannelObj);
            if (jsonContent == null)
            {
                Debug.WriteLine("Channel object could not be translated to a json document.");
                return false;
            }

            // Server Request.
            string serverResponse = null;
            try
            {
                // Führe Request zur Aktualisierung des Inhalts aus.
                serverResponse = await channelApi.SendUpdateChannelRequestAsync(
                    activeModerator.ServerAccessToken,
                    oldChannel.Id,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("Channel not found on server. Channel probably deleted.");
                    // TODO - Behandlung Not Found. Kanal wahrscheinlich gelöscht.
                }

                // Bilde ab auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Führe lokale Aktualisierung des Datensatzes aus.
            try
            {
                Channel updatedChannel = jsonParser.ParseChannelFromJson(serverResponse);
                if (updatedChannel == null)
                {
                    throw new ClientException(ErrorCodes.JsonParserError, "Couldn't parse server response.");
                }

                // Notification Settings bleiben unverändert.
                updatedChannel.AnnouncementNotificationSetting = oldChannel.AnnouncementNotificationSetting;
                if (updatedChannel != null)
                {
                    channelDatabaseManager.UpdateChannelWithSubclass(updatedChannel);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DatabaseException. Couldn't perform local channel update.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Hilfsmethode, welche die Validierung der Properties eines Kanalobjekts ausführt.
        /// Die Properties werden unter Berücksichtigung des Typs des Kanals und möglicher 
        /// Properties entsprechender Subklassen ausgeführt. Falls Validierungsfehler auftreten,
        /// werden die über die Report-Schnittstelle an das ViewModel gemeldet.
        /// </summary>
        /// <param name="channel">Das Kanalobjekt, das validiert werden soll.</param>
        /// <returns>Liefert true, wenn keine Validierungsfehler aufgetreten sind, liefert false bei Validierungsfehlern.</returns>
        private bool validateChannelProperties(Channel channel)
        {
            switch (channel.Type)
            {
                case ChannelType.LECTURE:
                    Lecture lecture = channel as Lecture;
                    if (lecture != null)
                    {
                        lecture.ClearValidationErrors();
                        lecture.ValidateAll();
                    }
                    if (lecture.HasValidationErrors())
                    {
                        reportValidationErrors(lecture.GetValidationErrors());
                        return false;
                    }
                    break;
                case ChannelType.EVENT:
                    Event eventObj = channel as Event;
                    if (eventObj != null)
                    {
                        eventObj.ClearValidationErrors();
                        eventObj.ValidateAll();
                    }
                    if (eventObj.HasValidationErrors())
                    {
                        reportValidationErrors(eventObj.GetValidationErrors());
                        return false;
                    }
                    break;
                case ChannelType.SPORTS:
                    Sports sportObj = channel as Sports;
                    if (sportObj != null)
                    {
                        sportObj.ClearValidationErrors();
                        sportObj.ValidateAll();
                    }
                    if (sportObj.HasValidationErrors())
                    {
                        reportValidationErrors(sportObj.GetValidationErrors());
                        return false;
                    }
                    break;
                default:
                    channel.ClearValidationErrors();
                    channel.ValidateAll();
                    if (channel.HasValidationErrors())
                    {
                        reportValidationErrors(channel.GetValidationErrors());
                        return false;
                    }
                    break;
            }
            return true;
        }

        /// <summary>
        /// Bereitet ein Objekt vom Typ Channel vor, welches alle Properties enthält, die sich geändert haben.
        /// Die Methode bekommt eine alte Version eines Channel Objekts und eine neue Version und ermittelt 
        /// dann die Properties, die eine Aktualisierung erhalten haben und schreibt diese in eine neue Channel
        /// Instanz. Die von der Methode zurückgelieferte Channel Instanz kann dann direkt für die Aktualisierung auf
        /// dem Server verwendet werden. Achtung: Hat sich überhaupt keine Property geändert, so gibt die Methode null zurück.
        /// </summary>
        /// <param name="oldChannel">Das Channel Objekt vor der Aktualisierung.</param>
        /// <param name="newChannel">Das Channel Objekt mit den aktuellen Werten.</param>
        /// <returns>Ein Objekt der Klasse Channel, bei dem die Properties, die sich geändert haben, mit den
        ///     aktualisierten Werten gefüllt sind.</returns>
        private Channel prepareUpdatableChannelInstance(Channel oldChannel, Channel newChannel)
        {
            bool hasChanged = false;
            Channel updatedChannel = new Channel();

            // Vergleiche zunächst Properties der allgemeinen Channel Klasse.
            if (oldChannel.Name != newChannel.Name)
            {
                hasChanged = true;
                updatedChannel.Name = newChannel.Name;
            }

            if (oldChannel.Description != newChannel.Description)
            {
                hasChanged = true;
                updatedChannel.Description = newChannel.Description;
            }

            if (oldChannel.Term != newChannel.Term)
            {
                hasChanged = true;
                updatedChannel.Term = newChannel.Term;
            }

            if (oldChannel.Locations != newChannel.Locations)
            {
                hasChanged = true;
                updatedChannel.Locations = newChannel.Locations;
            }

            if (oldChannel.Dates != newChannel.Dates)
            {
                hasChanged = true;
                updatedChannel.Dates = newChannel.Dates;
            }

            if (oldChannel.Contacts != newChannel.Contacts)
            {
                hasChanged = true;
                updatedChannel.Contacts = newChannel.Contacts;
            }

            if (oldChannel.Website != newChannel.Website)
            {
                hasChanged = true;
                updatedChannel.Website = newChannel.Website;
            }

            // Vergleiche, ob kanalspezifische Felder sich geändert haben bei den Kanälen eines Typs mit solchen Feldern.
            if (oldChannel.Type == newChannel.Type)
            {
                switch (oldChannel.Type)
                {
                    case ChannelType.LECTURE:
                        Lecture updatedLecture = new Lecture()
                        {
                            Name = updatedChannel.Name,
                            Description = updatedChannel.Description,
                            Type = ChannelType.LECTURE,
                            Term = updatedChannel.Term,
                            Locations = updatedChannel.Locations,
                            Dates = updatedChannel.Dates,
                            Contacts = updatedChannel.Contacts,
                            Website = updatedChannel.Website
                        };

                        Lecture oldLecture = oldChannel as Lecture;
                        Lecture newLecture = newChannel as Lecture;

                        if (oldLecture.Lecturer != newLecture.Lecturer)
                        {
                            hasChanged = true;
                            updatedLecture.Lecturer = newLecture.Lecturer;
                        }

                        if (oldLecture.Assistant != newLecture.Assistant)
                        {
                            hasChanged = true;
                            updatedLecture.Assistant = newLecture.Assistant;
                        }

                        if (oldLecture.Faculty != newLecture.Faculty)
                        {
                            hasChanged = true;
                            updatedLecture.Faculty = newLecture.Faculty;
                        }

                        if (oldLecture.StartDate != newLecture.StartDate)
                        {
                            hasChanged = true;
                            updatedLecture.StartDate = newLecture.StartDate;
                        }

                        if (oldLecture.EndDate != newLecture.EndDate)
                        {
                            hasChanged = true;
                            updatedLecture.EndDate = newLecture.EndDate;
                        }

                        // Setze updatedChannel neu.
                        updatedChannel = updatedLecture;
                        break;
                    case ChannelType.EVENT:
                        Event updatedEvent = new Event()
                        {
                            Name = updatedChannel.Name,
                            Description = updatedChannel.Description,
                            Type = ChannelType.EVENT,
                            Term = updatedChannel.Term,
                            Locations = updatedChannel.Locations,
                            Dates = updatedChannel.Dates,
                            Contacts = updatedChannel.Contacts,
                            Website = updatedChannel.Website
                        };

                        Event oldEvent = oldChannel as Event;
                        Event newEvent = newChannel as Event;

                        if (oldEvent.Cost != newEvent.Cost)
                        {
                            hasChanged = true;
                            updatedEvent.Cost = newEvent.Cost;
                        }

                        if (oldEvent.Organizer != newEvent.Organizer)
                        {
                            hasChanged = true;
                            updatedEvent.Organizer = newEvent.Organizer;
                        }

                        // Setze updatedChannel neu.
                        updatedChannel = updatedEvent;
                        break;
                    case ChannelType.SPORTS:
                        Sports updatedSport = new Sports()
                        {
                            Name = updatedChannel.Name,
                            Description = updatedChannel.Description,
                            Type = ChannelType.SPORTS,
                            Term = updatedChannel.Term,
                            Locations = updatedChannel.Locations,
                            Dates = updatedChannel.Dates,
                            Contacts = updatedChannel.Contacts,
                            Website = updatedChannel.Website
                        };

                        Sports oldSport = oldChannel as Sports;
                        Sports newSport = newChannel as Sports;

                        if (oldSport.Cost != newSport.Cost)
                        {
                            hasChanged = true;
                            updatedSport.Cost = newSport.Cost;
                        }

                        if (oldSport.NumberOfParticipants != newSport.NumberOfParticipants)
                        {
                            hasChanged = true;
                            updatedSport.NumberOfParticipants = newSport.NumberOfParticipants;
                        }

                        // Setze updatedChannel neu.
                        updatedChannel = updatedSport;
                        break;
                    case ChannelType.OTHER:
                        updatedChannel.Type = ChannelType.OTHER;
                        break;
                    case ChannelType.STUDENT_GROUP:
                        updatedChannel.Type = ChannelType.STUDENT_GROUP;
                        break;
                }
            }

            // Prüfe, ob sich überhaupt eine Property geändert hat.
            if (!hasChanged)
            {
                Debug.WriteLine("No Property of channel has been updated. Method will return null.");
                updatedChannel = null;
            }

            return updatedChannel;
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
            
            string serverResponse = null;
            try
            {
                // Frage die verantwortlichen Moderatoren für den Kanal ab. 
                serverResponse = await channelApi.SendGetModeratorsOfChannelRequestAsync(
                    getLocalUser().ServerAccessToken,
                    channelId);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("Couldn't retrieve responsible moderators. " +
                "Error code is: {0} and status code was {1}.", ex.ErrorCode, ex.ResponseStatusCode);
                throw new ClientException(ex.ErrorCode, "API call failed.");
            }

            // Extrahiere Moderatoren-Objekte aus der Antwort.
            responsibleModerators = jsonParser.ParseModeratorListFromJson(serverResponse);

            return responsibleModerators;
        }

        /// <summary>
        /// Löscht den Kanal mit der angegebenen Id. Sendet einen Löschrequest an den Server,
        /// so dass der Kanal auf dem Server gelöscht wird.
        /// Lokal wird der Kanal zunächst nur als gelöscht markiert, der Datensatz bleibt aber erhalten.
        /// </summary>
        /// <param name="channelId">Die Id des zu löschenden Kanals.</param>
        public async Task DeleteChannelAsync(int channelId)
        {
            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
                return;

            try
            {
                // Sende Löschrequest an den Server.
                await channelApi.SendDeleteChannelRequest(
                    activeModerator.ServerAccessToken,
                    channelId);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("DeleteChannelAsync: Error during deletion process. No successful deletion on server.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Wenn Löschrequest an Server erfolgreich, dann markiere den Kanal lokal als gelöscht.
            MarkChannelAsDeleted(channelId);
        }
        #endregion RemoteChannelFunctions

        #region RemoteAnnouncementFunctions
        /// <summary>
        /// Frage die Announcements zu dem Kanal mit der angegebenen Id vom Server ab.
        /// Die Abfrage kann durch die Angabe der Nachrichtennummer beeinflusst werden. Es
        /// werden nur die Announcements mit einer höheren Nachrichtennummer als der angegebenen 
        /// abgefragt.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die Announcements abgefragt werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, ab der die Announcements abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob der Request bei mehrfachen gleichen Requests innerhalb eines Zeitraums erneut ausgeführt werden soll,
        ///     oder ob der Eintrag aus dem Cache verwendet werden soll.</param>
        /// <returns>Eine Liste von Announcement Objekten. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn der Abfruf der Nachrichten fehlgeschlagen ist.</exception>
        public async Task<List<Announcement>> GetAnnouncementsOfChannelAsync(int channelId, int messageNr, bool withCaching)
        {
            List<Announcement> announcements = new List<Announcement>();

            string serverResponse = null;
            try
            {
                // Frage alle Announcements zu dem gegebenen Kanal ab, beginnend bei der angegebenen Nachrichtennummer.
                serverResponse = await channelApi.SendGetAnnouncementsRequestAsync(
                    getLocalUser().ServerAccessToken,
                    channelId,
                    messageNr,
                    withCaching);
            }
            catch(APIException ex)
            {
                Debug.WriteLine("Couldn't retrieve announcements of channel. " +
                    "Error code is: {0} and status code was {1}.", ex.ErrorCode, ex.ResponseStatusCode);
                throw new ClientException(ex.ErrorCode, "API call failed.");
            }

            // Extrahiere Announcements aus JSON-Dokument.
            announcements = jsonParser.ParseAnnouncementListFromJson(serverResponse);
            if (announcements == null)
            {
                throw new ClientException(ErrorCodes.JsonParserError, "Error during parsing of server response.");
            }

            return announcements;
        }

        /// <summary>
        /// Speichere eine empfangen Announcement ab. Falls notwendig werden fehlende Informationene
        /// (z.B. über den Autor) vom Server geladen.
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
                        if (responsibleModerators != null)
                        {
                            StoreResponsibleModeratorsForChannel(announcement.ChannelId, responsibleModerators);
                        }
                        
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
        /// Speichere eine Menge von empfangenen Announcements in der Datenbank ab.Falls notwendig werden fehlende Informationene
        /// (z.B. über den Autor) vom Server geladen.
        /// </summary>
        /// <param name="announcements">Eine Liste von Announcement Objekten.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn die übergebene Menge an Announcements nicht 
        ///     in der lokalen Datenbank gespeichert werden konnte.</exception>
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
                            if (responsibleModerators == null)
                            {
                                Debug.WriteLine("Couldn't retrieve responsible moderators.");
                                continue;
                            }

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
                // Fehler wird an Aufrufer weitergereicht.
                Debug.WriteLine("Couldn't store the received announcements of channel. Message was {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Local database failure.");
            }
        }

        /// <summary>
        /// Erzeugt eine neue Nachricht für den Kanal mit der angegebenen Id. 
        /// Die Announcement wird auf dem Server angelegt und dieser verteilt sie an 
        /// alle Abonnenten.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Announcement angelegt werden soll.</param>
        /// <param name="newAnnouncement">Ein neues Announcement Objekt.</param>
        /// <returns>Liefert true, wenn die Announcement erfolgreich angelegt werden konnte, sonst false.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn das Anlegen auf dem Server fehlgeschlagen ist.</exception>
        public async Task<bool> CreateAnnouncementAsync(int channelId, Announcement newAnnouncement)
        {
            if (newAnnouncement == null)
                return false;

            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
            {
                Debug.WriteLine("Moderator not logged in.");
                return false;
            }


            // Führe Validierung der übergebenen Daten durch.
            clearValidationErrors();
            newAnnouncement.ClearValidationErrors();
            newAnnouncement.ValidateAll();
            if (newAnnouncement.HasValidationErrors())
            {
                // Melde Validierungsfehler und breche ab.
                reportValidationErrors(newAnnouncement.GetValidationErrors());
                return false;
            }

            string jsonContent = jsonParser.ParseAnnouncementToJsonString(newAnnouncement);
            if (jsonContent == null)
            {
                Debug.WriteLine("CreateAnnouncementAsync failed, the announcement could " +
                    " not be translated into a json document.");
                return false;
            }

            string serverResponse = null;
            try
            {
                serverResponse = await channelApi.SendCreateAnnouncementRequestAsync(
                    activeModerator.ServerAccessToken,
                    channelId,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("Server says channel not found. Channel is probably deleted.");
                    // TODO - Behandlung von ChannelNotFound.
                }

                Debug.WriteLine("CreateAnnouncement failed. The message is: {0}.", ex.Message);
                // Bilde ab auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Extrahiere Announcement aus ServerResponse.
            Announcement createdAnnouncement = jsonParser.ParseAnnouncementFromJsonString(serverResponse);

            if (createdAnnouncement != null)
            {
                // Speichere Announcement.
                await StoreReceivedAnnouncementAsync(createdAnnouncement);
            }

            return true;
        }
        #endregion RemoteAnnouncementFunctions

        #region LocalAnnouncementFunctions
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

        /// <summary>
        /// Rufe die höchste MessageNumber ab, die aktuell einer Announcement des Kanals zugeordnet ist. 
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, von dem die höchste MessageNr abgerufen werden soll.</param>
        /// <returns>Die höchste MessageNumber.</returns>
        public int GetHighestMsgNrForChannel(int channelId)
        {
            return channelDatabaseManager.GetHighestMessageNumberOfChannel(channelId);
        }

        /// <summary>
        /// Markiere diejenigen Announcements des Kanals, der durch die angegebene Id identifieziert wird, als gelesen,
        /// die bisher noch als ungelsen markiert sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Markierung der Announcements erfolgen soll.</param>
        public void MarkAnnouncementsAsRead(int channelId)
        {
            try
            {
                channelDatabaseManager.MarkAnnouncementsAsRead(channelId);
            }
            catch(DatabaseException ex)
            {
                // Gebe Exception nicht an den Aufrufer weiter.
                Debug.WriteLine("Mark announcements as read has failed. Message is {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Ruft alle Announcements ab, die für den Kanal mit der angegebenen Id im lokelen
        /// Speicher vorhanden sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den alle lokal vorhandenen Announcements abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Announcement Objekten.</returns>
        public List<Announcement> GetAllAnnouncementsOfChannel(int channelId)
        {
            List<Announcement> announcements = null;
            try
            {
                announcements = channelDatabaseManager.GetAllAnnouncementsOfChannel(channelId);
            }
            catch(DatabaseException ex)
            {
                // Gebe Exception nicht an den Aufrufer weiter.
                Debug.WriteLine("Retrieval of announcements has failed. Message is {0}.", ex.Message);
            }
            return announcements;
        }

        /// <summary>
        /// Gibt die Announcement für den Kanal mit der angegebenen Id zurück, die zuletzt empfangen wurde.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, von dem die Announcement abgefragt wird.</param>
        /// <returns>Liefert ein Announcement Objekt zurück, oder null, falls keine entsprechende Announcement existiert.</returns>
        public Announcement GetLastReceivedAnnouncement(int channelId)
        {
            Announcement lastReceivedAnnouncement = null;
            try
            {
                List<Announcement> announcements = channelDatabaseManager.GetLatestAnnouncements(channelId, 1, 0);
                if(announcements != null && announcements.Count == 1)
                {
                    lastReceivedAnnouncement = announcements[0];
                }
            }
            catch(DatabaseException ex)
            {
                // Gebe Exception nicht an den Aufrufer weiter.
                Debug.WriteLine("Retrieval of announcement has failed. Message is {0}.", ex.Message);
            }
            return lastReceivedAnnouncement;
        }

        /// <summary>
        /// Löscht alle zu dem Kanal mit der angegebenen Id gehörenden Announcement Nachrichten.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die zugehörigen Announcements gelöscht werden.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschen fehlschlägt.</exception>
        public void RemoveAllAnnouncementsFromChannel(int channelId)
        {
            try
            {
                channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channelId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("RemoveAllAnnouncementsFromChannel: Deletion failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalAnnouncementFunctions
                
        #region LocalManagedChannelsFunctions
        /// <summary>
        /// Fügt den übergebenen Kanal der Liste der verwalteten Kanäle hinzu für den übergebenen
        /// Moderator. Trägt dabei den übergebenen Moderator als Verantwortlichen für den übergebenen Kanal
        /// ein und schaut ob der lokale Datensatz des Kanals hinzugefügt oder aktualisiert werden muss. 
        /// </summary>
        /// <param name="moderator">Der Moderator, für den der Kanal zur Liste der verwalteten Kanäle hinzukommt.</param>
        /// <param name="newManagedChannel">Der hinzuzufügende Kanal.</param>
        public void AddChannelToLocalManagedChannels(Moderator moderator, Channel newManagedChannel)
        {
            if (moderator == null || newManagedChannel == null)
                return;

            try
            {
                // Prüfe, ob der aktuell eingeloggte Moderator schon in der Datenbank enthalten ist.
                // Falls nicht, speichere ihn ab.
                if (!moderatorDatabaseManager.IsModeratorStored(moderator.Id))
                {
                    Debug.WriteLine("SynchronizeLocalManagedChannels: Need to store the moderator with id {0} in local DB.",
                        moderator.Id);
                    moderatorDatabaseManager.StoreModerator(moderator);
                }

                // Prüfe ob Kanal schon in lokalen Datensätzen vorhanden ist.
                if (!channelDatabaseManager.IsChannelContained(newManagedChannel.Id))
                {
                    Debug.WriteLine("AddChannelToLocalManagedChannels: Adding channel with id {0} to local channels.",
                        newManagedChannel.Id);
                    // Wenn nicht, füge Kanal hinzu. 
                    AddToLocalChannels(newManagedChannel);
                }
                else
                {
                    // Frage lokale Version ab und prüfe, ob diese aktualisiert werden muss.
                    Channel localChannel = GetChannel(newManagedChannel.Id);

                    if (DateTimeOffset.Compare(localChannel.ModificationDate, newManagedChannel.ModificationDate) < 0)
                    {
                        Debug.WriteLine("SynchronizeLocalManagedChannels: Need to update channel with id {0}.",
                            localChannel.Id);
                        // Übernehme NotificationSettings von lokalem Kanal.
                        newManagedChannel.AnnouncementNotificationSetting = localChannel.AnnouncementNotificationSetting;
                        // Ersetze lokalen Datensatz durch neuen Datensatz.
                        ReplaceLocalChannel(newManagedChannel);
                    }
                }

                // Füge den Moderator als Verantwortlichen für den Kanal hinzu.
                channelDatabaseManager.AddModeratorToChannel(newManagedChannel.Id, moderator.Id, true);

                // Stoße das Herunterladen der für den neu hinzugekommenen Kanal relevanten Daten an.
                // Bemerkung: Falls das fehlschlägt wird kein Fehler geworfen. Die Daten können auch im Fehlerfall später nachgeladen werden.
                Task.Run(() => retrieveAndStoreManagedChannelInfoAsync(moderator, newManagedChannel.Id));
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Couldn't add channel to local managed channels. Msg is {0}.",
                    ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Hole die vom Moderator mit der angegebenen Id verwalteten Kanäle, die lokal in der Anwendung vorhanden sind.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <returns>Eine Liste von Channel Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf der verwalteten Kanäle fehlschlägt.</exception>
        public List<Channel> GetManagedChannels(int moderatorId)
        {
            List<Channel> managedChannels = null;
            Moderator activeModerator = GetLocalModerator();

            try
            {
                managedChannels = channelDatabaseManager.GetManagedChannels(activeModerator.Id);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Error occurred during retrieval of managed channels. Msg is: {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, "GetManagedChannels failed.");
            }

            return managedChannels;
        }

        /// <summary>
        /// Führe eine Synchronisation der lokalen Menge an Kanälen, für die der übergebene
        /// Moderator als Verantwortlicher eingetragen ist, mit der übergebenen Liste an Kanalressourcen
        /// durch. Die Methode prüft, ob die übergebene Menge an Kanalressourcen noch mit den aktuell lokal
        /// gehaltenen Kanalressourcen übereinstimmt und fügt der lokal verwalteten Liste fehlende Kanäle
        /// hinzu. Es können auch Kanäle aus der Liste rausgenommen werden, wenn diese nicht mehr vom übergebenen
        /// Moderator verwaltet werden. Bereits lokal vorhanden Kanäle werden falls notwendig mit den aktuelleren
        /// Daten der Referenzliste aktualisiert.
        /// </summary>
        /// <param name="activeModerator">Der Moderator, der für die zu synchronisierenden Kanäle als Verantwortlicher eingetragen ist.</param>
        /// <param name="referenceList">Die Liste an Kanalressourcen, gegen die die lokal verwaltete Liste synchronisiert wird.</param>
        public void SynchronizeLocalManagedChannels(Moderator activeModerator, List<Channel> referenceList)
        {
            if (activeModerator == null)
                return;

            try
            {
                // Prüfe, ob der aktuell eingeloggte Moderator schon in der Datenbank enthalten ist.
                // Falls nicht, speichere ihn ab.
                if (!moderatorDatabaseManager.IsModeratorStored(activeModerator.Id))
                {
                    Debug.WriteLine("SynchronizeLocalManagedChannels: Need to store the moderator with id {0} in local DB.",
                        activeModerator.Id);
                    moderatorDatabaseManager.StoreModerator(activeModerator);
                }
                
                // Frage verwaltete Kanäle aus der DB ab.
                List<Channel> managedChannelsFromDB = channelDatabaseManager.GetManagedChannels(activeModerator.Id);

                // Vergleiche beide Listen. Wenn Kanal in der Referenzliste ist, der noch nicht in den lokalen 
                // Datensätzen vorhanden ist, dann füge ihn hinzu. Wenn ein Kanal schon lokal vorhanden ist, dann prüfe,
                // ob eine Aktualisierung des lokalen Datensatzes notwendig ist.
                foreach (Channel referenceChannel in referenceList)
                {
                    bool isContained = false;

                    for (int i = 0; i < managedChannelsFromDB.Count; i++)
                    {
                        Channel localChannel = managedChannelsFromDB[i];

                        // Prüfe Ids.
                        if (referenceChannel.Id == localChannel.Id)
                        {
                            isContained = true;

                            // Prüfe, ob Aktualisierung erforderlich.
                            if (DateTimeOffset.Compare(localChannel.ModificationDate, referenceChannel.ModificationDate) < 0)
                            {
                                Debug.WriteLine("SynchronizeLocalManagedChannels: Need to update channel with id {0}.",
                                    localChannel.Id);
                                // Übernehme NotificationSettings von lokalem Kanal.
                                referenceChannel.AnnouncementNotificationSetting = localChannel.AnnouncementNotificationSetting;
                                // Ersetze lokalen Datensatz durch neuen Datensatz.
                                ReplaceLocalChannel(referenceChannel);
                            }

                            // Beende Schleife, wenn Treffer gefunden.
                            break;
                        }
                    }

                    if (!isContained)
                    {
                        // Füge Kanal hinzu.
                        Debug.WriteLine("SynchronizeLocalManagedChannels: Need to add channel with id {0} to managed channels.",
                                    referenceChannel.Id);
                        AddToLocalChannels(referenceChannel);

                        // Füge den Moderator als Verantwortlichen für den Kanal hinzu.
                        channelDatabaseManager.AddModeratorToChannel(referenceChannel.Id, activeModerator.Id, true);

                        // Stoße das Herunterladen der für den neu hinzugekommenen Kanal relevanten Daten an.
                        // Bemerkung: Falls das fehlschlägt wird kein Fehler geworfen. Die Daten können auch im Fehlerfall später nachgeladen werden.
                        Task.Run(() => retrieveAndStoreManagedChannelInfoAsync(activeModerator, referenceChannel.Id));
                    }
                }


                // Prüfe, ob ein Kanal nicht mehr vom Moderator verwaltet wird.
                // Prüfe hierzu, ob in der Liste der lokalen verwalteten Kanäle einer steht, 
                // der nicht mehr in der Referenz-Liste von Kanälen steht.
                for (int i = 0; i < managedChannelsFromDB.Count; i++)
                {
                    bool isContained = false;

                    foreach (Channel referenceChannel in referenceList)
                    {
                        if (referenceChannel.Id == managedChannelsFromDB[i].Id)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        RemoveChannelFromManagedChannels(activeModerator, managedChannelsFromDB[i]);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("SynchronizeLocalManagedChannels: Database exception occurred in " + 
                    "SynchronizeLocalManagedChannels. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, "Failed to update managed channels relationships.");
            }

        }

        /// <summary>
        /// Prüft, ob alle Beziehungen zwischen Kanalressourcen und dem aktuell eingeloggten
        /// Moderator aktuell sind und aktualisiert diese falls notwendig. 
        /// </summary>
        /// <param name="managedChannels">Liste von aktuell verwalteten Kanälen des Moderators.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlschlägt.</exception>
        //public void UpdateManagedChannelsRelationships(List<Channel> managedChannels)
        //{
        //    Moderator activeModerator = GetLocalModerator();
        //    if (activeModerator == null)
        //        return;

        //    try
        //    {
        //        // Prüfe, ob der aktuell eingeloggte Moderator schon in der Datenbank enthalten ist.
        //        if (!moderatorDatabaseManager.IsModeratorStored(activeModerator.Id))
        //        {
        //            Debug.WriteLine("Need to store the moderator with id {0} in local DB.", activeModerator.Id);
        //            moderatorDatabaseManager.StoreModerator(activeModerator);
        //        }

        //        // Prüfe, ob Moderator auch lokal als Verantwortlicher für Kanäle eingetragen ist.
        //        foreach (Channel channel in managedChannels)
        //        {
        //            if (!channelDatabaseManager.IsResponsibleForChannel(channel.Id, activeModerator.Id))
        //            {
        //                // Kanal gefunden, der vorher noch nicht von diesem Moderator verwaltet wurde.
        //                // Trage Moderator ein.
        //                Debug.WriteLine("Need to add the moderator with id {0} as a responsible moderator " +
        //                    "for the channel with id {1}.", activeModerator.Id, channel.Id);
        //                channelDatabaseManager.AddModeratorToChannel(channel.Id, activeModerator.Id, true);

        //                // Stoße das Herunterladen der für den neu hinzugekommenen Kanal relevanten Daten an.
        //                // Bemerkung: Falls das fehlschlägt wird kein Fehler geworfen. Die Daten können auch im Fehlerfall später nachgeladen werden.
        //                Task.Run(() => retrieveAndStoreManagedChannelInfoAsync(activeModerator, channel.Id));
        //            }
        //        }

        //        // Frage verwaltete Kanäle aus der DB ab.
        //        List<Channel> managedChannelsFromDB = channelDatabaseManager.GetManagedChannels(activeModerator.Id);
        //        // Prüfe, ob es darin noch einen Kanal gibt, der nicht mehr in der aktuellen Liste von Kanälen steht.
        //        for (int i = 0; i < managedChannelsFromDB.Count; i++)
        //        {
        //            bool isContained = false;

        //            foreach (Channel channel in managedChannels)
        //            {
        //                if (channel.Id == managedChannelsFromDB[i].Id)
        //                {
        //                    isContained = true;
        //                }
        //            }

        //            if (!isContained)
        //            {
        //                RemoveChannelFromManagedChannels(activeModerator, managedChannelsFromDB[i]);
        //            }
        //        }

        //    }
        //    catch (DatabaseException ex)
        //    {
        //        Debug.WriteLine("Database exception occurred in UpdateManagedChannelsRelationships. Msg is {0}.", ex.Message);
        //        throw new ClientException(ErrorCodes.LocalDatabaseException, "Failed to update managed channels relationships.");
        //    }
        //}

        /// <summary>
        /// Nimmt einen Kanal aus der Liste der verwalteten Kanäle raus und räumt
        /// mit diesem Kanal in Verbindung stehende Ressourcen weg, die dann nicht mehr
        /// benötigt werden. 
        /// </summary>
        /// <param name="activeModerator">Der gerade aktive Moderator, für den der Kanal aus der Liste der
        ///     verwalteten Kanäle ausgetragen wird.</param>
        /// <param name="channel">Der Kanal, der aus der Liste genommen wird.</param>
        public void RemoveChannelFromManagedChannels(Moderator activeModerator, Channel channel)
        {
            try
            {
                // Setzte Verantwortlichkeit auf inaktiv für diesen Kanal.
                Debug.WriteLine("Need to set moderator isActive to false for channel with id {0}.", channel.Id);
                channelDatabaseManager.AddModeratorToChannel(
                    channel.Id,
                    activeModerator.Id,
                    false);

                // Prüfe, ob der Kanal abonniert ist.
                bool isSubscribed = channelDatabaseManager.IsChannelSubscribed(channel.Id);
                if (!isSubscribed)
                {
                    // Kanal nicht noch vom lokalen Nutzer abonniert. Lösche Announcements und Moderator-Info.
                    Debug.WriteLine("Channel with id {0} not subscribed. Delete announcements and moderator info.", channel.Id);
                    channelDatabaseManager.DeleteAllAnnouncementsOfChannel(channel.Id);
                    channelDatabaseManager.RemoveAllModeratorsFromChannel(channel.Id);
                }

                // Lösche in jedem Fall die Reminder.
                Debug.WriteLine("Deleting reminders for channel with id {0}.", channel.Id);
                channelDatabaseManager.DeleteRemindersForChannel(channel.Id);
            }
            catch (DatabaseException ex)
            {
                // Keine weitere Aktion, da diese Funktionalität im Hintergrund abläuft und
                // nicht vom Nutzer aktiv ausgelöst wird.
                Debug.WriteLine("Error during removal of managed channel. No further action is taken." + 
                    "Error message is: {0}.", ex.Message);
            }
        }
        #endregion LocalManagedChannelsFunctions

        #region RemoteManagedChannelsFunctions
        /// <summary>
        /// Ruft die Liste an Kanalressourcen vom Server ab, für die der Moderator mit der angegebenen Id verantwortlich ist.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators, für den die Liste an Kanälen abgefragt werden soll.</param>
        /// <returns>Eine Liste von Channel Objekte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abfrage vom Server fehlschlägt.</exception>
        public async Task<List<Channel>> RetrieveManagedChannelsFromServerAsync(int moderatorId)
        {
            List<Channel> managedChannels = null;
            Moderator activeModerator = GetLocalModerator();

            string serverResponse = null;
            try
            {
                if (activeModerator != null)
                {
                    serverResponse = await channelApi.SendGetChannelsAssignedToModeratorRequestAsync(
                        activeModerator.ServerAccessToken,
                        activeModerator.Id);
                }
            }
            catch (APIException ex)
            {
                Debug.WriteLine("Error occurred. Retrieving the managed channels has failed. " +
                    "Message is: {0}.", ex.Message);
                throw new ClientException(ex.ErrorCode, "Retrieving of managed channels failed.");
            }

            managedChannels = jsonParser.ParseChannelListFromJson(serverResponse);

            if (managedChannels != null)
                Debug.WriteLine("Retrieved a list of managed channels with {0} items.", managedChannels.Count);

            return managedChannels;
        }

        /// <summary>
        /// Hilfsmethode zur Unterstützung der Synchronistation der verwalteten Kanäle.
        /// Ruft Daten zu dem Kanal mit der angegebenen Id vom Server ab. Dazu gehören
        /// die verantwortlichen Moderatoren und die Reminder. Speichert die Daten für den
        /// Kanal in der lokalen Datenbank ab. Ruft jedoch nicht die eigentlichen Kanaldaten ab.
        /// </summary>
        /// <param name="activeModerator">Der Moderator, der für den Kanal als Verantwortlicher eingetragen ist.
        ///     Ist der lokal eingeloggte Moderator.</param>
        /// <param name="channelId">Die Id des Kanals, für den die Daten abgerufen werden sollen.</param>
        private async Task retrieveAndStoreManagedChannelInfoAsync(Moderator activeModerator, int channelId)
        {
            Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Started method.");
            if (activeModerator == null)
                return;

            // Rufe alle Moderatoren zu dem Kanal ab.
            try
            {
                List<Moderator> moderators = await GetResponsibleModeratorsAsync(channelId);
                if (moderators == null)
                    return;
 
                foreach (Moderator moderator in moderators)
                {
                    if (!moderatorDatabaseManager.IsModeratorStored(moderator.Id))
                    {
                        Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Need to store the " + 
                            "moderator with id {0} in local DB.", moderator.Id);
                        moderatorDatabaseManager.StoreModerator(moderator);
                    }

                    if (!channelDatabaseManager.IsResponsibleForChannel(channelId, moderator.Id))
                    {
                        Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Add moderator with id {0} to " + 
                            "channel with id {1}.", channelId, moderator.Id);
                        channelDatabaseManager.AddModeratorToChannel(channelId, moderator.Id, moderator.IsActive);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Storing of the responsible moderators has failed.");
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Message is: {0}.", ex.Message);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Retrieval of the responsible moderators has failed.");
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: " +
                    "Message is: {0} and error code is {1}.", ex.Message, ex.ErrorCode);
            }
            
            // Rufe Reminder zu dem Kanal ab.
            try
            {
                List<Reminder> reminders = await GetRemindersOfChannelAsync(channelId, false);
                channelDatabaseManager.BulkInsertReminder(reminders);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Storing of the reminders has failed.");
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Message is: {0}.", ex.Message);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Retrieval of the reminders has failed.");
                Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: " + 
                    "Message is: {0} and error code is {1}.", ex.Message, ex.ErrorCode);
            }

            Debug.WriteLine("retrieveAndStoreManagedChannelInfoAsync: Finished method.");
        }

        #endregion RemoteManagedChannelsFunctions

        #region LocalReminderFunctions
        /// <summary>
        /// Holt den Reminder mit der angegebenen Id.
        /// </summary>
        /// <param name="reminderId">Die Id des Reminders.</param>
        /// <returns>Eine Instanz der Klasse Reminder, oder null, falls kein
        ///     Reminder mit dieser Id lokal verwaltet wird.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn der
        ///     Reminder nicht abgerufen werden konnte.</exception>
        public Reminder GetReminder(int reminderId)
        {
            Reminder reminder = null;
            try
            {
                reminder = channelDatabaseManager.GetReminder(reminderId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Error during GetReminder. Msg is: {0}.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return reminder;
        }

        /// <summary>
        /// Holt die lokal verwalteten Reminder für den Kanal mit der angegebenen Id.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Reminder geholt werden sollen.</param>
        /// <returns>Liste von Objekten vom Typ Reminder.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Fehler auftritt während des Lesens der Reminder.</exception>
        public List<Reminder> GetRemindersOfChannel(int channelId)
        {
            List<Reminder> reminders = null;
            try
            {
                reminders = channelDatabaseManager.GetRemindersForChannel(channelId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Error during GetRemindersOfChannel. Msg is: {0}.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return reminders;
        }

        /// <summary>
        /// Speichere den Reminder lokal im Speicher ab.
        /// </summary>
        /// <param name="reminder">Das Reminder Objekt mit den Reminder Daten.</param>
        /// <exception cref="ClientException">Wirft Exception, wenn Speichern fehlschlägt.</exception>
        public void StoreReminder(Reminder reminder)
        {
            try
            {
                if (!moderatorDatabaseManager.IsModeratorStored(reminder.AuthorId))
                {
                    Debug.WriteLine("Missing author. Can't store reminder directly. Start fallback method");
                    Task.Run(() => storeReminderIncludingRelevantInformationAsync(reminder));
                }
                else
                {
                    channelDatabaseManager.StoreReminder(reminder);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Failed to store reminder.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Speichert eine Liste von Reminder Objekten lokal im Speicher ab.
        /// </summary>
        /// <param name="reminders">Eine Liste von Reminder Objekten.</param>
        /// <exception cref="ClientException">Wirft Exception, wenn Speichern fehlschlägt.</exception>
        public void StoreReminders(List<Reminder> reminders)
        {
            try
            {
                channelDatabaseManager.BulkInsertReminder(reminders);
            }
            catch (DatabaseException ex) 
            {
                Debug.WriteLine("Failed to store reminders.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Ersetze die lokal verwaltete Reminderressource durch eine neuere Version
        /// derselben Ressource.
        /// </summary>
        /// <param name="newReminder">Die neuere Version der Reminderressource.</param>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn ein Fehler
        ///     während des Vorgangs auftritt.</exception>
        public void ReplaceLocalReminder(Reminder newReminder)
        {
            try
            {
                channelDatabaseManager.UpdateReminder(newReminder);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Failed to replace local reminder.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Entfernt den Reminder mit der angegebenen Id von den lokal gespeicherten Remindern.
        /// </summary>
        /// <param name="reminderId">Die Id des zu löschenden Reminders.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschen fehlschlägt.</exception>
        public void DeleteLocalReminder(int reminderId)
        {
            try
            {
                channelDatabaseManager.DeleteReminder(reminderId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteLocalReminder: Deletion failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Vergleicht die übergebene Referenzliste von Reminder Objekten gegen die aktuell von 
        /// der Anwendung gehaltene Liste von Reminder Objekten und fügt den lokalen Datensätzen
        /// Reminder Objekte hinzu, die in der Referenzliste stehen, lokal jedoch noch fehlen. Zudem wird
        /// geprüft, ob bereits vorhandene Reminder aktualisiert werden müssen. Ist der Reminder in der Referenzliste
        /// aktueller als der lokal gespeicherte, so wird der lokale Reminder aktualisiert.
        /// </summary>
        /// <param name="referenceList">Die Liste von Remindern, die als Referenz dient.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Reminder gehören.</param>
        public void AddOrUpdateLocalReminders(List<Reminder> referenceList, int channelId)
        {
            try
            {
                // Hole zunächst die lokale Liste von Remindern.
                List<Reminder> localReminderList = channelDatabaseManager.GetRemindersForChannel(channelId);

                // Prüfe, ob es einen Eintrag in der Liste der aktualisierten Reminder gibt, der noch nicht lokal
                // gespeichert ist und prüfe, ob ein bereits vorhandener Reminder aktualisiert werden muss.
                foreach (Reminder referenceReminder in referenceList)
                {
                    bool isContained = false;

                    for (int i = 0; i < localReminderList.Count; i++)
                    {
                        if (referenceReminder.Id == localReminderList[i].Id)
                        {
                            isContained = true;

                            // Prüfe, ob Aktualisierung erforderlich.
                            if (DateTimeOffset.Compare(localReminderList[i].ModificationDate, referenceReminder.ModificationDate) < 0)
                            {
                                Debug.WriteLine("Update of reminder with id {0} necessary.", referenceReminder.Id);
                                channelDatabaseManager.UpdateReminder(referenceReminder);
                            }
                            else if (localReminderList[i].Ignore != referenceReminder.Ignore)
                            {
                                Debug.WriteLine("Need to update the ignore flag of the reminder with id {0}.", referenceReminder.Id);
                                localReminderList[i].Ignore = referenceReminder.Ignore;
                                channelDatabaseManager.UpdateReminder(localReminderList[i]);
                            }
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("Need to add reminder with id {0}.", referenceReminder.Id);
                        StoreReminder(referenceReminder);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("AddOrUpdateLocalReminders: Database failure.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Synchronisiert die lokalen Datensätze der Reminder eines Kanals, die in der Anwendung verwaltet werden
        /// mit der übergebenen Liste an Reminderressourcen. Fügt Reminder, die in der Liste sind, aber nicht lokal gespeichert sind,
        /// den lokalen Remindern hinzu und entfernt lokale Reminder, wenn sie nicht mehr in der Liste stehen. Bei lokal bereits vorhandenen 
        /// Remindern wird geschaut, ob diese aktualisiert werden müssen.
        /// </summary>
        /// <param name="referenceList">Die aktualisierte Liste von Remindern, mittels deren die Synchronisation vorgenommen werden.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem die Reminder gehören.</param>
        public void SynchronizeLocalReminders(List<Reminder> referenceList, int channelId)
        {
            // Beginne zunächst mit dem Hinzufügen fehlender Reminder oder dem Aktualisieren bestehender Reminder.
            AddOrUpdateLocalReminders(referenceList, channelId);

            try
            {
                // Hole zunächst die lokale Liste von Remindern.
                List<Reminder> localReminderList = channelDatabaseManager.GetRemindersForChannel(channelId);

                // Prüfe, ob es einen lokalen Reminder gibt, der nicht mehr in der Liste der aktualisierten Reminder steht.
                for (int i = 0; i < localReminderList.Count; i++)
                {
                    bool isContained = false;

                    foreach (Reminder referenceReminder in referenceList)
                    {
                        if (referenceReminder.Id == localReminderList[i].Id)
                        {
                            isContained = true;
                            continue;
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("Need to delete reminder with id {0} from local reminders.", localReminderList[i].Id);
                        DeleteLocalReminder(localReminderList[i].Id);
                    }
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Error in SynchronizeLocalReminders. Message is: {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalReminderFunctions

        #region RemoteReminderFunctions
        /// <summary>
        /// Ruft die Liste von Remindern für den Kanal mit der angegebenen Id vom Server ab.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Reminder abgerufen werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob der Request immer explizit asugeführt werden soll, oder ob 
        ///     Ergebnisse aus dem Cache in Ordnung sind.</param>
        /// <returns>Liste von Reminder Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Reminder nicht abgerufen werden konnten.</exception>
        public async Task<List<Reminder>> GetRemindersOfChannelAsync(int channelId, bool withCaching)
        {
            List<Reminder> reminders = null;
            Moderator activeModerator = GetLocalModerator();

            if (activeModerator == null)
                return null;

            string serverResponse = null;
            try
            {
                serverResponse = await channelApi.SendGetRemindersRequestAsync(
                    activeModerator.ServerAccessToken,
                    channelId,
                    withCaching);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("Request to server not successful.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                reminders = jsonParser.ParseReminderListFromJson(serverResponse);
            }

            return reminders;
        }

        /// <summary>
        /// Erzeugt einen neuen Reminder. Führt die
        /// Validierung der Daten durch. Überträgt die Reminder-Daten an den Server und
        /// speichert die Daten lokal ab, wenn die Bestätigung vom Server kommt.
        /// </summary>
        /// <param name="newReminder">Das Objekt, welches die Daten des neu anzulegenden
        ///     Reminders beinhaltet.</param>
        /// <returns>Liefert true, wenn Reminder erfolgreich angelegt werden konnte, sonst false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn ein Fehler während des Erstellungsvorgangs auftritt.
        ///     Beispielsweise dann, wenn der Server den Request ablehnt.</exception>
        public async Task<bool> CreateReminderAsync(Reminder newReminder)
        {
            if (newReminder == null)
                return false;

            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
            {
                Debug.WriteLine("No active moderator. Need to abort CreateReminderAsync.");
                return false;
            }

            // Führe Validierung durch.
            clearValidationErrors();
            newReminder.ClearValidationErrors();
            newReminder.ValidateAll();
            if (newReminder.HasValidationErrors())
            {
                // Melde Validierungsfehler und breche ab.
                reportValidationErrors(newReminder.GetValidationErrors());
                return false;
            }

            // Erstelle JSON-Dokument.
            string jsonContent = jsonParser.ParseReminderToJson(newReminder);
            if (jsonContent == null)
            {
                Debug.WriteLine("Couldn't parse reminder to json. Need to abort execution.");
                return false;
            }

            string serverResponse = null;
            try
            {
                // Setzte Request an Server ab.
                serverResponse = await channelApi.SendCreateReminderRequestAsync(
                    activeModerator.ServerAccessToken,
                    newReminder.ChannelId,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("Channel not found on server. Channel probably deleted.");
                    // TODO - Behandlung Not Found. Kanal wahrscheinlich gelöscht.
                }

                Debug.WriteLine("Request to create reminder has failed.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parse Reminder aus Server-Antwort.
            Reminder createdReminder = jsonParser.ParseReminderFromJson(serverResponse);
            if (createdReminder != null)
            {
                // Speichere Reminder lokal ab.
                StoreReminder(createdReminder);
            }

            return true;
        }

        /// <summary>
        /// Aktualisiert einen Reminder. Ermittelt die zu aktualisierenden Properties und führt einen
        /// Aktualisierungsrequest an den Server aus, um die Ressource zu aktualisieren. Bei einer erfolgreichen
        /// Aktualisierung wird auch der lokal gehaltene Datensatz aktualisiert.
        /// </summary>
        /// <param name="oldReminder">Das Reminder Objekt mit den Daten vor der Aktualisierung.</param>
        /// <param name="newReminder">Das Reminder Objekt mit den aktualisiertern Daten.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich durchgeführt werden konnte.
        ///     Liefert false, wenn Aktualisierung fehlgeschlagen ist, z.B. aufgrund eines Validierungsfehlers.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn ein Fehler während der Aktualisierung auftritt, der
        ///     dem Nutzer gemeldet werden muss, z.B. wenn der Server den Request abgelehnt hat.</exception>
        public async Task<bool> UpdateReminderAsync(Reminder oldReminder, Reminder newReminder)
        {
            if (oldReminder == null || newReminder == null)
                return false;

            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
                return false;

            // Validiere zunächst die neu eingegebenen Daten, bei Validierungsfehlern kann hier gleich abgebrochen werden.
            clearValidationErrors();
            newReminder.ClearValidationErrors();
            newReminder.ValidateAll();
            if (newReminder.HasValidationErrors())
            {
                // Melde Validierungsfehler und breche ab.
                reportValidationErrors(newReminder.GetValidationErrors());
                return false;
            }

            // Erstelle Objekt für Aktualisierungsrequest an den Server.
            Reminder updatableReminder = createUpdatableReminderInstance(oldReminder, newReminder);
            if (updatableReminder == null)
            {
                Debug.WriteLine("No changes in reminder object detected. No update request required.");
                return true;
            }

            // Generiere Json-Dokument.
            string jsonContent = jsonParser.ParseReminderToJson(updatableReminder);
            if (jsonContent == null)
                return false;

            string serverResponse = null;
            try
            {
                // Sende Request an den Server.
                serverResponse = await channelApi.SendUpdateReminderRequestAsync(
                    activeModerator.ServerAccessToken,
                    oldReminder.ChannelId,
                    oldReminder.Id,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    Debug.WriteLine("Channel not found on server. Channel probably deleted.");
                    // TODO - Behandlung Not Found. Kanal wahrscheinlich gelöscht.
                }

                if (ex.ErrorCode == ErrorCodes.ReminderNotFound)
                {
                    Debug.WriteLine("Reminder not found on server. Reminder probably deleted.");
                    // TODO - Behandlung von Not Found. Reminder wahrscheinlich gelöscht.
                }

                Debug.WriteLine("Update request for reminder with id {0} has failed.", oldReminder.Id);
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parse Server Antwort.
            if (serverResponse != null)
            {
                Reminder updatedReminder = jsonParser.ParseReminderFromJson(serverResponse);
                if (updatableReminder != null)
                {
                    ReplaceLocalReminder(updatedReminder);
                }
            }

            return true;
        }

        /// <summary>
        /// Erstellt ein Objekt vom Typ Reminder, bei welchem genau die Felder gesetzt sind, die sich 
        /// bei den übergebenen Instanzen geändert haben. Das zurückgelieferte Objekt enthält dabei die aktualisierten
        /// Werte in den geänderten Feldern. Das zurückgegebene Objekt kann direkt für einen Aktualisierungsrequest an den 
        /// Server genutzt werden. Gab es überhaupt keine Änderung so liefert die Methode null zurück.
        /// </summary>
        /// <param name="oldReminder">Das Objekt mit den Daten vor der Aktualisierung.</param>
        /// <param name="newReminder">Das Objekt mit den aktualisierten Werten.</param>
        /// <returns>Ein Objekt vom Typ Reminder, in welchem genau die Felder gesetzt sind, die 
        ///     sich geändert haben. Liefert null, wenn es gar keine Änderung gab.</returns>
        private Reminder createUpdatableReminderInstance(Reminder oldReminder, Reminder newReminder)
        {
            bool hasChanged = false;
            Reminder updatableReminder = new Reminder();

            if (DateTimeOffset.Compare(oldReminder.StartDate, newReminder.StartDate) != 0)
            {
                hasChanged = true;
                updatableReminder.StartDate = newReminder.StartDate;
            }

            if (DateTimeOffset.Compare(oldReminder.EndDate, newReminder.EndDate) != 0)
            {
                hasChanged = true;
                updatableReminder.EndDate = newReminder.EndDate;
            }

            if (oldReminder.Interval != newReminder.Interval)
            {
                hasChanged = true;
                updatableReminder.Interval = newReminder.Interval;
            }

            if (oldReminder.Ignore != newReminder.Ignore)
            {
                hasChanged = true;
                updatableReminder.Ignore = newReminder.Ignore;
            }

            if (oldReminder.Text != newReminder.Text)
            {
                hasChanged = true;
                updatableReminder.Text = newReminder.Text;
            }

            if (oldReminder.Title != newReminder.Title)
            {
                hasChanged = true;
                updatableReminder.Title = newReminder.Title;
            }

            // Beachte: Message Priority muss gesetzt werden, da sonst der Default-Wert beim JSON Parsing
            // in das JSON Dokument geschrieben wird und dadurch ungewollt die Einstellung des Reminder ändern könnte.
            if (oldReminder.MessagePriority != newReminder.MessagePriority)
            {
                hasChanged = true;
                updatableReminder.MessagePriority = newReminder.MessagePriority;
            }
            else
            {
                updatableReminder.MessagePriority = oldReminder.MessagePriority;
            }

            // Prüfe, ob sich überhaupt eine Property geändert hat.
            if (!hasChanged)
            {
                Debug.WriteLine("No Property of reminder has been updated. Method will return null.");
                updatableReminder = null;
            }

            return updatableReminder;
        }

        /// <summary>
        /// Löscht einen Reminder. Es wird ein Request zum Löschen des Reminder-Datensatzes an den Server
        /// geschickt. Der Reminder wird bei einer erfolgreichen Löschung auf dem Server auch lokal gelöscht.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem der Reminder gehört.</param>
        /// <param name="reminderId">Die Id des Reminder, der gelöscht werden soll.</param>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn der Löschvorgang fehlgeschlagen ist.</exception>
        public async Task DeleteReminderAsync(int channelId, int reminderId)
        {
            Moderator activeModerator = GetLocalModerator();
            if (activeModerator == null)
                return;

            try
            {
                // Sende Request zum Löschen des Reminder.
                await channelApi.SendDeleteReminderRequestAsync(
                    activeModerator.ServerAccessToken, 
                    channelId,
                    reminderId);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("DeleteReminderAsync: Failed to delete reminder, server request has failed.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Wenn Request erfolgreich, dann lösche Reminder auch lokal.
            try
            {
                channelDatabaseManager.DeleteReminder(reminderId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteReminderAsync: Failed to delete reminder locally.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Hilfsmethode, die eine Speicherung eines Reminders realisiert, bei der 
        /// fehlende Informationen zunächst vom Server abgefragt werden. 
        /// Beachte: Mögliche Fehler werden von dieser Methode nicht zurückgeliefert.
        /// </summary>
        /// <param name="reminder">Der zu speichernde Reminder.</param>
        private async Task storeReminderIncludingRelevantInformationAsync(Reminder reminder)
        {
            Debug.WriteLine("Starting storeReminderIncludingRelevantInformationAsync.");
            try
            {
                List<Moderator> moderators = await GetResponsibleModeratorsAsync(reminder.ChannelId);
                if (moderators == null)
                    return;

                foreach (Moderator moderator in moderators)
                {
                    if (!moderatorDatabaseManager.IsModeratorStored(moderator.Id))
                    {
                        Debug.WriteLine("Need to store moderator with id {0} in local DB.", moderator.Id);
                        moderatorDatabaseManager.StoreModerator(moderator);
                    }

                    if (moderator.IsActive)
                    {
                        if (!channelDatabaseManager.IsResponsibleForChannel(reminder.ChannelId, moderator.Id))
                        {
                            Debug.WriteLine("Need to add moderator with id {0} as responsible moderator for channel with id {1}.",
                                moderator.Id, reminder.ChannelId);
                            channelDatabaseManager.AddModeratorToChannel(reminder.ChannelId, moderator.Id, true);
                        }
                    }
                }

                if (!moderatorDatabaseManager.IsModeratorStored(reminder.AuthorId))
                {
                    // Wenn noch immer der Moderator fehlt, dann bilde ab auf Dummy Moderator.
                    Debug.WriteLine("Need to map author to dummy moderator.");
                    reminder.AuthorId = 0;
                }

                // Speichere reminder.
                channelDatabaseManager.StoreReminder(reminder);
                Debug.WriteLine("Finished storeReminderIncludingRelevantInformationAsync.");
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Fallback method storeReminderIncludingRelevantInformationAsync failed due to database exception." + 
                    "Reminder with id {0} couldn't be stored. Msg is: {1}", reminder.Id, ex.Message);
            }
            catch (ClientException clientEx)
            {
                Debug.WriteLine("Fallback method storeReminderIncludingRelevantInformationAsync failed due to client exception." +
                    "Reminder with id {0} couldn't be stored. ErrorCode is: {1}", reminder.Id, clientEx.ErrorCode);
            }
        }
        #endregion RemoteReminderFunctions
    }
}
