﻿using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;
using Windows.Web.Http.Filters;

namespace DataHandlingLayer.API
{
    /// <summary>
    /// </summary>
    public class API
    {
        private string baseURL;
        /// <summary>
        /// Die Basis-URL für Requests an den REST-Server.
        /// </summary>
        public string BaseURL
        {
            get { return baseURL; }
            set { baseURL = value; }
        }

        /// <summary>
        /// Konstruktor zur Erzeugung einer Instanz der API Klasse.
        /// </summary>
        public API(){
            baseURL = "http://134.60.71.137/ulm-university-news";
        }

        /// <summary>
        /// Sende einen HTTP POST Request an den Server, der als Inhalt das übergebene JSON-Dokument enthält.
        /// Mittels dieses Requests kann eine Ressource auf dem Server angelegt werden. 
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="content">Der zu sendende Inhalt in Form eines JSON-Dokuments.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <param name="urlParameters">Per URI zu übergebene Parameter. Dieser Parameter kann null sein, wenn keine Parameter übergeben werden sollen.</param>
        /// <returns>Die Antwort des Servers bei erfolgreicher Bearbeitung des Requests in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendHttpPostRequestWithJsonBodyAsync(string serverAccessToken, string content, string restResourcePath, Dictionary<string, string> urlParameters)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            // Füge URL Parameter an, falls vorhanden.
            if (urlParameters != null && urlParameters.Count != 0)
            {
                restResourcePath = createRestURIWithParameters(restResourcePath, urlParameters);
            }

            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Post, content, restResourcePath);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            var buffer = await response.Content.ReadAsBufferAsync();
            byte[] rawBytes = new byte[buffer.Length];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(rawBytes);
            }
            string responseContent = Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length); 

            if (statusCode == HttpStatusCode.Created || statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("POST request to URI {0} completed successfully.", request.RequestUri);
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
        /// Sende einen PATCH Request an den Server, der als Inhalt eine Beschreibung von durchzuführenden
        /// Änderungen in Form eines JSON Merge Patch Dokuments beinhaltet. Mittels dieses Requests können
        /// Änderungen an Ressourcen vorgenommen werden.
        /// </summary>
        /// <param name="serverAccessToken"> Das Zugriffstoken des Requestors.</param>
        /// <param name="content">Die Beschreibung an durchzuführenden Änderungen als JSON-Merge-Patch Dokument.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <param name="urlParameters">Per URI zu übergebene Parameter. Dieser Parameter kann null sein, wenn keine Parameter übergeben werden sollen.</param>
        /// <returns>Die Antwort des Servers bei erfolgreicher Bearbeitung des Requests in Form eines JSON-Dokuments.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendHttpPatchRequestWithJsonBody(string serverAccessToken, string content, string restResourcePath, Dictionary<string, string> urlParameters)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            // Füge URL Parameter an, falls vorhanden.
            if (urlParameters != null && urlParameters.Count != 0)
            {
                restResourcePath = createRestURIWithParameters(restResourcePath, urlParameters);
            }

            HttpRequestMessage request = createHttpRequestMessageWithJsonBody(HttpMethod.Patch, content, restResourcePath);
            Debug.WriteLine("Sending HTTP PATCH request to URL {0} with content {1}.", request.RequestUri, content);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            var buffer = await response.Content.ReadAsBufferAsync();
            byte[] rawBytes = new byte[buffer.Length];
            using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(rawBytes);
            }
            string responseContent = Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length); 

            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("PATCH request to URI {0} completed successfully." , request.RequestUri);
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
        /// Sende einen HTTP GET Request an den Server. Mittels diesem Request können Ressourcen
        /// vom Server abgefragt werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <param name="urlParameters">Per URI zu übergebene Parameter. Dieser Parameter kann null sein, wenn keine Parameter übergeben werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching durch den HttpClient bei diesem Request gewünscht ist oder nicht.</param>
        /// <returns>Die Antwort des Servers bei erfolgreicher Bearbeitung des Requests in Form eines JSON-Dokuments.</returns>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task<string> SendHttpGetRequestAsync(string serverAccessToken, string restResourcePath, Dictionary<string, string> urlParameters, bool withCaching)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();
            
            if(!withCaching)
            {
                // Definiere HttpClient neu.
                var rootFilter = new HttpBaseProtocolFilter();

                rootFilter.CacheControl.ReadBehavior = Windows.Web.Http.Filters.HttpCacheReadBehavior.MostRecent;
                rootFilter.CacheControl.WriteBehavior = Windows.Web.Http.Filters.HttpCacheWriteBehavior.NoCache;

                httpClient = new HttpClient(rootFilter);

                // Füge zusätzlich noch If-Modified Since Header hinzu.              
                httpClient.DefaultRequestHeaders.IfModifiedSince = new DateTimeOffset(DateTime.UtcNow);
                Debug.WriteLine("Adding header to prevent caching. Added header is: {0}.",
                    httpClient.DefaultRequestHeaders.IfModifiedSince);
            }

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            // Füge URL Parameter an, falls vorhanden.
            if (urlParameters != null && urlParameters.Count != 0)
            {
                restResourcePath = createRestURIWithParameters(restResourcePath, urlParameters);
            }

            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Get, restResourcePath);

            Debug.WriteLine("GET Request to URI: {0}", request.RequestUri);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            var buffer = await response.Content.ReadAsBufferAsync();
            byte[] rawBytes = new byte[buffer.Length];
            using(var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(rawBytes);
            }
            string responseContent = Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length); 

            if (statusCode == HttpStatusCode.Ok)
            {
                Debug.WriteLine("GET request to URI {0} completed successfully.", request.RequestUri);
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
        /// Sende einen HTTP DELETE Request an den Server. Mittels diesem Request können Ressourcen
        /// gelöscht werden.
        /// </summary>
        /// <param name="serverAccessToken">Das Zugriffstoken des Requestors.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <exception cref="APIException">Wirft APIException, wenn Request fehlgeschlagen ist, oder Server den Request abgelehnt hat.</exception>
        public async Task SendHttpDeleteRequestAsync(string serverAccessToken, string restResourcePath)
        {
            // Erstelle einen HTTP Request.
            HttpClient httpClient = new HttpClient();

            // Definiere HTTP-Request und Http-Request Header.
            httpClient.DefaultRequestHeaders.Add("Authorization", serverAccessToken);
            httpClient.DefaultRequestHeaders.Accept.TryParseAdd("application/json");

            HttpRequestMessage request = createHttpRequestMessageWithoutContent(HttpMethod.Delete, restResourcePath);

            // Sende den Request und warte auf die Antwort.
            HttpResponseMessage response = await sendHttpRequest(httpClient, request);

            // Lies Antwort aus.
            var statusCode = response.StatusCode;
            if (statusCode == HttpStatusCode.NoContent)
            {
                Debug.WriteLine("DELETE request to URI {0} completed successfully.", request.RequestUri);
            }
            else
            {
                // Bilde auf Fehlercode ab und werfe Exception.
                var buffer = await response.Content.ReadAsBufferAsync();
                byte[] rawBytes = new byte[buffer.Length];
                using (var reader = Windows.Storage.Streams.DataReader.FromBuffer(buffer))
                {
                    reader.ReadBytes(rawBytes);
                }
                string responseContent = Encoding.UTF8.GetString(rawBytes, 0, rawBytes.Length); 
                mapNonSuccessfulRequestToAPIException(statusCode, responseContent);
            }
        }

        /// <summary>
        /// Eine Hilfsmethode, die ein DateTimeOffset Objekt in ein Format umwandelt, welches dem ISO 8601 Standard entspricht, und
        /// als String zurückliefert. Diese Methode kann verwendet werden, um DateTimeOffset Objekte in ein
        /// Format zu bringen, die vom Server verstanden werden. 
        /// </summary>
        /// <param name="datetime">Das umzuwandelnde DateTimeOffset Objekt.</param>
        /// <returns>Die Datums- und Uhrzeitangabe im UTC Format.</returns>
        public string ParseDateTimeToISO8601Format(DateTimeOffset datetime)
        {
            // DateTimeOffset dto = new DateTimeOffset(datetime,TimeZoneInfo.Local.GetUtcOffset(DateTimeOffset.Now));
            CultureInfo cultureInfo = new CultureInfo("de-DE");

            string format = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzzz";
            string datetimeString = datetime.ToString(format, cultureInfo);

            //string datetimeString = datetime.ToUniversalTime().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");;
            
            return datetimeString;
        }

        /// <summary>
        /// Hilfsmethode, die Parameter an die REST URI anhängt. 
        /// </summary>
        /// <param name="restResourcePath">Der Ressourcen Pfad der REST URI, an den die Parameter angehängt werden sollen.</param>
        /// <param name="urlParameters">Ein Verzeichnis der anzuhängenden Parameter. Die Parameter werden als Schlüssel-Wert Paare übergeben.</param>
        /// <returns>Die URI mit angehängten Parametern als String.</returns>
        private string createRestURIWithParameters(string restResourcePath, Dictionary<string, string> urlParameters)
        {
            // Füge URL Parameter an, falls vorhanden.
            if (urlParameters != null && urlParameters.Count != 0)
            {
                // Hole erstes Element.
                var element = urlParameters.ElementAt(0);
                restResourcePath = restResourcePath + "?" + System.Net.WebUtility.UrlEncode(element.Key) + "=" + System.Net.WebUtility.UrlEncode(element.Value);
                urlParameters.Remove(element.Key);

                while (urlParameters.Count > 0)
                {
                    var secondaryElement = urlParameters.ElementAt(0);
                    // Hänge weitere Parameter an.
                    restResourcePath = restResourcePath + "&" + System.Net.WebUtility.UrlEncode(secondaryElement.Key) + "=" + System.Net.WebUtility.UrlEncode(secondaryElement.Value);
                    urlParameters.Remove(secondaryElement.Key);
                }
            }

            return restResourcePath;
        }

        /// <summary>
        /// Eine Hilfsmethode, die einen HTTP Request absetzt und die Antwort des Servers in Form
        /// einer HttpResponseMessage zurückliefert.
        /// </summary>
        /// <param name="httpClient">Die Instanz des HttpClient, mittels der der Request abgeschickt werden soll.</param>
        /// <param name="request">Der Request in Form eines HttpRequestMessage Objekts.</param>
        /// <returns>Eine Instanz vom Typ HttpResponseMessage.</returns>
        /// <exception cref="APIException">Wirt APIException, wenn Request fehlschlägt.</exception>
        protected async Task<HttpResponseMessage> sendHttpRequest(HttpClient httpClient, HttpRequestMessage request)
        {
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

            return response;
        }

        /// <summary>
        /// Eine Hilfsmethode, die aus den übergebenen Parametern ein Objekt vom Typ HttpRequestMessage formt.
        /// </summary>
        /// <param name="httpMethod">Die zu verwendende Http-Methode, z.B. POST oder GET.</param>
        /// <param name="jsonContent">Der eigentliche Inhalt in Form eines JSON Dokuments.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <returns>Ein Objekt vom Typ HttpRequestMessage.</returns>
        protected HttpRequestMessage createHttpRequestMessageWithJsonBody(HttpMethod httpMethod, string jsonContent, string restResourcePath)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = httpMethod;
            request.Content = new HttpStringContent(jsonContent, Windows.Storage.Streams.UnicodeEncoding.Utf8, "application/json");
            request.RequestUri = new Uri(BaseURL + restResourcePath);

            return request;
        }

        /// <summary>
        /// Eine Hilfsmethode, die aus den übergebenen Parametern ein Objekt vom Typ HttpRequestMessage formt.
        /// </summary>
        /// <param name="httpMethod">Die zu verwendende Http-Methode, z.B. POST oder GET.</param>
        /// <param name="restResourcePath">Der REST Ressourcen Pfad, der an die Basis URI des Requests angehängt wird. Dieser Teil
        /// der URI spezifiziert die exakte Ressource, die über den Request angesprochen wird.</param>
        /// <returns>Ein Objekt vom Typ HttpRequestMessage.</returns>
        protected HttpRequestMessage createHttpRequestMessageWithoutContent(HttpMethod httpMethod, string restResourcePath)
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = httpMethod;
            request.RequestUri = new Uri(BaseURL + restResourcePath);

            return request;
        }

        /// <summary>
        /// Extrahiert den ErrorCode aus einer vom REST-Server übermittelten Json Fehlernachricht. 
        /// </summary>
        /// <param name="jsonString">Die Fehlernachricht im Json Format.</param>
        /// <returns>Den extrahierten Error Code, oder -1 wenn der Error-Code nicht extrahiert werden konnte.</returns>
        protected int parseErrorCodeFromJson(string jsonString)
        {            
            int errorCode;
            try
            {
                JsonObject jsonObj = Windows.Data.Json.JsonValue.Parse(jsonString).GetObject();
                errorCode = Convert.ToInt32(jsonObj["errorCode"].GetNumber());
            }
            catch (Exception ex) 
            {
                Debug.WriteLine("Error parsing error code. The exception message is: " + ex.Message);
                errorCode = -1;
            }
            return errorCode;
        }

        /// <summary>
        /// Diese Methode kann genutzt werden, wenn man vom Server eine Antwort erhält, die durch den Status Code auf 
        /// eine fehlerhafte Bearbeitung des Requests hinweist. Die Methode bildet den Inhalt einer HTTP Response Nachricht
        /// auf einen Fehlercode ab und wirft eine APIException an den Aufrufer der API Methode zurück. 
        /// </summary>
        /// <param name="statusCode">Der Status Code der HTTP Response vom Server.</param>
        /// <param name="responseContent">Der Inhalt der HTTP Response Nachricht.</param>
        protected void mapNonSuccessfulRequestToAPIException(HttpStatusCode statusCode, String responseContent)
        {
            int errorCode = -1;
            // Ermittle Fehlercode.
            switch (statusCode)
            {
                case HttpStatusCode.BadRequest:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    if (errorCode == -1)
                    {
                        // Bilde auf generischen Fehlercode ab.
                        errorCode = ErrorCodes.BadRequest;
                    }
                    break;
                case HttpStatusCode.Unauthorized:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                case HttpStatusCode.Forbidden:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                case HttpStatusCode.NotFound:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    if (errorCode == -1)
                    {
                        // Bilde auf generischen Fehlercode ab.
                        errorCode = ErrorCodes.NotFound;
                    }
                    break;
                case HttpStatusCode.MethodNotAllowed:
                    // Bilde auf generischen Fehlercode ab.
                    errorCode = ErrorCodes.MethodNotAllowed;
                    break;
                case HttpStatusCode.Conflict:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                case HttpStatusCode.Gone:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                case HttpStatusCode.Locked:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                case HttpStatusCode.UnsupportedMediaType:
                    // Bilde auf genereischen Fehlercode ab.
                    errorCode = ErrorCodes.UnsupportedMediaType;
                    break;
                case HttpStatusCode.InternalServerError:
                    errorCode = parseErrorCodeFromJson(responseContent);
                    break;
                default:
                    Debug.WriteLine("Could not map response code to an APIException. Answer from server was: " + responseContent);
                    break;
            }

            Debug.WriteLine("Received response with response code: " + (int)statusCode + ". Throw exception with error code: " + errorCode);
            // Gebe aufgetretenen Fehler an den Aufrufer via APIException weiter.
            throw new APIException((int)statusCode, errorCode);
        }

    }
}
