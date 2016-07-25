using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using DataHandlingLayer.DataModel;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using Windows.UI.Notifications;

namespace DataHandlingLayer.Controller
{
    public class PushNotificationController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Instanz der ChannelController Klasse.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Eine Instanz der GroupController Klasse.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse PushNotificationController.
        /// </summary>
        public PushNotificationController()
            : base()
        {
            channelController = new ChannelController();
            groupController = new GroupController();
        }

        /// <summary>
        /// Behandelt eine vom WNS empfangene RawNotification. Die Push Nachricht wird geparset und abhängig
        /// von ihrem Typ behandelt.
        /// </summary>
        /// <param name="receivedNotificationMessage">Die empfangene Notification in Form einer PushMessage Instanz.</param>
        /// <returns>Liefert true, wenn die PushNachricht erfolgreich behandelt wurde, ansonsten false.</returns>
        public async Task<bool> HandlePushNotificationAsync(PushMessage receivedNotificationMessage)
        {
            bool handledSuccessfully = false;

            if(receivedNotificationMessage == null)
            {
                return handledSuccessfully;
            }

            Debug.WriteLine("HandlePushNotificationAsync: Received message of type: {0}.", receivedNotificationMessage.PushType);

            // Lese als erstes den Typ der empfangenen Push Nachricht aus. Behandle die PushNachricht nach Typ.
            PushType pushType = receivedNotificationMessage.PushType;
            switch (pushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    handledSuccessfully = await handleAnnouncementNewPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.ANNOUNCEMENT_DELETED:
                    // Aktuell nicht unterstützt.
                    break;
                case PushType.CHANNEL_CHANGED:
                    handledSuccessfully = await handleChannelChangedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.CHANNEL_DELETED:
                    handledSuccessfully = handleChannelDeletedPushMsg(receivedNotificationMessage);
                    break;
                case PushType.MODERATOR_ADDED:
                    handledSuccessfully = await handleModeratorAddedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.MODERATOR_CHANGED:
                    // Aktuell nicht unterstützt.
                    break;
                case PushType.MODERATOR_REMOVED:
                    handledSuccessfully = handleModeratorRemovedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.GROUP_DELETED:
                    handledSuccessfully = handleGroupDeletedPushMsg(receivedNotificationMessage);
                    break;
                case PushType.GROUP_CHANGED:
                    handledSuccessfully = await handleGroupChangedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.PARTICIPANT_NEW:
                    handledSuccessfully = await handleParticipantNewPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.PARTICIPANT_LEFT:
                    handledSuccessfully = handleParticipantLeftPushMsg(receivedNotificationMessage);
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    handledSuccessfully = handleParticipantLeftPushMsg(receivedNotificationMessage);
                    break;
                case PushType.PARTICIPANT_CHANGED:
                    // Aktuell nicht unterstützt.
                    break;
                case PushType.CONVERSATION_NEW:
                    handledSuccessfully = await handleConversationNewPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.CONVERSATION_CHANGED:
                    handledSuccessfully = await handleConversationChangedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.CONVERSATION_CHANGED_ALL:
                    handledSuccessfully = await handleConversationChangedAllPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.CONVERSATION_CLOSED:
                    // Aktuell nicht unterstützt. Wird über CONVERSATION_CHANGED abgedeckt.
                    break;
                case PushType.CONVERSATION_DELETED:
                    handledSuccessfully = handleConversationDeletedPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.CONVERSATION_MESSAGE_NEW:
                    handledSuccessfully = await handleConversationMessageNewPushMsgAsync(receivedNotificationMessage);
                    break;
                case PushType.BALLOT_NEW:
                    break;
                case PushType.BALLOT_CHANGED:
                    break;
                case PushType.BALLOT_CHANGED_ALL:
                    break;
                case PushType.BALLOT_OPTION_NEW:
                    break;
                case PushType.BALLOT_OPTION_ALL:
                    break;
                case PushType.BALLOT_OPTION_DELETED:
                    break;
                case PushType.BALLOT_OPTION_VOTE:
                    break;
                case PushType.BALLOT_OPTION_VOTE_ALL:
                    break;
                case PushType.BALLOT_CLOSED:
                    break;
                case PushType.BALLOT_DELETED:
                    break;
                case PushType.USER_CHANGED:
                    break;
                default:
                    break;
            }

            return handledSuccessfully;
        }

        /// <summary>
        /// Extrahiert den Inhalt der RawNotification und gibt die Notification
        /// in Form eines PushMessage Objekts an den Aufrufer zurück.
        /// </summary>
        /// <param name="receivedNotification">Die empfangene RawNotification als Objekt.</param>
        /// <returns>Eine Instanz der Klasse PushMessage.</returns>
        public PushMessage GetPushMessageFromNotification(RawNotification receivedNotification)
        {
            // Parse JSON Inhalt der Notification. 
            string notificationJsonContent = receivedNotification.Content;
            PushMessage pushMessage = jsonParser.ParsePushMessageFromJson(notificationJsonContent);
            
            return pushMessage;
        }

        /// <summary>
        /// Bestimmt für eine gegebene PushMessage anhand der lokalen Einstellungen des Nutzers 
        /// und anhand des Typs der Push Nachricht, ob eine Benachrichtigung des Benutzers erforderlich
        /// ist.
        /// </summary>
        /// <param name="pm">Die Push Nachricht in Form eines PushMessage Objekts.</param>
        /// <returns>Liefert true, wenn der Benutzer benachrichtigt werden soll, ansonsten false.</returns>
        public bool IsUserNotificationRequired(PushMessage pm)
        {
            if (pm == null)
                return false;

            AppSettings appSettings = GetApplicationSettings();
            bool notificationRequired = false;

            switch(pm.PushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    // Prüfe Anwendungseinstellungen ab. Soll der Nutzer über die eingetroffene Announcement informiert werden?
                    int channelId = pm.Id1;
                    Debug.WriteLine("It will be checked whether the user needs to be notified about the received announcement" + 
                        " for the channel with id {0}.", channelId);

                    notificationRequired = checkNotificationRequiredForNewAnnouncement(appSettings, channelId);
                    break;
                case PushType.CONVERSATION_MESSAGE_NEW:
                    // Prüfe Anwendungseinstellungen ab. Soll der Nutzer über die eingetroffene Konversationsnachricht informiert werden?
                    int groupId = pm.Id1;
                    int conversationId = pm.Id2;
                    Debug.WriteLine("It will be checked whether the user needs to be notified about the received conversation message" +
                        " for the group with id {0} and the conversation with id {1}.", groupId, conversationId);

                    notificationRequired = checkNotificationRequiredForConversationMessage(appSettings, groupId, conversationId);
                    break;
                case PushType.CHANNEL_DELETED:
                    // Informieren bei Kanal-Löschung.
                    notificationRequired = true;
                    break;
                case PushType.GROUP_DELETED:
                    // Informieren bei Gruppen-Löschung.
                    notificationRequired = true;
                    break;
                case PushType.PARTICIPANT_NEW:
                    // Informieren bei Teilnehmer hinzugefügt.
                    notificationRequired = true;
                    break;
                case PushType.PARTICIPANT_LEFT:
                    // Informiere bei Teilnehmer ausgetreten.
                    notificationRequired = true;
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    // Informiere bei Teilnehmer ausgetreten.
                    notificationRequired = true;
                    break;
                case PushType.CONVERSATION_NEW:
                    // Informiere bei neuer Konversation.
                    notificationRequired = true;
                    break;
            }

            return notificationRequired;
        }

        /// <summary>
        /// Gibt die Überschrift für die Nachricht zurück, mittels der dem Nutzer
        /// die eingetroffene Push Nachricht angekündigt wird.
        /// </summary>
        /// <param name="msg">Die eingegangene Push Nachricht als PushMessage Instanz.</param>
        /// <returns>Die Überschrift für die Nutzerbenachrichtigung.</returns>
        public string GetUserNotificationHeadline(PushMessage msg)
        {
            string headline = string.Empty;

            switch (msg.PushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    // Id1 ist die Kanal-Id in diesem Kontext.
                    headline = getChannelName(msg.Id1);
                    break;
                case PushType.CHANNEL_DELETED:
                    // Id1 ist die Kanal-Id in diesem Kontext.
                    headline = getChannelName(msg.Id1);
                    break;
                case PushType.CONVERSATION_MESSAGE_NEW:
                    string groupName = getGroupName(msg.Id1);
                    string conversationTitle = getConversationTitle(msg.Id2);
                    headline = groupName + ": " + conversationTitle;
                    break;
                case PushType.GROUP_DELETED:
                    headline = getGroupName(msg.Id1);
                    break;
                case PushType.PARTICIPANT_NEW:
                    headline = getGroupName(msg.Id1);
                    break;
                case PushType.PARTICIPANT_LEFT:
                    headline = getGroupName(msg.Id1);
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    headline = getGroupName(msg.Id1);
                    break;
                case PushType.CONVERSATION_NEW:
                    headline = getGroupName(msg.Id1);
                    break;
            }

            return headline;
        }

        /// <summary>
        /// Liefert den Schlüssel mittels dem der Inhalt der Nutzerbenachrichtigung über die
        /// eingegangene Push Nachricht aus den Resource Dateien extrahiert werden kann. Mittels
        /// diesem Schlüssel kann man also den anzuzeigenden Inhalt der Nutzerbenachrichtigung in der
        /// vom Nutzer bevorzugten Sprache ermitteln.
        /// </summary>
        /// <param name="msg">Die eingegangene Push Nachricht in Form einer PushMessage Instanz.</param>
        /// <returns>Den Schlüssel mittels dem der Inhalt der Nutzerbenachrichtigung extrahiert werden kann.</returns>
        public string GetUserNotificationContentLocalizationKey(PushMessage msg)
        {
            string localizationKey = string.Empty;

            switch (msg.PushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    localizationKey = "PushNotificationReceivedAnnouncementNew";
                    break;
                case PushType.CHANNEL_DELETED:
                    localizationKey = "PushNotificationReceivedChannelDeleted";
                    break;
                case PushType.CONVERSATION_MESSAGE_NEW:
                    localizationKey = "PushNotificationReceivedConversationMessageNew";
                    break;
                case PushType.GROUP_DELETED:
                    localizationKey = "PushNotificationReceivedGroupDeleted";
                    break;
                case PushType.PARTICIPANT_NEW:
                    localizationKey = "PushNotificationParticipantNew";
                    break;
                case PushType.PARTICIPANT_LEFT:
                    localizationKey = "PushNotificationParticipantLeft";                    
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    localizationKey = "PushNotificationParticipantRemoved";
                    break;
                case PushType.CONVERSATION_NEW:
                    localizationKey = "PushNotificationConversationNew";
                    break;
            }

            return localizationKey;
        }

        /// <summary>
        /// Gibt einen von der bevorzugten Sprache unabhängigen String zurück, der an die
        /// Beschreibung der Benachrichtigung angehängt wird.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Den anzuhängenden String.</returns>
        public string GetResourceAppendix(PushMessage msg)
        {
            string resourceAppendix = string.Empty;

            switch (msg.PushType)
            {
                case PushType.PARTICIPANT_NEW:
                    // Id2 ist die Id des betroffenen Teilnehmers.
                    int participantId = msg.Id2;
                    resourceAppendix = getParticipantName(participantId);
                    break;
                case PushType.PARTICIPANT_LEFT:
                    // Id2 ist die Id des betroffenen Teilnehmers.
                    resourceAppendix = getParticipantName(msg.Id2);
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    // Id2 ist die Id des betroffenen Teilnehmers.
                    resourceAppendix = getParticipantName(msg.Id2);
                    break;
                case PushType.CONVERSATION_NEW:
                    // Id2 ist die Id der neuen Konversation.
                    resourceAppendix = getConversationTitle(msg.Id2);
                    break;
            }

            return resourceAppendix;
        }

        /// <summary>
        /// Prüft im Falle einer eingegangenen Announcement, ob der Nutzer benachrichtigt werden soll.
        /// Die Entscheidung wird abhängig von den Anwendungseinstellungen, oder falls definiert, abhängig
        /// von den kanalspezifischen Einstellungen getroffen.
        /// </summary>
        /// <param name="appSettings">Die aktuell gültigen Anwendungseinstellungen.</param>
        /// <param name="channelId">Die Id des betroffenen Kanals.</param>
        /// <returns>Liefert true, wenn der Nutzer benachrichtigt werden soll, ansonsten false.</returns>
        private bool checkNotificationRequiredForNewAnnouncement(AppSettings appSettings, int channelId)
        {
            bool notificationRequired = false;
                        
            DataHandlingLayer.DataModel.Enums.NotificationSetting settings;

            // Hole den Kanal und entscheide abhängig von den Einstellungen.
            Channel affectedChannel = channelController.GetChannel(channelId);
            if (affectedChannel.AnnouncementNotificationSetting
                != DataHandlingLayer.DataModel.Enums.NotificationSetting.APPLICATION_DEFAULT)
            {
                Debug.WriteLine("Take channel specific settings.");

                settings = affectedChannel.AnnouncementNotificationSetting;
            }
            else
            {
                Debug.WriteLine("Take application specific settings.");

                settings = appSettings.MsgNotificationSetting;
            }

            switch (settings)
            {
                case DataHandlingLayer.DataModel.Enums.NotificationSetting.ANNOUNCE_PRIORITY_HIGH:
                    // Prüfe, ob die empfangene Announcement Priorität hoch hatte.
                    Announcement lastRecvAnnouncement = channelController.GetLastReceivedAnnouncement(channelId);
                    if (lastRecvAnnouncement != null)
                    {
                        if (lastRecvAnnouncement.MessagePriority == Priority.HIGH)
                        {
                            notificationRequired = true;
                        }
                    }
                    break;
                case DataHandlingLayer.DataModel.Enums.NotificationSetting.ANNOUNCE_ALL:
                    notificationRequired = true;
                    break;
                case DataHandlingLayer.DataModel.Enums.NotificationSetting.ANNOUNCE_NONE:
                    notificationRequired = false;
                    break;
                default:
                    Debug.WriteLine("No case matched. Will return false.");
                    break;
            }

            Debug.WriteLine("The result of whether the user will be notified is {0}.", notificationRequired);
            return notificationRequired;
        }

        /// <summary>
        /// Prüft im Falle einer eingegangenen ConversationMessage, ob der Nutzer benachrichtigt werden soll.
        /// Die Entscheidung wird abhängig von den Anwendungseinstellungen, oder falls definiert, abhängig
        /// von den gruppenspezifischen Einstellungen getroffen.
        /// </summary>
        /// <param name="appSettings">Die aktuell gültigen Anwendungseinstellungen</param>
        /// <param name="groupId">Die Id der betroffenen Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der betroffenen Konversation.</param>
        /// <returns>Liefert true, wenn der Nutzer benachrichtigt werden soll, ansonsten false.</returns>
        private bool checkNotificationRequiredForConversationMessage(AppSettings appSettings, int groupId, int conversationId)
        {
            bool notificationRequired = false;

            DataHandlingLayer.DataModel.Enums.NotificationSetting settings;

            // Hole die Gruppe und prüfe die Einstellungen.
            Group affectedGroup = groupController.GetGroup(groupId);
            if (affectedGroup.GroupNotificationSetting != 
                DataModel.Enums.NotificationSetting.APPLICATION_DEFAULT)
            {
                Debug.WriteLine("Take group specific settings.");

                settings = affectedGroup.GroupNotificationSetting;
            }
            else
            {
                Debug.WriteLine("Take application specific settings.");

                settings = appSettings.MsgNotificationSetting;
            }

            switch (settings)
            {
                case DataModel.Enums.NotificationSetting.ANNOUNCE_PRIORITY_HIGH:
                    // Prüfe, ob die eingegangene Nachricht die Priorität hoch hatte.
                    ConversationMessage lastMessage = groupController.GetLastConversationMessage(conversationId);
                    if (lastMessage != null)
                    {
                        if (lastMessage.MessagePriority == Priority.HIGH)
                        {
                            notificationRequired = true;
                        }
                    }
                    break;
                case DataModel.Enums.NotificationSetting.ANNOUNCE_ALL:
                    notificationRequired = true;
                    break;
                case DataModel.Enums.NotificationSetting.ANNOUNCE_NONE:
                    notificationRequired = false;
                    break;
                default:
                    Debug.WriteLine("No cases match, will return false.");
                    notificationRequired = false;
                    break;
            }

            Debug.WriteLine("The result of whether the user will be notified is {0}.", notificationRequired);
            return notificationRequired;
        }

        /// <summary>
        /// Gibt den Namen des Kanals zurück, der die angegebene Id besitzt.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals.</param>
        /// <returns>Den Namen des Kanals.</returns>
        private string getChannelName(int channelId)
        {
            string channelName = string.Empty;
            try
            {
                Channel affectedChannel = channelController.GetChannel(channelId);

                if (affectedChannel != null)
                    channelName = affectedChannel.Name;
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("getChannelName: Couldn't extract channel name.");
                Debug.WriteLine("Msg is: {0}.", ex.Message);
            }

            return channelName;
        }

        /// <summary>
        /// Gib den Titel der Konversation zurück, der die angegebene Id besitzt.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <returns>Der Titel der Konversation.</returns>
        private string getConversationTitle(int conversationId)
        {
            string conversationTitle = string.Empty;
            try
            {
                Conversation conversation = groupController.GetConversation(conversationId, false);
                
                if (conversation != null)
                    conversationTitle = conversation.Title;
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("getConversationTitle: Couldn't extract conversation title.");
                Debug.WriteLine("Msg is: {0}.", ex.Message);
            }

            return conversationTitle;
        }

        /// <summary>
        /// Gibt den Namen des Teilnehmers zurück, der über die angegebene Id identifziert wird.
        /// </summary>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <returns>Der Name des Teilnehmers.</returns>
        private string getParticipantName(int participantId)
        {
            string participantName = string.Empty;

            try
            {
                UserController userController = new UserController();
                User user = userController.GetUser(participantId);
                if (user != null)
                {
                    participantName = user.Name;
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("getConversationTitle: Couldn't extract participant name.");
                Debug.WriteLine("Msg is: {0}.", ex.Message);
            }

            return participantName;
        }

        /// <summary>
        /// Gibt den Namen der Gruppe zurück, die durch die Id eindeutig identifiziert ist.
        /// </summary>
        /// <param name="groupId"></param>
        /// <returns></returns>
        private string getGroupName(int groupId)
        {
            string groupName = string.Empty;
            try
            {
                Group group = groupController.GetGroup(groupId);

                if (group != null)
                    groupName = group.Name;
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("getGroupName: Couldn't extract group name.");
                Debug.WriteLine("Msg is: {0}.", ex.Message);
            }

            return groupName;
        }

        #region ChannelBasedHandlers
        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ ANNOUNCEMENT_NEW. Ruft für den betroffenen Kanal
        /// die neusten Announcements vom Server ab und speichert diese in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="msg">Die empfangende Push Nachricht.</param>
        /// <returns>Liefert true, wenn die PushNachricht erfolgreich behandelt wurde, ansonsten false.</returns>
        private async Task<bool> handleAnnouncementNewPushMsgAsync(PushMessage msg)
        {
            // Lese die Kanal-Id des betroffenen Kanals aus.
            int channelId = msg.Id1;

            // Frage ab, was aktuell die höchste MessageNumber ist für diesen Kanal.
            int highestMsgNr = channelController.GetHighestMsgNrForChannel(channelId);

            try
            {
                // Frage die Announcements für diesen Kanal ab.
                List<Announcement> announcements = await channelController.GetAnnouncementsOfChannelAsync(channelId, highestMsgNr, false);

                if (announcements != null)
                {
                    // Speichere die abgerufenen Announcements ab.
                    if (announcements.Count == 1)
                    {
                        await channelController.StoreReceivedAnnouncementAsync(announcements[0]);
                    }
                    else
                    {
                        await channelController.StoreReceivedAnnouncementsAsync(announcements);
                    }
                }
            }
            catch(ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of Announcement_New push message failed. Message is {0}.", ex.Message);
                return false;
            }
         
            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ CHANNEL_CHANGED. Ruft die Kanalinformationen
        /// des betroffenen Kanals ab und aktualisiert die lokalen Datensätze.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleChannelChangedPushMsgAsync(PushMessage msg)
        {
            // Lese die Kanal-Id des betroffenen Kanals aus.
            int channelId = msg.Id1;

            try
            {
                // Rufe die neuesten Informationen zum Kanal ab.
                Channel newChannel = await channelController.GetChannelInfoAsync(channelId);
                // Rufe aktuelle Instanz ab.
                Channel currentChannel = channelController.GetChannel(channelId);

                // Prüfe, ob Aktualisierung erforderlich. Es könnte sein der Nutzer dieser App selbst hat die
                // Aktualisierung ausgelöst, dann wäre der lokale Datensatz schon auf dem aktuellsten Stand.
                if (newChannel != null &&
                    DateTimeOffset.Compare(currentChannel.ModificationDate, newChannel.ModificationDate) < 0)
                {
                    Debug.WriteLine("handleChannelChangedPushMsgAsync: Update required.");
                    // Speichere die neusten Kanalinformationen lokal ab.
                    channelController.ReplaceLocalChannelWhileKeepingNotificationSettings(newChannel);
                }
                else
                {
                    Debug.WriteLine("handleChannelChangedPushMsgAsync: No update required.");
                    return false;
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of Channel_Changed push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ CHANNEL_DELETED. Markiert den 
        /// Kanal lokal als gelöscht.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private bool handleChannelDeletedPushMsg(PushMessage msg)
        {
            // Lese die Kanal-Id des betroffenen Kanals aus.
            int channelId = msg.Id1;

            try
            {
                channelController.MarkChannelAsDeleted(channelId);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of Channel_Deleted push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }


        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ MODERATOR_ADDED. Fügt den 
        /// betroffenen Moderator als Verantwortlichen zu dem angegebenen Kanal hinzu.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleModeratorAddedPushMsgAsync(PushMessage msg)
        {
            // Lese die Kanal-Id des betroffenen Kanals aus.
            int channelId = msg.Id1;

            // Lese die Moderator-Id des betroffenene Moderator aus.
            int moderatorId = msg.Id2;

            try
            {
                // Frage Moderatorendatensätze des Kanals ab.
                List<Moderator> moderators = await channelController.GetResponsibleModeratorsAsync(channelId);
                if (moderators != null)
                {
                    foreach (Moderator moderator in moderators)
                    {
                        if (moderator.Id == moderatorId)
                        {
                            // Füge den Moderator dem Kanal hinzu.
                            channelController.AddModeratorToChannel(channelId, moderator);
                        }
                    }
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of Moderator_Added push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ MODERATOR_REMOVED. Entfernt den 
        /// betroffenen Moderator aus Liste der Verantwortlichen für den angegebenen Kanal.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private bool handleModeratorRemovedPushMsgAsync(PushMessage msg)
        {
            // Lese die Kanal-Id des betroffenen Kanals aus.
            int channelId = msg.Id1;

            // Lese die Moderator-Id des betroffenene Moderator aus.
            int moderatorId = msg.Id2;

            try
            {
                channelController.RemoveModeratorFromChannel(channelId, moderatorId);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of Moderator_Removed push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }
        #endregion

        #region GroupBasedHandlers

        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ CONVERSATION_MESSAGE_NEW. Ruft die
        /// empfange Nachricht vom Server ab und speichert sie in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleConversationMessageNewPushMsgAsync(PushMessage msg)
        {
            // Extrahiere Parameter.
            int groupId = msg.Id1;
            int conversationId = msg.Id2;

            try
            {
                // Frage höchste Nachrichtennummer für Kanal ab.
                int highestMsgNr = groupController.GetHighestMessageNumberOfConversation(conversationId);

                // Frage Nachrichten ab.
                List<ConversationMessage> messages = await groupController.GetConversationMessagesAsync(
                    groupId,
                    conversationId,
                    highestMsgNr,
                    false);

                if (messages != null)
                {
                    if (messages.Count == 1)
                    {
                        bool successful = groupController.StoreConversationMessage(messages.First());
                        if (!successful)
                        {
                            Debug.WriteLine("handleConversationMessageNewPushMsgAsync: Trying to retrieve the missing data.");
                            // Versuche Nachricht mit Abruf der notwendigen Daten zu speichern.
                            await groupController.SynchronizeConversationsWithServerAsync(groupId, true);
                            groupController.StoreConversationMessage(messages.First());
                        }
                    }
                    else
                    {
                        // Speichere die Nachrichten ab.
                        groupController.StoreConversationMessages(groupId, conversationId, messages);
                    }
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ConversationMessageNew push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Push-Nachricht vom Typ GROUP_DELETED. Markiert
        /// die entsprechende Gruppe als gelöscht in den lokalen Datensätzen.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private bool handleGroupDeletedPushMsg(PushMessage msg)
        {
            int groupId = msg.Id1;

            try
            {
                // Markiere Gruppe als gelöscht.
                groupController.MarkGroupAsDeleted(groupId);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of GroupDeleted push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ GROUP_CHANGED. Fragt die neusten Gruppendaten 
        /// vom Server ab. Aktualisiert den lokalen Datensatz der Gruppe.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleGroupChangedPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;

            try
            {
                // Frage neuste Gruppendaten ab.
                Group newGroup = await groupController.GetGroupAsync(groupId, false);

                // Aktualisiere den Datensatz der Gruppe.
                groupController.UpdateGroup(newGroup, false);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of GroupChanged push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ PARTICIPANT_NEW. Fragt die Teilnehmer 
        /// vom Server ab. Fügt den neuen Teilnehmer auch lokal der Gruppe hinzu.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleParticipantNewPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;
            int participantId = msg.Id2;

            try
            {
                // Frage zunächst die Teilnehmer der Gruppe ab.
                List<User> participants = await groupController.GetParticipantsOfGroupAsync(groupId, false);

                if (participants != null && participants.Count > 0)
                {
                    User addedParticipant = participants.Find(item => item.Id == participantId);

                    // Füge Nutzer der Gruppe hinzu.
                    if (addedParticipant != null)
                    {
                        groupController.AddParticipantToGroup(groupId, addedParticipant);
                    }                    
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ParticipantNew push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ PARTICIPANT_LEFT. Setzt den betroffenen
        /// Teilnehmer lokal auf inaktiv.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private bool handleParticipantLeftPushMsg(PushMessage msg)
        {
            int groupId = msg.Id1;
            int participantId = msg.Id2;

            try
            {
                // Frage lokale Teilnehmer ab.
                Dictionary<int, User> participantsLookupDir = groupController.GetParticipantsLookupDirectory(groupId);

                if (participantsLookupDir != null && participantsLookupDir.ContainsKey(participantId))
                {
                    // Setze Teilnehmer auf inaktiv.
                    groupController.ChangeActiveStatusOfParticipant(groupId, participantId, false);
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ParticipantLeft push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ CONVERSATION_NEW. Ruft die Daten der neuen 
        /// Konversation ab und speichert sie lokal ab.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleConversationNewPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;
            int conversationId = msg.Id2;

            try
            {
                // Frage die Daten zur Konversation ab.
                Conversation conversation = await groupController.GetConversationAsync(groupId, conversationId);

                if (conversation != null)
                {
                    bool successful = groupController.StoreConversation(groupId, conversation);
                    if (!successful)
                    {
                        Debug.WriteLine("handleConversationNewPushMsgAsync: Fallback behavior.");
                        await groupController.SynchronizeConversationsWithServerAsync(groupId, true);
                    }
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ConversationNew push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ CONVERSATION_CHANGED. Ruft die Daten der geänderten 
        /// Konversation ab und aktualisiert den lokalen Datensatz der Konversation.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleConversationChangedPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;
            int conversationId = msg.Id2;

            try
            {
                // Frage Daten der geänderten Konversation ab.
                Conversation conv = await groupController.GetConversationAsync(groupId, conversationId);

                if (conv != null)
                {
                    bool successful = groupController.UpdateConversation(groupId, conv);
                    if (!successful)
                    {
                        Debug.WriteLine("handleConversationChangedPushMsgAsync: Fallback behavior.");
                        await groupController.SynchronizeConversationsWithServerAsync(groupId, true);
                    }
                }
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ConversationChanged push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ CONVERSATION_CHANGED_ALL. Stößt eine Synchronisation 
        /// der Konversationen der betroffenen Gruppe an.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private async Task<bool> handleConversationChangedAllPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;

            try
            {
                // Führe Synchronisation der Konversationen der Gruppe aus.
                await groupController.SynchronizeConversationsWithServerAsync(groupId, true);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ConversationChangedAll push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Behandelt eine eingehende Nachricht vom Typ CONVERSATION_DELETED. Löscht die 
        /// betroffene Konversation aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="msg">Die empfangene Push Nachricht.</param>
        /// <returns>Liefert true, wenn Behandlung erfolgreich, ansonsten false.</returns>
        private bool handleConversationDeletedPushMsgAsync(PushMessage msg)
        {
            int groupId = msg.Id1;
            int conversationId = msg.Id2;

            try
            {
                // Lösche Konversation aus den lokalen Datensätzen.
                groupController.DeleteConversation(conversationId);
            }
            catch (ClientException ex)
            {
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund ablaufen.
                Debug.WriteLine("Handling of ConversationDeleted push message failed. Message is {0}.", ex.Message);
                return false;
            }

            return true;
        }
        #endregion


    }
}
