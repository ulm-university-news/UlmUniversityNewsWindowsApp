using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;
using Newtonsoft.Json;
using DataHandlingLayer.DataModel;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using Windows.UI.Notifications;

namespace DataHandlingLayer.Controller
{
    public class PushNotificationController
    {
        #region Fields
        /// <summary>
        /// Eine Instanz der ChannelController Klasse.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse PushNotificationController.
        /// </summary>
        public PushNotificationController()
        {
            channelController = new ChannelController();
        }

        /// <summary>
        /// Behandelt eine vom WNS empfangene RawNotification. Die Push Nachricht wird geparset und abhängig
        /// von ihrem Typ behandelt.
        /// </summary>
        /// <param name="receivedNotification">Die empfangene RawNotification.</param>
        /// <returns>Liefert true, wenn die PushNachricht erfolgreich behandelt wurde, ansonsten false.</returns>
        public async Task<bool> HandlePushNotificationAsync(RawNotification receivedNotification)
        {
            bool handledSuccessfully = false;

            PushMessage pushMessage = GetPushMessageFromNotification(receivedNotification);

            if(pushMessage == null)
            {
                return handledSuccessfully;
            }

            // Lese als erstes den Typ der empfangenen Push Nachricht aus. Behandle die PushNachricht nach Typ.
            PushType pushType = pushMessage.PushType;
            switch (pushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    handledSuccessfully = await handleAnnouncementNewPushMsg(pushMessage);
                    break;
                case PushType.ANNOUNCEMENT_DELETED:
                    break;
                case PushType.CHANNEL_CHANGED:
                    break;
                case PushType.CHANNEL_DELETED:
                    break;
                case PushType.MODERATOR_ADDED:
                    break;
                case PushType.MODERATOR_CHANGED:
                    break;
                case PushType.MODERATOR_REMOVED:
                    break;
                case PushType.GROUP_DELETED:
                    break;
                case PushType.GROUP_CHANGED:
                    break;
                case PushType.PARTICIPANT_NEW:
                    break;
                case PushType.PARTICIPANT_LEFT:
                    break;
                case PushType.PARTICIPANT_REMOVED:
                    break;
                case PushType.PARTICIPANT_CHANGED:
                    break;
                case PushType.CONVERSATION_NEW:
                    break;
                case PushType.CONVERSATION_CHANGED:
                    break;
                case PushType.CONVERSATION_CHANGED_ALL:
                    break;
                case PushType.CONVERSATION_CLOSED:
                    break;
                case PushType.CONVERSATION_DELETED:
                    break;
                case PushType.CONVERSATION_MESSAGE_NEW:
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
            PushMessage pushMessage = null;
            try
            {
                pushMessage = JsonConvert.DeserializeObject<PushMessage>(notificationJsonContent);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("Handling of received push notification not successful. Json parser error occurred. " +
                    "Message is {0}.", ex.Message);
            }

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

            bool notificationRequired = false;

            switch(pm.PushType)
            {
                case PushType.ANNOUNCEMENT_NEW:
                    // TODO - Prüfe Anwendungseinstellungen ab. Soll der Nutzer über die eingetroffene Announcement informiert werden?
                    notificationRequired = true;
                    break;
            }

            return notificationRequired;
        }

        /// <summary>
        /// Behandelt eine eingehende Push Nachricht vom Typ ANNOUNCEMENT_NEW. Ruft für den betroffenen Kanal
        /// die neusten Announcements vom Server ab und speichert diese in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="msg">Die empfangende Push Nachricht.</param>
        /// <returns>Liefert true, wenn die PushNachricht erfolgreich behandelt wurde, ansonsten false.</returns>
        private async Task<bool> handleAnnouncementNewPushMsg(PushMessage msg)
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
                // Keine weitere Fehlerbehandlung hier, da dies Operationen im Hintergrund sind.
                Debug.WriteLine("Handling of Announcement_New push message failed. Message is {0}.", ex.Message);
                return false;
            }
         
            return true;
        }
               

    }
}
