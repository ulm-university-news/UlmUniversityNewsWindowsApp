﻿using DataHandlingLayer.Exceptions;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Post, jsonContent, "/channel");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Create channel request completed successfully.");
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
        }

        /// <summary>
        /// Fragt Kanäle vom Server ab. Die Anfrage kann dabei durch den lastUpdated Parameter eingeschränkt werden.
        /// Wird lastUpdated angegeben, so wird der Server nur Kanäle zurückgeben, die seit dem angegebenen Datum
        /// geämdert wurden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="lastUpdated">Das Datum, ab dem man die Datensätze der Kanäle haben will.</param>
        /// <returns>Eine Liste von Kanal-Ressourcen in Form eines JSON Dokuments als String.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetChannelsRequestAsync(string serverAccessToken, DateTime lastUpdated)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            string restResourcePath = string.Empty;
            if (lastUpdated != null)
            {
                restResourcePath = "/channel?lastUpdated=" + System.Net.WebUtility.UrlEncode(lastUpdated.ToString());
            }
            else
            {
                restResourcePath = "/channel";
            }
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, restResourcePath);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get channels request completed successfully.");
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel?moderatorId="+moderatorId);       

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get channels request completed successfully.");
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
        }

        /// <summary>
        /// Request an den Server, um die Kanal Ressource abzurufen, die durch die angegebene Id identifziert wird.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem man die Ressource abfragen will.</param>
        /// <returns>Gibt die angefragte Ressource in Form eines JSON Dokuments zurück.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetChannelRequestAsync(string serverAccessToken, int channelId)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Patch, jsonContent, "/channel/" + channelId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Update channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Delete, "/channel/" + channelId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("Delete channel with id {0} request completed successfully.", channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
        public async Task<string> SendPostAnnouncementRequestAsync(string serverAccessToken, int channelId, string jsonContent)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Post, jsonContent, "/channel/" + channelId + "/announcement");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Post announcement in channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
        }

        /// <summary>
        /// Frage per Request alle Announcements zum Kanal mit der angegebenen Id vom Server ab.
        /// Die Anfrage kann durch den Parameter messageNr eingeschränkt werden. Hierbei werden nur
        /// Announcements vom Server zurückgeliefert, die eine höhere MessageNr haben, als die angegebene MessageNr.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Announcements abgefragt werden.</param>
        /// <param name="messageNr">Die MessageNr, ab der man die Announcements vom Server haben will.</param>
        /// <returns>Eine Liste von Announcement Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetAnnouncementsRequestAsync(string serverAccessToken, int channelId, int messageNr)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/announcement?messageNr=" + messageNr);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get announcements in channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/announcement/" + messageNr);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("Delete announcement with messageNr {0} in channel with id {1} request completed successfully.", messageNr, channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Post, jsonContent, "/channel/" + channelId + "/reminder");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Create reminder in channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
        }

        /// <summary>
        /// Frage per Request alle Reminder zum angegebenen Kanal ab. Um die Reminder abfragen zu können
        /// muss man einen Moderatorenaccount besitzten und für den angegebenen Kanal verantwortlich sein.
        /// </summary>
        /// <param name="serverAccessToken">Ds Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals zu dem die Reminder abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Reminder Ressourcen in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendGetRemindersRequestAsync(string serverAccessToken, int channelId)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/reminder");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get reminders in channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/reminder/" + reminderId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get reminder with the id {0} in channel with id {1} request completed successfully.", reminderId, channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Patch, jsonContent, "/channel/" + channelId + "/reminder/" + reminderId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Update reminder with the id {0} in channel with id {1} request completed successfully.", reminderId, channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Delete, "/channel/" + channelId + "/reminder/" + reminderId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("Delete reminder with the id {0} in channel with id {1} request completed successfully.", reminderId, channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Post, "/channel/" + channelId + "/user");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Subscribe to channel with id {0} request completed successfully.", channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/user");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get subscribers for the channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
        }

        /// <summary>
        /// Sende Request zum Deabonnieren des angebenen Kanals. Der Nutzer, der durch die angegbene
        /// Id identifiziert wird, wird aus der Abonnentenliste des Kanals ausgetragen.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="channelId">Die Id des Kanals, der deabonniert wird.</param>
        /// <param name="userId">Die Id des Nutzers, der aus der Abonnentenliste ausgetragen wird.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendUnsubscribeChannelRequestAsync(string serverAccessToken, int channelId, int userId)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Delete, "/channel/" + channelId + "/user/" + userId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("Unsubscribe channel with id {0} request completed successfully.", channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Post, jsonContent, "/channel/" + channelId + "/moderator");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Add moderator as responsible moderator for the channel with id {0} request completed successfully.", channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/moderator");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Get moderators of the channel with id {0} request completed successfully.", channelId);
                Debug.WriteLine("Response from server is: " + responseContent);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }

            return responseContent;
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
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, "/channel/" + channelId + "/moderator/" + moderatorId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("Remove moderator as responsible moderator from the channel with id {0} request completed successfully.", channelId);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                string responseContent = await response.Content.ReadAsStringAsync();
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
        }
    }
}