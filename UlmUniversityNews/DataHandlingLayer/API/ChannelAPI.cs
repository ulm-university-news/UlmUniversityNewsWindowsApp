using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Web.Http;

namespace DataHandlingLayer.API
{
    /// <summary>
    /// Die Klasse ChannelAPI stellt Methoden bereit, um Requests an den REST Server abzusetzen. Mittels den bereitgestellten 
    /// Methoden können Requests bezüglich Kanälen und den entsprechenden Subressourcen abgesetzt werden. 
    /// </summary>
    public class ChannelAPI : API
    {
        /// <summary>
        /// Sendet einen Request zum Server, um einen neuen Kanal anzulegen. Die Daten für den
        /// neuen Kanal werden in Form eines JSON Dokuments im Body des Requests an den Server übermittelt.
        /// Damit der Request erfolgreich ist muss ein gültiges AccessToken eines Moderatoren oder Administratoren Accounts
        /// angegeben werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="jsonContent">Das JSON Dokument mit den Kanaldaten in Form eines Strings.</param>
        /// <returns>Die Antwort des Servers als String.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendCreateChannelRequestAsync(string serverAccessToken, string jsonContent)
        {
            // Setzte Request zum Anlegen eines Kanals ab.
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/channel",
                null
                );

            return serverResponse;
        }

        /// <summary>
        /// Fragt Kanäle vom Server ab. Die Anfrage kann dabei durch den lastUpdated Parameter eingeschränkt werden.
        /// Wird lastUpdated angegeben, so wird der Server nur Kanäle zurückgeben, die seit dem angegebenen Datum
        /// geändert wurden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="lastUpdated">Das Datum, ab dem man die Datensätze der Kanäle haben will.</param>
        /// <returns>Eine Liste von Kanal-Ressourcen in Form eines JSON Dokuments als String.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetChannelsRequestAsync(string serverAccessToken, DateTimeOffset lastUpdated)
        {
            Dictionary<string, string> parameters = null;
            if (lastUpdated != DateTimeOffset.MinValue)
            {
                // Erzeuge Parameter für lastUpdate;
                parameters = new Dictionary<string, string>();
                parameters.Add("lastUpdated", base.ParseDateTimeToISO8601Format(lastUpdated));
            }

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel",
                parameters,
                true);

            return serverResponse;
        }

        /// <summary>
        /// Sendet einen Request, um alle Kanäle vom Server abzufragen, für die der Moderator mit der angegebenen Id zuständig ist.
        /// Der Request kann nur von einem Moderator durchgeführt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestor.</param>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <returns>Liste von Kanalressourcen in Form eines JSON Dokuments als String.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetChannelsAssignedToModeratorRequestAsync(string serverAccessToken, int moderatorId)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("moderatorId", moderatorId.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel",
                parameters,
                false
                );

            return serverResponse;
        }

        /// <summary>
        /// Request an den Server, um die Kanal Ressource abzurufen, die durch die angegebene Id identifziert wird.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem man die Ressource abfragen will.</param>
        /// <param name="withCaching">Gibt an, ob der HttpClient die Antwort auch aus dem Cache laden darf, falls
        ///     derselbe Request vor kurzer Zeit bereits einmal ausgeführt wurde.</param>
        /// <returns>Gibt die angefragte Ressource in Form eines JSON Dokuments zurück.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetChannelRequestAsync(string serverAccessToken, int channelId, bool withCaching)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId,
                null, 
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request an den Server, um den Kanal mit der angegebenen Id zu aktualisieren. 
        /// Die Aktualisierung darf nur durch einen Moderator erfolgen, der auch als Moderator für diesen
        /// Kanal eingetragen ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, der aktualisiert werden soll.</param>
        /// <param name="jsonContent">Die durchzuführenden Aktualisierungen in Form eines JSON Merge Patch Dokuments.</param>
        /// <returns>Die aktualisierte Ressource in Form eines JSON Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendUpdateChannelRequestAsync(string serverAccessToken, int channelId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPatchRequestWithJsonBody(
                serverAccessToken,
                jsonContent,
                "/channel/" + channelId,
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen des Kanals mit der angegebenen Id zum Server.
        /// Der Kanal kann nur von einem zugeteilten Moderator gelöscht werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des zu löschenden Kanals.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendDeleteChannelRequest(string serverAccessToken, int channelId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/channel/" + channelId);
        }

        // Announcement Requests:

        /// <summary>
        /// Sende eine neue Announcement im Kanal mit der angegebenen Id. Die Announcement
        /// kann nur von einem Moderator gesendet werden, der zuständig für den Kanal ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, in den die Announcement geschickt wird.</param>
        /// <param name="jsonContent">Die Announcement in Form eines JSON-Dokuments.</param>
        /// <returns>Die erstellte Announcement Ressource in Form eines JSON Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendCreateAnnouncementRequestAsync(string serverAccessToken, int channelId, string jsonContent)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/channel/" + channelId + "/announcement",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Frage per Request alle Announcements zum Kanal mit der angegebenen Id vom Server ab.
        /// Die Anfrage kann durch den Parameter messageNr eingeschränkt werden. Hierbei werden nur
        /// Announcements vom Server zurückgeliefert, die eine höhere MessageNr haben, als die angegebene MessageNr.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Announcements abgefragt werden.</param>
        /// <param name="messageNr">Die MessageNr, ab der man die Announcements vom Server haben will.</param>
        /// <param name="withCaching">Gibt an, ob der HttpClient die Antwort auch aus dem Cache nehmen darf,
        ///     wenn der selbe Request kurze Zeit zuvor schon ausgeführt wurde.</param>
        /// <returns>Eine Liste von Announcement Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetAnnouncementsRequestAsync(string serverAccessToken, int channelId, int messageNr, bool withCaching)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("messageNr", messageNr.ToString());

            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/announcement",
                parameters,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen der Announcement Nachricht mit der angegebenen MessageNr aus dem Kanal,
        /// der durch die spezifizierte Id identifziert wird. Zum Löschen einer Announcement muss man
        /// einen Moderatorenaccount besitzten und für den entsprechenden Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, aus dem die Announcement gelöscht werden soll.</param>
        /// <param name="messageNr">Die MessageNr der Announcement, die gelöscht werden soll.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendDeleteAnnouncementRequestAsync(string serverAccessToken, int channelId, int messageNr)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/announcement/" + messageNr
                );
        }

        // Reminder Requests:

        /// <summary>
        /// Sende einen Request zum Anlegen eines neuen Reminder im Kanal mit der angegebenen Id. Die Daten des Reminder
        /// werden in Form eines JSON-Dokuments übergeben. Um einen Reminder anlegen zu können muss man einen Moderatorenaccount
        /// besitzten und für den angebenen Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, für den der neue Reminder angelegt wird.</param>
        /// <param name="jsonContent">Die Daten des Reminder in Form eines JSON-Dokuments.</param>
        /// <returns>Die neu angelegte Ressource in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendCreateReminderRequestAsync(string serverAccessToken, int channelId, string jsonContent)
        {
            // Setzte Request an Server ab.
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/channel/" + channelId + "/reminder",
                null);

            return serverResponse;
        }

        /// <summary>
        /// Frage per Request alle Reminder zum angegebenen Kanal ab. Um die Reminder abfragen zu können
        /// muss man einen Moderatorenaccount besitzten und für den angegebenen Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Ds Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Reminder abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob der HttpClient die Antwort auch aus dem Cache laden darf, falls
        ///     derselbe Request vor kurzer Zeit bereits einmal ausgeführt wurde.</param>
        /// <returns>Eine Liste von Reminder Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetRemindersRequestAsync(string serverAccessToken, int channelId, bool withCaching)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/reminder",
                null,
                withCaching);

            return serverResponse;
        }

        /// <summary>
        /// Frage per Request einen Reminder, der durch die spezifizierte Id identifiziert wird, zum angegebenen Kanal ab. Um den Reminder
        /// abfragen zu können muss man einen Moderatorenaccount besitzten und für den angegebenen Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Ds Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Reminder abgefragt werden sollen.</param>
        /// <param name="reminderId">Die Id des Reminders, der abgefragt werden sollen.</param>
        /// <returns>Eine Reminder Ressource in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetReminderRequestAsync(string serverAccessToken, int channelId, int reminderId)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/reminder/" + reminderId,
                null,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request, um den Reminder mit der gegebenen Id zu aktualisieren. Um einen Reminder eines
        /// Kanals zu aktualisieren benötigt man einen Moderatorenaccount und muss für den entsprechenden Kanal
        /// verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem der Reminder gehört.</param>
        /// <param name="reminderId">Die Id des Reminders, der geändert werden soll.</param>
        /// <param name="jsonContent">Die Beschreibung an durchzuführenden Änderungen auf der Reminder Ressource in Form eines JSON Merge Patch Dokuments.</param>
        /// <returns>Die aktualisierte Reminder Ressource in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendUpdateReminderRequestAsync(string serverAccessToken, int channelId, int reminderId, string jsonContent)
        {
            // Sende Request an den Server.
            string serverResponse = await base.SendHttpPatchRequestWithJsonBody(
                serverAccessToken,
                jsonContent,
                "/channel/" + channelId + "/reminder/" + reminderId,
                null);

            return serverResponse;
        }

        /// <summary>
        /// Sende einen Request zum Löschen des angegebenen Reminders. Zum Löschen des Reminders benötigt man
        /// einen Moderatorenaccount und muss für den angegebenen Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, von dem der Reminder gelöscht werden soll.</param>
        /// <param name="reminderId">Die Id des zu löschenden Reminders.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendDeleteReminderRequestAsync(string serverAccessToken, int channelId, int reminderId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/reminder/" + reminderId
                );
        }

        // Abonnieren/Deabonnieren eines Kanals:

        /// <summary>
        /// Sende einen Request zum Abonnieren des angegebenen Kanals durch den lokalen Nutzer.
        /// Der lokale Nutzer wird hierbei über das Zugriffstoken auf Serverseite identifiziert.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, der abonniert werden soll.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendSubscribeChannelRequestAsync(string serverAccessToken, int channelId)
        {
            string serverResponse = await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                string.Empty,
                "/channel/" + channelId + "/user",
                null);
        }

        /// <summary>
        /// Sende einen Request zum Abfragen aller Abonnenten eines Kanals. Dieser Request
        /// kann nur von einem Moderator durchgeführt werden, der für den angebenen Kanal zuständig ist.
        /// Der Request kann verwendet werden, um beispielsweise die Anzahl an Abonnenten eines Kanals zu ermitteln.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, für den die Abonnenten abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Nutzer-Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetSubscribersRequestAsync(string serverAccessToken, int channelId)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/user",
                null,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Sende Request zum Deabonnieren des angebenen Kanals. Der Nutzer, der durch das angegbene
        /// AccessToken identifiziert wird, wird aus der Abonnentenliste des Kanals ausgetragen.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, der deabonniert wird.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendUnsubscribeChannelRequestAsync(string serverAccessToken, int channelId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/user"
                );
        }

        // Kanalverwaltung durch Moderatoren:

        /// <summary>
        /// Request, um einen Moderator als Verantwortlichen für einen Kanal einzutragen. Der Moderator wird
        /// hierbei als Ressource in Form eines JSON-Dokuments an den Server übermittelt. Um den Request ausführen 
        /// zu können muss man ein Moderator sein, der für den angegebenen Kanal verantwortlich ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, für den der Moderator als Verantwortlicher eingetragen werden soll.</param>
        /// <param name="jsonContent">Der Moderator, der als Verantwortlicher eingetragen werden soll, als Ressource in Form eines JSON-Dokuments.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendAddModeratorToChannelRequestAsync(string serverAccessToken, int channelId, string jsonContent)
        {
            await base.SendHttpPostRequestWithJsonBodyAsync(
                serverAccessToken,
                jsonContent,
                "/channel/" + channelId + "/moderator",
                null);
        }

        /// <summary>
        /// Request um die verantwortlichen Moderatoren zu einem Kanal abzufgragen. Um diesen Request durchführen zu können
        /// muss man ein Moderator sein, der für den gegebenen Kanal zuständig ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem die verantwortlichen Moderatoren abgefragt werden sollen.</param>
        /// <returns>Liste von Moderatoren-Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetModeratorsOfChannelRequestAsync(string serverAccessToken, int channelId)
        {
            // Frage die verantwortlichen Moderatoren für den Kanal ab. 
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/moderator",
                null,
                false);

            return serverResponse;
        }

        /// <summary>
        /// Request zum Entfernen eines Moderator als Verantwortlichen des Kanals. Dieser Request kann nur 
        /// von Moderatoren durchgeführt werden, die für den gegebenen Kanal verantwortlich sind.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, von dem der Moderator als Verantwortlicher entfernt wird.</param>
        /// <param name="moderatorId">Die Id des Moderators, der von der Liste der verantwortlichen Moderatoren entfernt wird.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendRemoveModeratorFromChannelRequestAsync(string serverAccessToken, int channelId, int moderatorId)
        {
            await base.SendHttpDeleteRequestAsync(
                serverAccessToken,
                "/channel/" + channelId + "/moderator/" + moderatorId);
        }
    }
}
