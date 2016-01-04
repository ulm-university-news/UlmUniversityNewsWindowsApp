using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller
{
    /// <summary>
    /// Die MainController Klassen enthält Funktionalität, die von allen Controllern benötigt wird.
    /// Dazu gehört die Bereitstellung des lokalen Nutzerobjekts. Zudem wird Funktionalität bereitgestellt,
    /// die bei der Validierung der Daten im Model aufgetretenen Validierungsfehler an die ViewModels zurückzumelden.
    /// </summary>
    public class MainController
    {

        /// <summary>
        /// Referenz auf eine Realisierung des IValidationErrorReport Interface mittels dem Fehler an den Aufrufer zurückgemeldet werden.
        /// </summary>
        private IValidationErrorReport _validationErrorReporter;

        /// <summary>
        /// Konstruktor zur Initialisierung des MainController.
        /// </summary>
        protected MainController(IValidationErrorReport validationErrorReporter)
        {
            this._validationErrorReporter = validationErrorReporter;
        }

        /// <summary>
        /// Konstruktor für MainController.
        /// </summary>
        protected MainController()
        {

        }

        /// <summary>
        /// Liest die vom Model gelieferten Validierungsfehler aus und meldet sie gesammelt für jede fehlerhafte Property 
        /// zurück an das ViewModel.
        /// </summary>
        /// <param name="validationMessages">Das vom Model überlieferte Verzeichnis von Fehlernachrichten.</param>
        protected void reportValidationErrors(Dictionary<string, string> validationMessages)
        {
            if (_validationErrorReporter != null)
            {
                foreach (KeyValuePair<string, string> entry in validationMessages)
                {
                    // Informiere ViewModel über Validierungsfehler bezüglich dieser Property.
                    _validationErrorReporter.ReportValidationError(entry.Key, entry.Value);
                }
            }
        }

        /// <summary>
        /// Meldet an das ViewModel zurück, dass alle Validierungsfehler entfernt werden sollen.
        /// </summary>
        protected void clearValidationErrors()
        {
            if(_validationErrorReporter != null)
            {
                _validationErrorReporter.RemoveAllFailureMessages();
            }
        }

        /// <summary>
        /// Meldet an das ViewModel zurück, dass der Validierungsfehler zum angegebenen Property entfernt werden soll.
        /// </summary>
        /// <param name="property">Die betroffene Property.</param>
        protected void clearValidationErrorForProperty(string property)
        {
            if (_validationErrorReporter != null)
            {
                _validationErrorReporter.RemoveFailureMessagesForProperty(property);
            }
        }

        /// <summary>
        /// Gibt den lokalen Nutzer zurück. Liefert null zurück wenn kein lokaler
        /// Nutzer definiert ist. Da diese Methode in allen ViewModel Klassen benötigt wird
        /// ist sie in der abstrakten Basisklasse implementiert.
        /// </summary>
        /// <returns>Instanz der User Klasse, oder null wenn kein lokaler Nutzer definiert ist.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn beim Ermitteln des lokalen Nutzers ein Fehler aufgetreten ist.</exception>
        protected User getLocalUser()
        {
            Debug.WriteLine("Get the local user.");
            // Frage zuerst das lokale Nutzerobjekt aus dem Cache ab.
            User localUser = LocalUser.GetInstance().GetCachedLocalUserObject();
            if (localUser == null)
            {
                // Lokales Nutzerobjekt noch nicht im Cache. Frage es aus der DB ab.
                try
                {
                    LocalUserDatabaseManager localUserDB = new LocalUserDatabaseManager();
                    localUser = localUserDB.GetLocalUser();

                    // Speichere Objekt im Cache.
                    LocalUser.GetInstance().CacheLocalUserObject(localUser);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Database exception occurred in GetLocalUser(). Message of exception is: " + ex.Message);
                    // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                    // TODO - hier Exception werfen?
                    //throw new ClientException(ErrorCodes.LocalDatabaseException, "Retrieval of local user account has failed.");
                }
            }

            return localUser;
        }

        /// <summary>
        /// Erzeugt ein Moderator Objekt aus dem übergebenen JSON String. Ist eine
        /// Umwandlung des JSON Strings nicht möglich, so wird eine ClientException 
        /// geworfen.
        /// </summary>
        /// <param name="jsonString">Der JSON String, der in ein Moderator Objekt umgewandelt werden soll.</param>
        /// <returns>Eine Instanz der Klasse Moderator.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException wenn kein Moderator Objekt aus dem JSON String übergeben werden kann.</exception>
        protected Moderator parseModeratorObjectFromJSON(string jsonString)
        {
            Moderator moderator = null;
            try
            {
                moderator = JsonConvert.DeserializeObject<Moderator>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("Error during deserialization. Exception is: " + ex.Message);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of JSON object has failed.");
            }
            return moderator;
        }
    }
}
