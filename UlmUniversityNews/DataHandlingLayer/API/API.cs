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
        public API(){
            baseURL = "http://134.60.71.137/ulm-university-news";
            //baseURL = "http://localhost:8080/";
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
