using DataHandlingLayer.API;
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
    public class GroupController : MainController
    {
        #region Field
        /// <summary>
        /// Die Referenz auf die API Komponente für Gruppen.
        /// </summary>
        private GroupAPI groupAPI;

        /// <summary>
        /// Referenz auf den JsonManager, um das Parsen von Objekten durchzuführen.
        /// </summary>
        private JsonParsingManager jsonManager;

        /// <summary>
        /// Refernz auf die Controller Instanz für die Verwaltung der Nutzerdaten.
        /// </summary>
        private UserController userController;

        /// <summary>
        /// Referenz auf die Datenbank Manager Instanz für Gruppen.
        /// </summary>
        private GroupDatabaseManager groupDBManager;
        #endregion Field

        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupController.
        /// </summary>
        public GroupController()
            : base()
        {
            groupAPI = new GroupAPI();
            jsonManager = new JsonParsingManager();
            groupDBManager = new GroupDatabaseManager();
            userController = new UserController();
        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupController.
        /// </summary>
        /// <param name="errorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public GroupController(IValidationErrorReport errorReporter)
            : base(errorReporter)
        {
            groupAPI = new GroupAPI();
            jsonManager = new JsonParsingManager();
            groupDBManager = new GroupDatabaseManager();
            userController = new UserController();
        }

        /// <summary>
        /// Liefert das Nutzerobjekt des lokalen Nutzers zurück.
        /// </summary>
        /// <returns>Instanz der Klasse User.</returns>
        public User GetLocalUser()
        {
            return getLocalUser();
        }

        #region RemoteGroupMethods
        /// <summary>
        /// Anlegen einer neuen Gruppe im System. Die Daten der Gruppe
        /// werden in Form eines Objekts der Klasse Group übergeben. Ein Request
        /// zum Anlegen der Gruppe auf dem Server wird ausgeführt und die erzeugte
        /// Ressource lokal gespeichert.
        /// </summary>
        /// <param name="newGroup">Das Objekt mit den Daten der neuen Gruppe.</param>
        /// <returns>Liefert true, wenn das Anlegen erfolgreich war, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Erstellungsvorgang fehlschlägt.</exception>
        public async Task<bool> CreateGroupAsync(Group newGroup)
        {
            if (newGroup == null)
                return false;

            // Hole das lokale Nutzerobjekt.
            User localUser = getLocalUser();

            // Setze Nutzer als Gruppenadmin.
            newGroup.GroupAdmin = localUser.Id;

            // Führe Validierung durch.
            clearValidationErrors();
            newGroup.ClearValidationErrors();
            newGroup.ValidateAll();
            if (newGroup.HasValidationErrors())
            {
                // Melde Validierungsfehler.
                reportValidationErrors(newGroup.GetValidationErrors());
                // Breche ab.
                return false;
            }

            // Hashe das Passwort, das der Nutzer eingegeben hat.
            HashingHelper.HashingHelper hashingHelper = new HashingHelper.HashingHelper();
            newGroup.Password = hashingHelper.GenerateSHA256Hash(newGroup.Password);

            // Umwandeln des Objekts in Json.
            string jsonContent = jsonManager.ParseGroupToJson(newGroup);
            if (jsonContent == null)
            {
                Debug.WriteLine("Error during serialization from group object to json string. Could " +
                    "not create a group. Execution is aborted.");
                return false;
            }

            // Absetzen des Erstellungsrequests.
            string serverResponse = null;
            try
            {
                // Setzte Request zum Anlegen eines Kanals ab.
                serverResponse = await groupAPI.SendCreateGroupRequest(
                    localUser.ServerAccessToken,
                    jsonContent);
            }
            catch (APIException ex)
            {
                // Bilde ab auf ClientException.
                throw new ClientException(ex.ErrorCode, "Server rejected create group request.");
            }

            // Extrahiere Group Objekt aus Antwort des Servers.
            Group responseGroupObj = jsonManager.ParseGroupFromJson(serverResponse);
            if (responseGroupObj != null)
            {
                // Speichere Gruppe lokal ab.
                try
                {
                    // Prüfe, ob Gruppenadmin (lokaler Nutzer in diesem Fall) als User lokal bereits gespeichert ist.
                    if (!userController.IsUserLocallyStored(localUser.Id))
                    {
                        // Speichere lokalen Nutzer in den lokalen Nutzerdatensätzen für Gruppen ab.
                        // Wird benötigt, da sonst Foreign Key constraints verletzt werden beim Speichern der Gruppe.
                        userController.StoreUserLocally(localUser);
                    }

                    groupDBManager.StoreGroup(responseGroupObj);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Database Exception, message is: {0}.", ex.Message);
                    // Bilde ab auf ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Storing process of " +
                        "created group object in local DB has failed");
                }   
            }
            else
            {
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of server response has failed.");
            }

            return true;
        }
        #endregion RemoteGroupMethods

        #region LocalGroupMethods
        /// <summary>
        /// Liefert alle lokal gehaltenen Datensätze von Gruppen-Ressourcen zurück.
        /// </summary>
        /// <returns>Eine Liste von Group Instanzen.</returns>
        /// <exception cref="ClientException">Wirft eine Exception, wenn der Abruf aus der lokalen DB scheitert.</exception>
        public List<Group> GetAllGroups()
        {
            List<Group> groups = new List<Group>();
            try
            {
                groups = groupDBManager.GetAllGroups();
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetAllGroups: Error occurred in DB. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return groups;
        }
        #endregion LocalGroupMethods
    }
}
