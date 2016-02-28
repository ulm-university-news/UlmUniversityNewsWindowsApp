using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace DataHandlingLayer.Controller
{
    public class LoginController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Referenz der API Klasse.
        /// </summary>
        private API.API api;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der LoginController Klasse.
        /// </summary>
        public LoginController()
            : base()
        {
            api = new API.API();
        }

        /// <summary>
        /// Erzeugt eine Instanz der LoginController Klasse.
        /// </summary>
        /// <param name="validationErrorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public LoginController(IValidationErrorReport validationErrorReporter)
            : base(validationErrorReporter)
        {
            api = new API.API(); 
        }

        /// <summary>
        /// Führt den Login Vorgang aus. Die übergebenen Accountdaten werden an den
        /// Server übermittelt und dort geprüft. 
        /// </summary>
        /// <param name="username">Der vom Nutzer eingegebene Nutzername.</param>
        /// <param name="password">Das vom Nutzer eingegebene Passwort.</param>
        /// <returns>Liefert true, wenn der Login-Vorgang erfolgreich abgeschlossen wurde, sonst false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn beim Login-Vorgang ein Fehler aufgetreten ist.</exception>
        public async Task<bool> PerformLoginAsync(string username, string password){
            // Verzeichnis für mögliche Validationsfehler während des Logins.
            Dictionary<string, string> validationErrors = new Dictionary<string, string>();

            if (username == null)
            {
                validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCaseUsernameNull");
                reportValidationErrors(validationErrors);
                return false;
            }

            if (password == null)
            {
                validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCasePasswordNull");
                reportValidationErrors(validationErrors);
                return false;
            }

            // Hash über Passwort bilden, so dass das Passwort nicht im Klartext an den Server übertragen wird.
            string passwordHash = hashPassword(password);

            // Erzeuge Moderatoren-Instanz.
            Moderator moderator = new Moderator()
            {
                Name = username,
                Password = passwordHash
            };

            string moderatorJsonString = parseModeratorToJson(moderator);

            if (moderatorJsonString != null)
            {
                try
                {
                    string serverResponse = await api.SendHttpPostRequestWithJsonBodyAsync(
                        getLocalUser().ServerAccessToken,
                        moderatorJsonString,
                        "/moderator/authentication",
                        null);

                    Moderator loggedInModerator = parseModeratorObjectFromJSON(serverResponse);

                    // Lege Moderatorobjekt mit Daten des eingeloggten Moderators in den Cache.
                    LocalModerator.GetInstance().CacheModeratorObject(loggedInModerator);
                    clearValidationErrorForProperty("LoginFailedMsg");
                }
                catch (APIException ex)
                {
                    // Fange einige der möglichen Fehlerfälle ab und bilde sie in diesem Fall
                    // auf Validationsfehler ab.            
                    switch (ex.ErrorCode)
                    {
                        case ErrorCodes.ModeratorUnauthorized:
                            validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCaseUnauthorized");
                            reportValidationErrors(validationErrors);
                            break;
                        case ErrorCodes.ModeratorNotFound:
                            validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCaseNotFound");
                            reportValidationErrors(validationErrors);
                            break;
                        case ErrorCodes.ModeratorDeleted:
                            validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCaseGone");
                            reportValidationErrors(validationErrors);                            
                            break;
                        case ErrorCodes.ModeratorLocked:
                            validationErrors.Add("LoginFailedMsg", "LoginPageLoginFailureCaseLocked");
                            reportValidationErrors(validationErrors);
                            break;
                        default:
                            // Bilde auf ClientException ab und reiche den Fehler weiter.
                            throw new ClientException(ex.ErrorCode, "Login not successful, msg is: " + ex.Message);
                    }

                    Debug.WriteLine("Login failed.");
                    return false;
                }
            }

            Debug.WriteLine("Login successful.");
            return true;
        }

        /// <summary>
        /// Berechnet einen Hash über das übergebene Klartextpasswort. 
        /// </summary>
        /// <param name="password">Das Passwort im Klartext.</param>
        /// <returns>Einen Hash über das übergebene Passwort als String.</returns>
        private string hashPassword(string password)
        {
            Debug.WriteLine("The cleartext password is: {0}.", password);
            IBuffer bufferedPassword = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);

            // Apply SHA-256 Hash function.
            var hashFunction = HashAlgorithmProvider.OpenAlgorithm("SHA256");
            IBuffer hashedPassword = hashFunction.HashData(bufferedPassword);

            string passwordHash = CryptographicBuffer.EncodeToHexString(hashedPassword);
            Debug.WriteLine("The hashed password is: {0}.", passwordHash);

            return passwordHash;
        }

        /// <summary>
        /// Wandelt ein Objekt vom Typ Moderator in eine JSON Repräsentation um.
        /// </summary>
        /// <param name="moderator">Das umzuwandelnde Objekt.</param>
        /// <returns>Ein JSON-Dokument, welches das übergebene Objekt repräsentiert. Null falls Umwandlung fehlschlägt.</returns>
        private string parseModeratorToJson(Moderator moderator)
        {
            string jsonContent = null;
            try
            {
                jsonContent = JsonConvert.SerializeObject(moderator);
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception occurred during json parsing. Msg is {0}.", ex.Message);
            }
            return jsonContent;
        }
    }
}
