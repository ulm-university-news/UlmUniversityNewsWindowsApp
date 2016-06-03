using DataHandlingLayer.API;
using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
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

        /// <summary>
        /// Führe die Suche nach Gruppen anhand der übergebenen Suchparametern aus. Hierfür
        /// wird ein Request an den Server abgesetzt und die ermittelten Gruppen in der Antwort
        /// überliefert.
        /// </summary>
        /// <param name="groupName">Der Suchbegriff bezüglich des Namens der Gruppe.</param>
        /// <param name="groupType">Suche kann über Angabe von Typ noch eingeschränkt werden. 
        ///     Kann auch null sein, dann wird der Parameter bei der Suche ignoriert.</param>
        /// <returns>Eine Liste von Group Objekten. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft ClientException wenn Suchanfrage fehlschlägt 
        ///     oder abgelehnt wird.</exception>
        public async Task<List<Group>> SearchGroupsAsync(string groupName, GroupType? groupType)
        {
            List<Group> retrievedGroups = null;

            string type = null;
            if (groupType.HasValue)
            {
                type = groupType.Value.ToString();
            }

            string serverResponse = null;
            try
            {
                // Setzte Request mit Suchparametern an den Server ab.
                serverResponse = await groupAPI.SendGetGroupsRequest(
                    getLocalUser().ServerAccessToken,
                    groupName,
                    type);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("SearchGroupsAsync: Request to server failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                retrievedGroups = jsonManager.ParseGroupListFromJson(serverResponse);
            }

            return retrievedGroups;
        }

        /// <summary>
        /// Ruft die Details zu der Gruppen-Ressource mit der angegebenen ID
        /// vom Server ab.
        /// </summary>
        /// <param name="id">Die Id der Gruppe, zu der Details abgerufen werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request zugelassen werden soll.</param>
        /// <returns>Liefert eine Instanz der Klasse Group zurück, oder null, falls zu der
        ///     Id keine Ressource auf dem Server gefunden wurde.</returns>
        /// <exception cref="ClientException">Wirft ClientException wenn Anfrage fehlschlägt 
        ///     oder abgelehnt wird.</exception>
        public async Task<Group> GetGroupAsync(int id, bool withCaching)
        {
            Group group = null;

            string serverResponse = null;
            try
            {
                // Setze Request an den Server ab.
                serverResponse = await groupAPI.SendGetGroupRequest(
                    getLocalUser().ServerAccessToken,
                    id,
                    withCaching);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("GetGroupAsync: Request to server failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                group = jsonManager.ParseGroupFromJson(serverResponse);
            }

            return group;
        }

        /// <summary>
        /// Trete der Gruppe bei, die durch die angegebene Id repräsentiert wird.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="password">Das Passwort der Gruppe, das für den Request verwendet wird.</param>
        /// <returns>Liefer true, wenn der Beitritt erfolgreich war, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Beitritt fehlschlägt
        ///     oder der Server diesen ablehnt.</exception>
        public async Task<bool> JoinGroupAsync(int groupId, string password)
        {
            // Erstelle JSON-Dokument für Passwortübergabe.
            string jsonContent = jsonManager.CreatePasswordResource(password);

            // Frage zunächst die Gruppen-Ressource vom Server ab.
            Group group = await GetGroupAsync(groupId, false);

            // Frage die Teilnehmer der Gruppe ab.
            List<User> participants = await GetParticipantsOfGroupAsync(groupId, false);

            // Setze Request zum Beitreten in die Gruppe ab.
            try
            {
                await groupAPI.SendJoinGroupRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    jsonContent);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("JoinGroupAsync: Failed to join group. Msg is: {0}.", ex.Message);

                if (ex.ErrorCode == ErrorCodes.GroupIncorrectPassword)
                {
                    // TODO - Validation error "password incorrect"

                    return false;
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            try
            {
                // Speichere die Teilnehmer und die Gruppe lokal ab.
                userController.StoreUsersLocally(participants);
                StoreGroupLocally(group);

                // Füge Teilnehmer der Gruppe hinzu.
                AddParticipantsToGroup(groupId, participants);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("JoinGroupAsync: Storing of data has failed. Msg is: {0}.", ex.Message);

                // Lösche Gruppe wieder. Teilnehmer-Referenzen werden durch On Delete Cascade 
                // automatisch gelöscht. Die Nutzer-Ressourcen selbst können gespeichert bleiben.
                DeleteGroupLocally(groupId);

                // Trage den Nutzer wieder von der Gruppe aus.
                await RemoveParticipantFromGroupAsync(groupId, getLocalUser().Id);

                return false;
            }
            
            return true;
        }

        /// <summary>
        /// Entfernen eines Teilnehmers aus einer Gruppe. Es wird ein Request an den 
        /// Server geschickt, um den Teilnehmer aus der Gruppe zu entfernen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <returns>Liefert true, wenn der Teilnehmer erfolgreich von der Gruppe entfernt wurde.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public async Task<bool> RemoveParticipantFromGroupAsync(int groupId, int participantId)
        {
            try
            {
                await groupAPI.SendLeaveGroupRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    participantId);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("RemoveParticipantFromGroupAsync: Group seems to be deleted.");
                    Debug.WriteLine("This means participant is already removed.");

                    ChangeActiveStatusOfParticipant(groupId, participantId, false);

                    return true;
                }

                Debug.WriteLine("RemoveParticipantFromGroupAsync: Request has failed or was rejected.");
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            ChangeActiveStatusOfParticipant(groupId, participantId, false);
            
            return true;
        }

        /// <summary>
        /// Ruft die Teilnehmer der Gruppe mit der angegebnen Id vom Server ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="withCaching">Gibt an, ob für diesen Request Caching zugelassen werden soll, oder nicht.</param>
        /// <returns>Liefert eine Liste von User Objekten zurück. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Anfrage fehlschlägt oder abgelehnt wurde.</exception>
        public async Task<List<User>> GetParticipantsOfGroupAsync(int groupId, bool withCaching)
        {
            List<User> participants = new List<User>();

            string serverResponse = null;
            try
            {
                // Setze Request an den Server ab.
                serverResponse = await groupAPI.SendGetParticipantsRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    withCaching);
            }
            catch (APIException ex)
            {
                // TODO - Behandlung Gruppe nicht gefunden.

                Debug.WriteLine("GetParticipantsOfGroup: Request to server failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                participants = jsonManager.ParseUserListFromJson(serverResponse);
            }

            return participants;
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

        /// <summary>
        /// Speichert die Daten der Gruppe in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="group">Die zu speichernde Gruppe.</param>
        /// <exception cref="ClientException">Wenn die Speicherung fehlschlägt.</exception>
        public void StoreGroupLocally(Group group)
        {
            try
            {
                groupDBManager.StoreGroup(group);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreGroupLocally: Failed to store the group.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Löscht die Gruppe mit der angegebenen Id aus der Datenbank.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die gelöscht werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschvorgang fehlschlägt.</exception>
        public void DeleteGroupLocally(int groupId)
        {
            try
            {
                groupDBManager.DeleteGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreGroupLocally: Failed to store the group.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Liefert die Teilnehmer der Gruppe mit der angegebenen Id aus den
        /// lokalen Datensätzen zurück.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <returns>Eine Liste von User Objekten. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<User> GetActiveParticipantsOfGroup(int groupId)
        {
            List<User> participants = null;
            try
            {
                participants = groupDBManager.GetParticipantsOfGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetParticipantsOfGroup: Error during retrieval. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return participants;
        }

        /// <summary>
        /// Füge die Nutzer lokal als Teilnehmer der Gruppe hinzu.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Nutzer als Teilnehmer
        ///     hinzugefügt werden sollen.</param>
        /// <param name="participants">Die Liste der Nutzer, die als Teilnehmer hinzu-
        ///     gefügt werden sollen.</param>
        ///     <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public void AddParticipantsToGroup(int groupId, List<User> participants)
        {
            if (!groupDBManager.IsGroupStored(groupId))
            {
                Debug.WriteLine("AddParticipantsToGroup: There is no group with id {0} in the local datasets.",
                    groupId);

                throw new ClientException(ErrorCodes.GroupNotFound, "Cannot continue without stored group.");
            }

            // Speichere als erstes die Teilnehmer lokal ab.
            // Die gerufene Methode speichert nur Teilnehmer, die lokal noch nicht gespeichert sind.
            userController.StoreUsersLocally(participants);

            // Füge die Teilnehmer der Gruppe hinzu.
            try
            {
                groupDBManager.AddParticipantsToGroup(groupId, participants);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("AddParticipantsToGroup: Adding participants failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Ändere den Active Status des Teilnehmers mit der angegebenen Id bezüglich der spezifizierten Gruppe.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <param name="active">Der neue Active Status.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Änderung fehlschlägt.</exception>
        public void ChangeActiveStatusOfParticipant(int groupId, int participantId, bool active)
        {
            try
            {
                if (groupDBManager.RetrieveActiveStatusOfParticipant(groupId, participantId).HasValue)
                {
                    groupDBManager.ChangeActiveStatusOfParticipant(groupId, participantId, active);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("ChangeActiveStatusOfParticipant: Changing status of participant failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalGroupMethods
    }
}
