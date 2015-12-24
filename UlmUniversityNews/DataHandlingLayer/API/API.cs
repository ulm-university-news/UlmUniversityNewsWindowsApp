using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Data.Json;
using Windows.Web.Http;

namespace DataHandlingLayer.API
{
    /// <summary>
    /// Stellt die abstrakte Basisklasse aller API Klassen dar. Bietet Funktionalitäten an, die in jeder
    /// API Implementierungsklasse benötigt werden. Hat zudem die Basis-URL, die je nach Anfrage erweitert 
    /// werden kann.
    /// </summary>
    public abstract class API
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
        /// Konstruktor zur Initialisierung der API Klasse.
        /// </summary>
        protected API(){
            baseURL = "http://134.60.71.137/ulm-university-news";
            //baseURL = "http://localhost:8080/";
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
            JsonObject jsonObj = Windows.Data.Json.JsonValue.Parse(jsonString).GetObject();
            int errorCode;
            try
            {
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
