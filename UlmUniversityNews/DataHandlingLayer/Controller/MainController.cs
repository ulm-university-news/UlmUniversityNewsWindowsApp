using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.JsonManager;
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
        #region Fields
        /// <summary>
        /// Referenz auf eine Realisierung des IValidationErrorReport Interface mittels dem Fehler an den Aufrufer zurückgemeldet werden.
        /// </summary>
        private IValidationErrorReport _validationErrorReporter;

        /// <summary>
        /// Eine Referenz auf den JsonParsingManager der Anwendung.
        /// </summary>
        protected JsonParsingManager jsonParser;
        #endregion Fields

        /// <summary>
        /// Konstruktor zur Initialisierung des MainController.
        /// </summary>
        protected MainController(IValidationErrorReport validationErrorReporter)
        {
            this._validationErrorReporter = validationErrorReporter;
            jsonParser = new JsonParsingManager();
        }

        /// <summary>
        /// Konstruktor für MainController.
        /// </summary>
        protected MainController()
        {
            jsonParser = new JsonParsingManager();
        }

        #region ValidationRelatedFunctions
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
        #endregion ValidationRelatedFunctions

        /// <summary>
        /// Gibt den lokalen Nutzer zurück. Liefert null zurück wenn kein lokaler
        /// Nutzer definiert ist. Da diese Methode in allen ViewModel Klassen benötigt wird
        /// ist sie in der abstrakten Basisklasse implementiert.
        /// </summary>
        /// <returns>Instanz der User Klasse, oder null wenn kein lokaler Nutzer definiert ist.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn beim Ermitteln des lokalen Nutzers ein Fehler aufgetreten ist.</exception>
        protected User getLocalUser()
        {
            // Frage zuerst das lokale Nutzerobjekt aus dem Cache ab.
            User localUser = LocalUser.GetInstance().GetCachedLocalUserObject();
            if (localUser == null)
            {
                // Lokales Nutzerobjekt noch nicht im Cache. Frage es aus der DB ab.
                try
                {
                    Debug.WriteLine("Retrieve local user object from DB.");
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
            else
            {
                Debug.WriteLine("Retrieved local user object from cache.");
            }

            return localUser;
        }

        /// <summary>
        /// Gibt den lokalen (also den gerade eingeloggten) Moderator zurück. Liefert null zurück wenn kein lokaler
        /// Moderator definiert ist. Da diese Methode in vielen ViewModel Klassen benötigt wird
        /// ist sie in der abstrakten Basisklasse implementiert.
        /// </summary>
        /// <returns>Eine Instanz der Klasse Moderator. Null, wenn keine Instanz von Moderator gespeichert ist.</returns>
        public Moderator GetLocalModerator()
        {
            return LocalModerator.GetInstance().GetCachedModerator();
        }

        /// <summary>
        /// Liefert die aktuellen Anwendungseinstellungen gekapselt in Form eines Objekts zurück.
        /// Da diese Methode von sehr vielen ViewModel Klassen benötigt wird, ist sie in der abstrakten
        /// Oberklasse definiert.
        /// </summary>
        /// <returns>Instanz der Klasse AppSettings.</returns>
        public AppSettings GetApplicationSettings()
        {
            // Frage zunächst Objekt aus dem Cache ab.
            AppSettings appSettings = AppSettingsCache.GetInstance().GetCachedApplicationSettings();
            if (appSettings == null)
            {
                // Anwendungseinstellungen wurden noch nicht in Cache geladen. Lade sie aus der Datenbank.
                try
                {
                    Debug.WriteLine("Retrieve application settings from DB.");
                    ApplicationSettingsDatabaseManager appSettingsDB = new ApplicationSettingsDatabaseManager();
                    appSettings = appSettingsDB.LoadApplicationSettings();

                    // Speichere Objekt im Cache.
                    AppSettingsCache.GetInstance().CacheApplicationSettings(appSettings);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Database exception occurred in getApplicationSettings(). Message of exception is: " + ex.Message);
                }
            }
            else
            {
                Debug.WriteLine("Retrieved application settings object from cache.");
            }

            return appSettings;
        }

    }
}
