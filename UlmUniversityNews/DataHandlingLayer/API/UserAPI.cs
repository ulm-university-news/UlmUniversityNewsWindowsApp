using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web;
using Windows.Web.Http;

namespace DataHandlingLayer.API
{
    public class UserAPI : API
    {
        /// <summary>
        /// Sende einen Request an den REST-Server zum Erstellen eines neuen Nutzeraccounts.
        /// Bei erfolgreicher Erstellung des Accounts werden die Daten vom Server zurückgeliefert.
        /// </summary>
        /// <param name="jsonContent">Der JSON String mit den Daten für den Request.</param>
        /// <returns>Die Antwort des Servers im JSON Format bei erfolgreichem Request.</returns>
        /// <exception cref="APIException">Bei vom Server übermittelten Fehler-Nachricht.</exception>
        public async Task<String> SendCreateUserRequestAsync(String jsonContent)
        {
            // Erstelle einen Http-Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request Header.
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.Content = new HttpStringContent(jsonContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            request.RequestUri = new Uri(BaseURL + "/user");

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.SendRequestAsync(request);
            }
            catch (Exception ex) 
            {
                Debug.WriteLine("Exception thrown in SendCreateUserRequest. Message is: " + ex.Message + " and HResult is: " + ex.GetBaseException().HResult);
                WebErrorStatus error = WebError.GetStatus(ex.GetBaseException().HResult);
                if (WebErrorStatus.ServerUnreachable == error || WebErrorStatus.ServiceUnavailable == error 
                    || WebErrorStatus.Timeout == error || error == WebErrorStatus.HostNameNotResolved){
                        Debug.WriteLine("Throwing an APIException with ServerUnreachable.");
                        // Die Anfrage konnte nicht erfolgreich an den Server übermittelt werden.
                        throw new APIException(-1, ErrorCodes.ServerUnreachable);
                }
                if(response == null){
                    Debug.WriteLine("Cannot continue. Response object is null.");
                    throw new APIException(-1, ErrorCodes.ServerUnreachable);
                }
            }

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Created)
            {
                Debug.WriteLine("Create user request completed successfully.");
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
        /// Sende einen PATCH Request an den Server um den Nutzeraccount auf Serverseite zu aktualisieren.
        /// Bei erfolgreichem Aktualisieren des Nutzeraccounts werden die aktualisierten Daten vom Server
        /// zurückgeliefert.
        /// </summary>
        /// <param name="userId">Die Id des Nutzeraccounts, der aktualisiert werden soll.</param>
        /// <param name="serverAccessToken">Das Zugriffstoken des Anfragers mittels dem er auf dem Server identifiziert wird.</param>
        /// <param name="jsonContent">Das Json Patch Dokument.</param>
        /// <returns>Die Antwort des Servers im JSON Format bei erfolgreichem Request. Hier die aktualisierte Nutzer-Ressource.</returns>
        public async Task<String> SendUpdateUserRequestAsync(int userId, string serverAccessToken, string jsonContent)
        {
            // Erstelle einen Http-Request.
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);

            // Definiere HTTP-Request Header.
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Patch;
            request.Content = new HttpStringContent(jsonContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            request.RequestUri = new Uri(BaseURL + "/user/" + userId);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = null;
            try
            {
                response = await httpClient.SendRequestAsync(request);
            }
            catch (Exception ex)
            {
                if (response == null)
                {
                    Debug.WriteLine("Exception occured with message: " + ex.Message + "Throwing an APIException with ServerUnreachable.");
                    Debug.WriteLine("Cannot continue. Response object is null.");
                    throw new APIException(-1, ErrorCodes.ServerUnreachable);
                }
            }

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            string responseContent = await response.Content.ReadAsStringAsync();
            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("Update user request completed successfully.");
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
        /// Sende eine Anfrage an den Server, um den Datensatz des Nutzers abzufragen, der durch die 
        /// angegebene Id identifiziert ist.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Anfragers.</param>
        /// <param name="userId">Die Id des Nutzers, dessen Datensatz abgefragt werden soll.</param>
        /// <returns>Die Antwort des Strings als Server. Hier die abgefragte Nutzer-Ressource.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Anfrage fehlschlägt, oder der Server diese ablehnt.</exception>
        public async Task<string> SendGetUserRequestAsync(string serverAccessToken, int userId)
        {
            string serverResponse = await base.SendHttpGetRequestAsync(
                serverAccessToken,
                "/user/" + userId.ToString(),
                null,
                false);

            return serverResponse;
        }
    }
}
