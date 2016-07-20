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
            jsonParser = new JsonParsingManager();
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
            jsonParser = new JsonParsingManager();
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
            string jsonContent = jsonParser.ParseGroupToJson(newGroup);
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
            Group responseGroupObj = jsonParser.ParseGroupFromJson(serverResponse);
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

                    // Füge Teilnehmer der Gruppe hinzu.
                    AddParticipantToGroup(responseGroupObj.Id, getLocalUser());
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
                retrievedGroups = jsonParser.ParseGroupListFromJson(serverResponse);
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
                group = jsonParser.ParseGroupFromJson(serverResponse);
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
            HashingHelper.HashingHelper hashHelper = new HashingHelper.HashingHelper();
            string hash = hashHelper.GenerateSHA256Hash(password);

            // Erstelle JSON-Dokument für Passwortübergabe.
            string jsonContent = jsonParser.CreatePasswordResource(hash);

            // Frage zunächst die Gruppen-Ressource vom Server ab.
            Group group = await GetGroupAsync(groupId, false);

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
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Frage die Teilnehmer der Gruppe ab.
            List<User> participants = await GetParticipantsOfGroupAsync(groupId, false);

            try
            {
                // Speichere die Teilnehmer und die Gruppe lokal ab.
                userController.StoreUsersLocally(participants);
                StoreGroupLocally(group);

                // Füge Teilnehmer der Gruppe hinzu.
                AddParticipantsToGroup(groupId, participants);

                // TODO - check if this last call can be removed. User should already be added toghether with the other participants.
                // Trage Nutzer selbst als Teilnehmer ein.
                AddParticipantToGroup(groupId, getLocalUser());
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

            // Frage noch Konversationen-Daten ab.
            try
            {
                List<Conversation> conversations = await GetConversationsAsync(groupId, true, false);
                groupDBManager.BulkInsertConversations(groupId, conversations);

                foreach (Conversation conversation in conversations)
                {
                    StoreConversationMessages(groupId, conversation.Id, conversation.ConversationMessages);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("JoinGroupAsync: Failed to store the conversation data.");
                Debug.WriteLine("JoinGroupAsync: error msg is {0}.", ex.Message);
                // Werfe hier keinen Fehler an Aufrufer. Daten können später einfach nachgeladen werden.
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("JoinGroupAsync: Failed to retrieve the conversation data.");
                Debug.WriteLine("JoinGroupAsync: error msg is {0}.", ex.Message);
                // Werfe hier keinen Fehler an Aufrufer. Daten können später einfach nachgeladen werden.
            }

            // Frage noch die Abstimmungsdaten ab.
            try
            {
                List<Ballot> ballots = await GetBallotsAsync(groupId, true, false);
                Debug.WriteLine("JoinGroupAsync: Ballots is: {0}.", ballots);
                StoreBallots(groupId, ballots);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("JoinGroupAsync: Failed to retrieve or store the ballot data.");
                Debug.WriteLine("JoinGroupAsync: error msg is {0} and error code is {1}.", ex.Message, ex.ErrorCode);
                // Werfe hier keinen Fehler an Aufrufer. Daten können später einfach nachgeladen werden.
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
        /// Der lokale Nutzer verlässt die Gruppe mit der angegebenen Id.
        /// Schickt einen Request an den Server, um den Teilnehmer von der Gruppe zu entfernen.
        /// Behandelt auch den Fall, dass der lokale Nutzer Administrator dieser Gruppe ist. In diesem Fall
        /// wird zunächst ein neuer Administrator bestimmt und die Gruppe aktualisiert.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, aus der der lokale Nutzer austritt.</param>
        /// <exception cref="ClientException">Wirft Exception, wenn der Austritt fehlschlägt, oder der
        ///     entsprechende Request vom Server abgelehnt wurde.</exception>
        public async Task LeaveGroupAsync(int groupId)
        {
            User localUser = getLocalUser();

            // Hole die betroffene Gruppe aus dem lokalen Speicher.
            Group affectedGroup = GetGroup(groupId);

            // Sonderfallbehandlung: Nutzer ist Administrator der Gruppe.
            if (affectedGroup.GroupAdmin == localUser.Id)
            {
                // Frage in diesem Fall zunächst alle Teilnehmer der Gruppe vom Server ab, um sicher den aktuellsten Datensatz zu haben.
                List<User> participants = await GetParticipantsOfGroupAsync(groupId, false);

                // Sortiere alle "inaktiven" Nutzer aus.
                for (int i=0; i < participants.Count; i++)
                {
                    if (!participants[i].Active)
                    {
                        participants.RemoveAt(i);
                    }
                }

                if (participants.Count <= 1)
                {
                    // Sonderfall: Es gibt nur einen Teilnehmer der Gruppe, den lokalen Nutzer.
                    // Nutzer kann Gruppe nicht verlassen, da diese sonst leer wäre.
                    throw new ClientException(ErrorCodes.GroupAdminNotAllowedToExit, "Administrator cannot leave group.");
                }

                // Ansonsten: Wähle zufälligen Nutzer und übertrage die Administrationsrechte.
                Random rnd = new Random();
                User newAdmin = localUser;
                while (newAdmin.Id == localUser.Id)
                {
                    int rndIndex = rnd.Next(0, participants.Count);
                    newAdmin = participants[rndIndex];
                }
                Group newGroup = new Group()
                {
                    Id = affectedGroup.Id,
                    Name = affectedGroup.Name,
                    Description = affectedGroup.Description,
                    CreationDate = affectedGroup.CreationDate,
                    ModificationDate = affectedGroup.ModificationDate,
                    GroupNotificationSetting = affectedGroup.GroupNotificationSetting,
                    Deleted = affectedGroup.Deleted,
                    NumberOfUnreadMessages = affectedGroup.NumberOfUnreadMessages,
                    Participants = affectedGroup.Participants,
                    Password = affectedGroup.Password,
                    Term = affectedGroup.Term,
                    Type = affectedGroup.Type
                };
                newGroup.GroupAdmin = newAdmin.Id;

                // Führe Aktualisierung aus.
                bool successful = await UpdateGroupAsync(affectedGroup, newGroup, true);
                if (!successful)
                {
                    // Konnte Rechte nicht übertragen.
                    throw new ClientException(ErrorCodes.GroupAdminRightsTransferHasFailed, "Couldn't transfer admin rights.");
                }                
            }

            // Aus Gruppe austreten.
            try
            {
                // Sende Request an den Server, um lokalen Nutzer von der Gruppe zu entfernen.
                await groupAPI.SendLeaveGroupRequest(
                    localUser.ServerAccessToken,
                    groupId,
                    localUser.Id);

                Debug.WriteLine("LeaveGroupAsync: Successfully left group with id {0}.", groupId);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    // Kann Gruppe einfach entfernen.
                    DeleteGroupLocally(groupId);
                    
                    return;
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Entferne die Gruppe aus den lokalen Datensätzen. Zugehörige Daten werden kaskadierend gelöscht.
            DeleteGroupLocally(groupId);
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
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("GetParticipantsOfGroup: Group with id {0} seems to be deleted.", groupId);
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                Debug.WriteLine("GetParticipantsOfGroup: Request to server failed.");
                // Abbilden auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                participants = jsonParser.ParseUserListFromJson(serverResponse);
            }

            return participants;
        }

        /// <summary>
        /// Führt Aktualisierung der Gruppe aus. Es wird ermittelt welche Properties eine Aktualisierung 
        /// erhalten haben. Die Aktualisierungen werden an den Server übermittelt, der die Aktualisierung auf
        /// dem Serverdatensatz ausführt und die Teilnehmer über die Änderung informiert. Aktualisiert lokal jedoch
        /// nicht die Benachrichtigungseinstellungen für die Gruppe. Hierfür gibt es eine separate Funktion.
        /// </summary>
        /// <param name="oldGroup">Der Datensatz der Gruppe vor der Aktualisierung.</param>
        /// <param name="newGroup">Der Datensatz mit aktualisierten Daten.</param>
        /// <param name="ignorePassword">Gibt an, ob das Passwort Property bei der Aktualisierung ignoriert werden soll.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich war, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Fehler während des Aktualisierungsvorgangs auftritt.</exception>
        public async Task<bool> UpdateGroupAsync(Group oldGroup, Group newGroup, bool ignorePassword)
        {
            if (oldGroup == null || newGroup == null)
                return false;

            User localUser = getLocalUser();
            if (localUser == null)
                return false;

            // Gruppen-Admin sollte nicht 0 sein, falls das der Fall ist, setzte ihn auf den alten Wert.
            if (newGroup.GroupAdmin == 0)
            {
                newGroup.GroupAdmin = oldGroup.GroupAdmin;
            }

            // Führe Validierung aus. Wenn Validierung fehlschlägt, kann abgebrochen werden.
            clearValidationErrors();
            newGroup.ClearValidationErrors();
            newGroup.ValidateAll();

            if (newGroup.HasValidationErrors() && !ignorePassword)
            {
                reportValidationErrors(newGroup.GetValidationErrors());
                return false;
            }
            else if (ignorePassword)
            {
                // Prüfe alle Properties bis auf Passwort separat.
                if (newGroup.HasValidationError("Name") ||
                    newGroup.HasValidationError("Term") || 
                    newGroup.HasValidationError("Description"))
                {
                    Dictionary<string, string> validationErrors = newGroup.GetValidationErrors();
                    if (validationErrors.ContainsKey("Password"))
                    {
                        validationErrors.Remove("Password");
                    }
                    reportValidationErrors(validationErrors);
                    return false;
                }
            }

            // Erstelle ein Objekt für die Aktualisierung, welches die Daten enthält, die aktualisiert werden müssen.
            Group updatableGroupObj = prepareUpdatableGroupInstance(oldGroup, newGroup);

            if (updatableGroupObj == null)
                return false;   // Keine Aktualisierung notwendig.

            // Hash Passwort bei Änderung.
            if (updatableGroupObj.Password != null)
            {
                HashingHelper.HashingHelper hashHelper = new HashingHelper.HashingHelper();
                string hashedPassword = hashHelper.GenerateSHA256Hash(updatableGroupObj.Password);
                updatableGroupObj.Password = hashedPassword;
            }

            // Erstelle Json-Dokument für die Aktualisierung.
            string jsonContent = base.jsonParser.ParseGroupToJson(updatableGroupObj);
            if (jsonContent == null)
            {
                Debug.WriteLine("UpdateGroupAsync: Group object could not be translated to a json document.");
                return false;
            }

            // Server Request.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendUpdateGroupRequest(
                    getLocalUser().ServerAccessToken,
                    oldGroup.Id,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("UpdateGroupAsync: Group seems to be deleted from the server.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(oldGroup.Id);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Führe lokale Aktualisierung des Datensatzes aus.
            try
            {
                Group updatedGroup = base.jsonParser.ParseGroupFromJson(serverResponse);
                if (updatedGroup == null)
                {
                    throw new ClientException(ErrorCodes.JsonParserError, "Couldn't parse server response.");
                }

                // Notification settings bleiben unverändert.
                // updatedGroup.GroupNotificationSetting = oldGroup.GroupNotificationSetting;

                // Speichere neuen Datensatz ab.
                groupDBManager.UpdateGroup(updatedGroup, false);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("UpdateGroupAsync: Failed to store updated resource.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Bereitet ein Objekt vom Typ Group vor, welches alle Properties enthält, die sich geändert haben.
        /// Die Methode bekommt eine alte Version eines Group Objekts und eine neue Version und ermittelt 
        /// dann die Properties, die eine Aktualisierung erhalten haben und schreibt diese in eine neue Group
        /// Instanz. Die von der Methode zurückgelieferte Group Instanz kann dann direkt für die Aktualisierung auf
        /// dem Server verwendet werden. Achtung: Hat sich überhaupt keine Property geändert, so gibt die Methode null zurück.
        /// </summary>
        /// <param name="oldGroup">Das Group Objekt vor der Aktualisierung.</param>
        /// <param name="newGroup">Das Group Objekt mit den aktuellen Werten.</param>
        /// <returns>Ein Objekt der Klasse Group, bei dem die Properties, die sich geändert haben, mit den
        ///     aktualisierten Werten gefüllt sind.</returns>
        private Group prepareUpdatableGroupInstance(Group oldGroup, Group newGroup)
        {
            bool hasChanged = false;
            Group updatedGroup = new Group();

            // Vergleiche die Properties.
            if (oldGroup.Name != newGroup.Name)
            {
                hasChanged = true;
                updatedGroup.Name = newGroup.Name;
            }

            if (oldGroup.Description != newGroup.Description)
            {
                hasChanged = true;
                updatedGroup.Description = newGroup.Description;
            }

            // Aktualisiere Passwort, wenn es in der neuen Instanz gesetzt ist.
            if (newGroup.Password != null)
            {
                hasChanged = true;
                updatedGroup.Password = newGroup.Password;
            }

            if (oldGroup.Term != newGroup.Term)
            {
                hasChanged = true;
                updatedGroup.Term = newGroup.Term;
            }

            if (oldGroup.GroupAdmin != newGroup.GroupAdmin)
            {
                hasChanged = true;
                updatedGroup.GroupAdmin = newGroup.GroupAdmin;
            }

            // Prüfe, ob sich überhaupt ein Property geändert hat.
            if (!hasChanged)
            {
                Debug.WriteLine("prepareUpdatableGroupInstance: No property of group has been updated. Method will return null.");
                updatedGroup = null;
            }

            return updatedGroup;
        }

        /// <summary>
        /// Snychronisiert die lokalen Teilnehmerdaten der Gruppe mit der angegebenen Id mit den
        /// Daten vom Server. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Teilnehmerdaten aktualisiert werden sollen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlschlägt.</exception>
        public async Task SynchronizeGroupParticipantsAsync(int groupId)
        {
            Debug.WriteLine("SynchronizeGroupParticipantsAsync: Start synchronisation.");
            Stopwatch sw = Stopwatch.StartNew();

            // Frage zunächst die neuesten Teilnehmer-Informationen vom Server ab.
            List<User> participants = await GetParticipantsOfGroupAsync(groupId, false);

            // Synchronisiere die lokalen Nutzerdatensätze.
            userController.AddOrUpdateUsers(participants);

            Dictionary<int, User> participantsDirectory = GetParticipantsLookupDirectory(groupId);
            // Aktualisiere die Teilnehmerdaten bezüglich der lokalen Gruppe.
            foreach (User participant in participants)
            {
                
                if (!participantsDirectory.ContainsKey(participant.Id) && participant.Active)
                {
                    // Füge den Teilnehmer der Gruppe hinzu.
                    AddParticipantToGroup(groupId, participant);
                    Debug.WriteLine("SynchronizeGroupParticipantsAsync: Need to add participant with user id {0} " + 
                        "to the group with id {1}.", participant.Id, groupId);
                }
                else
                {
                    User currentParticipant = participantsDirectory[participant.Id];
                    if (!currentParticipant.Active && participant.Active)
                    {
                        ChangeActiveStatusOfParticipant(groupId, participant.Id, true);
                        Debug.WriteLine("SynchronizeGroupParticipantsAsync: Need to set participant with user id {0} " + 
                            "as an active participant again for group with id {1}.", participant.Id, groupId);
                    }

                    if (currentParticipant.Active && !participant.Active)
                    {
                        ChangeActiveStatusOfParticipant(groupId, participant.Id, false);
                        Debug.WriteLine("SynchronizeGroupParticipantsAsync: Need to set participant with user id {0} " + 
                            "inactive in group with id {1}.", participant.Id, groupId);
                    }
                }            
            }

            sw.Stop();
            Debug.WriteLine("SynchronizeGroupParticipantsAsync: Finished. Required time: {0}.", sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Lösche die Gruppe aus dem System. Setzt einen Request zum Löschen der Gruppe an den Server ab.
        /// Löscht die Gruppe aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die gelöscht werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschung fehlschlägt.</exception>
        public async Task DeleteGroupAsync(int groupId)
        {
            // Setze Request zum Löschen der Gruppe an den Server ab.
            try
            {
                await groupAPI.SendDeleteGroupRequest(
                    getLocalUser().ServerAccessToken,
                    groupId);

                Debug.WriteLine("DeleteGroupAsync: Successfully deleted group on the server.");
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    // Gruppe wohl schon gelöscht.
                    DeleteGroupLocally(groupId);
                    return;
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Lösche die Gruppe lokal. Löscht automatisch alle mit der Gruppe verknüpften Daten mit.
            DeleteGroupLocally(groupId);
        }
        #endregion RemoteGroupMethods

        #region RemoteConversationMethods
        /// <summary>
        /// Erzeugt eine neue Konversation. Es wird ein Request an den Server abgesetzt, um die 
        /// Konversation auf dem Server anzulegen. Ist das Anlegen erfolgreich, wird auch lokal
        /// eine Kopie der Konversation angelegt.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, in der eine neue Konversation angelegt werden soll.</param>
        /// <param name="newConversation">Das Objekt mit den Daten der neuen Konversation.</param>
        /// <returns>Liefert true, wenn das Anlegen erfolgreich war. Liefert false, wenn das Anlegen aufgrund
        ///     fehlender Daten oder sonstigen Validierungsfehlern nicht ausgeführt werden konnte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn das Anlegen fehlschlägt, oder 
        ///     vom Server abgelehnt wurde.</exception>
        public async Task<bool> CreateConversationAsync(int groupId, Conversation newConversation)
        {
            if (newConversation == null)
                return false;

            // Führe Validierung der Daten durch.
            clearValidationErrors();
            newConversation.ClearValidationErrors();
            newConversation.ValidateAll();
            if (newConversation.HasValidationErrors())
            {
                reportValidationErrors(newConversation.GetValidationErrors());
                return false;
            }

            // Parse Konversation zu JSON.
            string jsonContent = jsonParser.ParseConversationToJson(newConversation);
            if (jsonContent == null)
            {
                Debug.WriteLine("CreateConversationAsync: Failed to create a json object.");
                return false;
            }

            // Setze Request an den Server ab.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendCreateConversationRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    jsonContent);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("CreateConversationAsync: Request failed. Error code is {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("CreateConversationAsync: Request failed due to group not found.");
                    MarkGroupAsDeleted(groupId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                // Extrahiere Conversation aus der Serverantwort.
                Conversation createdConv = jsonParser.ParseConversationFromJson(serverResponse);

                if (createdConv != null)
                {
                    // Speichere die Konversation noch lokal ab.
                    bool successful = StoreConversation(groupId, createdConv);
                    if (!successful)
                        await UpdateUserDataAndStoreConversationAsync(groupId, createdConv);
                }
            }

            return true;
        }

        /// <summary>
        /// Führe eine Aktualisierung der Konversationsdaten durch. Es wird ein Request an den Server 
        /// abgesetzt, um die Daten der Konversation zu aktualisieren. Ist dieser erfolgreich, so werden die 
        /// Daten auch lokal aktualisiert.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="oldConversation">Das Conversation Objekt vor der Aktualisierung.</param>
        /// <param name="newConversation">Das Conversation Objekt nach der Aktualisierung.</param>
        /// <returns>Liefer true, wenn die Aktualisierung erfolgreich war. Liefert false, wenn die Konversation 
        ///     aufgrund einer fehlenden Datenvalidierung nicht aktualisiert werden konnte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlschlägt oder 
        ///     vom Server abgelehnt wurde.</exception>
        public async Task<bool> UpdateConversationAsync(int groupId, Conversation oldConversation, Conversation newConversation)
        {
            if (oldConversation == null || newConversation == null)
                return false;

            // Führe Validierung der neuen Daten durch.
            clearValidationErrors();
            newConversation.ClearValidationErrors();
            newConversation.ValidateAll();
            if (newConversation.HasValidationErrors())
            {
                reportValidationErrors(newConversation.GetValidationErrors());
                return false;
            }

            // Erstelle ein Objekt für die Aktualisierung, welches die Daten enthält, die aktualisiert werden müssen.
            Conversation updatableConversationObj = prepareUpdatableConversationInstance(oldConversation, newConversation);

            if (updatableConversationObj == null)
            {
                // Keine Aktualisierung notwendig.
                return false;
            }

            // Erstelle JSON Dokument für die Aktualisierung.
            string jsonContent = jsonParser.ParseConversationToJson(updatableConversationObj);
            if (jsonContent == null)
            {
                Debug.WriteLine("UpdateConversationAsync: Failed to create json document.");
                return false;
            }

            // Setze Request an den Server ab.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendUpdateConversationRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    oldConversation.Id,
                    jsonContent);
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("UpdateConversationAsync: Group not found on server. Group probably deleted.");
                    // Behandlung von Not Found. Gruppe wahrscheinlich gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                // Bilde ab auf ClientException.
                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                // Parse Server Antwort.
                Conversation updatedConv = jsonParser.ParseConversationFromJson(serverResponse);

                if (updatedConv != null)
                {
                    // Aktualisiere lokalen Datensatz.
                    bool successful = UpdateConversation(groupId, updatedConv);
                    if (!successful)
                    {
                        Debug.WriteLine("UpdateConversationAsync: Failed to update group locally. Trying to fix it by synchronizing participants info.");
                        await SynchronizeGroupParticipantsAsync(groupId);

                        // Versuche es erneut. Diesmal ohne Fehlerbehandlung.
                        successful = UpdateConversation(groupId, updatedConv);
                        if (!successful)
                            throw new ClientException(ErrorCodes.LocalDatabaseException, "Couldn't update conversation.");
                    }
                }

            }

            return true;
        }

        /// <summary>
        /// Bereitet ein Objekt vom Typ Conversation vor, welches alle Properties enthält, die sich geändert haben.
        /// Die Methode bekommt eine alte Version eines Conversation Objekts und eine neue Version und ermittelt 
        /// dann die Properties, die eine Aktualisierung erhalten haben und schreibt diese in eine neue Conversation
        /// Instanz. Die von der Methode zurückgelieferte Conversation Instanz kann dann direkt für die Aktualisierung auf
        /// dem Server verwendet werden. Achtung: Hat sich überhaupt keine Property geändert, so gibt die Methode null zurück.        
        /// </summary>
        /// <param name="oldConversation">Das Conversation Objekt vor der Aktualisierung.</param>
        /// <param name="newConversation">Das Channel Objekt mit den aktuellen Werten.</param>
        /// <returns>Ein Objekt der Klasse Conversation, bei dem die Properties, die sich geändert haben, mit den
        ///     aktualisierten Werten gefüllt sind.</returns>
        private Conversation prepareUpdatableConversationInstance(Conversation oldConversation, Conversation newConversation)
        {
            bool hasChanged = false;
            Conversation updatedConversation = new Conversation();

            // Vergleiche Properties.
            if (oldConversation.Title != newConversation.Title)
            {
                hasChanged = true;
                updatedConversation.Title = newConversation.Title;
            }

            if (newConversation.IsClosed.HasValue && 
                oldConversation.IsClosed != newConversation.IsClosed)
            {
                hasChanged = true;
                updatedConversation.IsClosed = newConversation.IsClosed;
            }

            // Prüfe, ob sich überhaupt eine Property geändert hat.
            if (!hasChanged)
            {
                Debug.WriteLine("No Property of conversation has been updated. Method will return null.");
                updatedConversation = null;
            }

            return updatedConversation;
        }

        /// <summary>
        /// Fragt zunächst für die Speicherung relevante Daten vom Server ab, z.B. die aktuellen
        /// Teilnehmer, so dass die aktuellesten Daten vorhanden sind. Speichert anschließend die 
        /// Konversation lokal ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Konversation gespeichert wird.</param>
        /// <param name="conversation">Die Daten der Konversation in Form eines Objekts des Typs Conversation.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf der Informationen oder Speicherung
        ///     fehlschlägt.</exception>
        public async Task UpdateUserDataAndStoreConversationAsync(int groupId, Conversation conversation)
        {
            // Frage zunächst die Teilnehmer der Gruppe ab.
            List<User> participants = await GetParticipantsOfGroupAsync(groupId, false);

            // Speichere die fehlenden Nutzer ab.
            userController.StoreUsersLocally(participants);

            // Speichere nun Konversation lokal ab.
            bool successful = StoreConversation(groupId, conversation);
            if (!successful)
            {
                Debug.WriteLine("UpdateUserDataAndStoreConversationAsync: Still failed to store conversation.");
                Debug.WriteLine("UpdateUserDataAndStoreConversationAsync: Trying to store conversation with dummy user admin.");
                conversation.AdminId = 0;
                successful = StoreConversation(groupId, conversation);
                if (!successful)
                    throw new ClientException(ErrorCodes.ConversationStorageFailedDueToMissingAdmin, "Couldn't store conversation due "
                        + "to missing conversation administrator.");
            }

        }

        /// <summary>
        /// Ruft die Konversationsnachrichten der angegebenen Konversation vom Server ab. Der Abruf
        /// kann durch die Nachrichtennummer eingeschränkt werden. Es werden nur die Nachrichten abgerufen,
        /// die eine höhere Nachrichtennummer haben, als die die angegeben wird.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten abgerufen werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, ab der die Nachrichten abgerufen werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching für den Request erlaubt sein soll.</param>
        /// <returns>Eine Liste von ConversationMessage Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public async Task<List<ConversationMessage>> GetConversationMessagesAsync(int groupId, int conversationId, int messageNr, bool withCaching)
        {
            List<ConversationMessage> conversationMessages = null;

            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendGetConversationMessagesRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    conversationId,
                    messageNr,
                    withCaching);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("GetConversationMessages: Request to server failed.");

                // TODO case Conversation not found, group not found.

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parsen der Serverantwort.
            if (serverResponse != null)
            {
                conversationMessages = base.jsonParser.ParseConversationMessageListFromJson(serverResponse);
            }

            return conversationMessages;
        }

        /// <summary>
        /// Ruft die Konversationen vom Server ab, die der angegebenen Gruppe zugeordnet sind.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversationen abgerufen werden sollen.</param>
        /// <param name="includeSubresources">Gibt an, ob Subressourcen zu den Konversationen (wie z.B. ConversationMessages)
        ///     ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching für diesen Request erlaubt sein soll.</param>
        /// <returns>Eine Liste von Conversation Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Abruf fehlschlägt,
        ///     oder der Request vom Server abgelehnt wird.</exception>
        public async Task<List<Conversation>> GetConversationsAsync(int groupId, bool includeSubresources, bool withCaching)
        {
            List<Conversation> conversations = null;

            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendGetConversationsRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    includeSubresources,
                    withCaching
                    );
            }
            catch (APIException ex)
            {
                Debug.WriteLine("GetConversationsAsync: Request to server failed.");

                // TODO case Conversation not found, group not found.

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parse Liste aus Server Antwort.
            if (serverResponse != null)
            {
                conversations = jsonParser.ParseConversationListFromJson(serverResponse);
            }

            return conversations;
        }

        /// <summary>
        /// Synchronisiert die lokalen Konversations-Ressourcen mit denen, die auf
        /// dem Server gespeichert sind. So kann man die Datensätze zwischen Client und Server
        /// wieder auf einen Stand bringen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Datensätze synchronisiert werden sollen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlschlägt.</exception>
        public async Task SynchronizeConversationsWithServerAsync(int groupId)
        {
            Debug.WriteLine("SynchronizeConversationsWithServerAsync: Start synchronisation.");
            // Synchronisiere die lokalen Teilnehmerdaten, so dass es dort keine Probleme gibt.
            await SynchronizeGroupParticipantsAsync(groupId);

            // Frage zunächst die neuesten Konversationsdaten vom Server ab.
            List<Conversation> referenceList = await GetConversationsAsync(groupId, true, false);
            
            // Frage lokale Datensätze ab.
            List<Conversation> currentList = GetConversations(groupId);

            // Füge neue Konversationen hinzu, oder aktualisiere die aktuellen falls notwendig.
            List<Conversation> updatableConversations = new List<Conversation>();
            List<Conversation> newConversations = new List<Conversation>();
            foreach (Conversation refConversation in referenceList)
            {
                bool isStored = false;

                for (int i = 0; i < currentList.Count; i++)
                {
                    if (currentList[i].Id == refConversation.Id)
                    {
                        isStored = true;
                        // Aktualisiere.
                        updatableConversations.Add(refConversation);
                        break;
                    }
                }

                if (!isStored)
                {
                    // Hinzufügen.
                    newConversations.Add(refConversation);
                    Debug.WriteLine("SynchronizeConversationsWithServerAsync: Need to add conversation with id {0}.", refConversation.Id);                  
                }
            }

            // Markiere Konversationen als gelöscht, wenn sie auf dem Server nicht mehr vorhanden sind.
            foreach (Conversation currentConv in currentList)
            {
                bool isStored = false;

                foreach (Conversation referenceConv in referenceList)
                {
                    if (referenceConv.Id == currentConv.Id)
                    {
                        isStored = true;
                        break;
                    }
                }

                if (!isStored)
                {
                    // Markiere als gelöscht (setze auf closed).
                    currentConv.IsClosed = true;
                    // Führe Markierung durch Aktualisierung aus.
                    updatableConversations.Add(currentConv);
                    Debug.WriteLine("SynchronizeConversationsWithServerAsync: Conversation with id {0} seems to be deleted on the server.", currentConv.Id);
                }
            }

            try
            {
                groupDBManager.BulkInsertConversations(groupId, newConversations);
                groupDBManager.UpdateConversations(updatableConversations);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("SynchronizeConversationsWithServerAsync: Failed to update or insert conversations.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            // Führe noch eine Aktualisierung der Nachrichten aus.
            foreach(Conversation conv in updatableConversations)
            {
                if (conv.ConversationMessages != null && conv.ConversationMessages.Count > 0)
                {
                    // Führe Methode aus zur Aktualisierung der Nachrichten aus.
                    try
                    {
                        // Die Methode fügt nur die Nachrichten hinzu, die noch nicht lokal gespeichert sind.
                        StoreConversationMessages(groupId, conv.Id, conv.ConversationMessages);
                    } catch(ClientException ex)
                    {
                        Debug.WriteLine("SynchronizeConversationsWithServerAsync: Failed to update messages " + 
                            "for conversation with id {0}. Msg is {1}.", conv.Id, ex.Message);
                        // Werfe keinen Fehler. Nachrichten können jederzeit nachgeladen werden.
                    }
                }
            }
            foreach (Conversation conv in newConversations)
            {
                if (conv.ConversationMessages != null && conv.ConversationMessages.Count > 0)
                {
                    // Führe Methode aus zur Aktualisierung der Nachrichten aus.
                    try
                    {
                        // Die Methode fügt nur die Nachrichten hinzu, die noch nicht lokal gespeichert sind.
                        StoreConversationMessages(groupId, conv.Id, conv.ConversationMessages);
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("SynchronizeConversationsWithServerAsync: Failed to update messages " +
                            "for conversation with id {0}. Msg is {1}.", conv.Id, ex.Message);
                        // Werfe keinen Fehler. Nachrichten können jederzeit nachgeladen werden.
                    }
                }
            }

            Debug.WriteLine("SynchronizeConversationsWithServerAsync. Finished.");
        }

        /// <summary>
        /// Prüft, ob die Daten der Gruppe noch aktuell sind. Vergleicht die Daten mit 
        /// den vom Server abgefragten aktuellsten Daten. Wenn Änderungen bestehen, dann werden
        /// die lokalen Datensätze aktualisiert. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe für die die Synchronisation durchgeführt werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlgeschlagen ist.</exception>
        public async Task SynchronizeGroupDetailsWithServerAsync(int groupId)
        {
            Debug.WriteLine("SynchronizeGroupDetailsWithServerAsync: Start synchronisation.");
            // Synchronisiere die lokalen Teilnehmerdaten.
            await SynchronizeGroupParticipantsAsync(groupId);

            // Hole neuste Gruppendaten vom Server.
            Group referenceGroup = await GetGroupAsync(groupId, false);

            // Hole Gruppendaten aus lokalen Datenstätzen.
            Group localGroup = GetGroup(groupId);

            if (localGroup.ModificationDate.CompareTo(referenceGroup.ModificationDate) < 0)
            {
                Debug.WriteLine("SynchronizeGroupDetailsWithServerAsync: Need to update local group data for group with id {0}.", groupId);

                try
                {
                    groupDBManager.UpdateGroup(referenceGroup, false);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Failed to update the group with id {0}.", groupId);
                    throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
                }
            }
            else
            {
                Debug.WriteLine("SynchronizeGroupDetailsWithServerAsync: No need to update the group.");
            }

            Debug.WriteLine("SynchronizeGroupDetailsWithServerAsync: Finished synchronisation.");
        }

        /// <summary>
        /// Erstelle eine neue Konversationsnachricht. Sendet einen Request an den Server,
        /// um die Nachricht anzulegen. Der Server verteilt die Nachricht dann an alle Teilnehmer.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, in der die Nachricht verschickt werden soll.</param>
        /// <param name="content">Der Inhalt der Nachricht.</param>
        /// <param name="messagePriority">Die Priorität der Nachricht.</param>
        /// <returns>Liefert true, wenn die Nachricht angelegt wurde. Liefert false, wenn die Validierung der Nachrichtendaten
        ///     fehlschlägt.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Request fehlschlägt oder vom Server abgelehnt 
        ///     wird, oder die Speicherung der Nachricht fehlschlägt.</exception>
        public async Task<bool> SendConversationMessageAsync(int groupId, int conversationId, string content, Priority messagePriority)
        {
            User localUser = getLocalUser();

            // Erstelle zunächst Conversation-Objekt.
            ConversationMessage message = new ConversationMessage()
            {
                Text = content,
                MessagePriority = messagePriority,
                AuthorId = localUser.Id
            };

            // Führe Validierung aus. Breche ab bei Validierungsfehler.
            clearValidationErrors();
            message.ClearValidationErrors();
            message.ValidateAll();
            if (message.HasValidationErrors())
            {
                reportValidationErrors(message.GetValidationErrors());
                return false;
            }

            // Parse Nachricht zu Json.
            string jsonContent = base.jsonParser.ParseConversationMessageToJson(message);

            // Setze Request an den Server ab.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendCreateConversationMessageRequest(
                    localUser.ServerAccessToken,
                    groupId,
                    conversationId,
                    jsonContent);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("SendConversationMessageAsync: Request to create conversation message failed.");

                // TODO: Fälle: GroupNotFound, ConversationNotFound, Conversation Locked.
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("SendConversationMessageAsync: Group seems to be deleted on the server.");
                    // Markiere die Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                if (ex.ErrorCode == ErrorCodes.ConversationNotFound)
                {
                    Debug.WriteLine("SendConversationMessageAsync: Conversation seems to be deleted on the server.");
                    // Markiere Conversation lokal als geschlossen.
                    MarkConversationAsClosed(conversationId);
                }

                // Wenn Konversation geschlossen ist, erhält man User Forbidden.
                if (ex.ErrorCode == ErrorCodes.UserForbidden)
                {
                    Debug.WriteLine("SendConversationMessageAsync: Cannot send message into a conversation that is closed.");
                    // Markiere Conversation als geschlossen.
                    MarkConversationAsClosed(conversationId);
                    // Wandle Status Code um.
                    ex.ErrorCode = ErrorCodes.ConversationIsClosed;
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parse Serverantwort.
            if (serverResponse != null)
            {
                ConversationMessage convMsg = base.jsonParser.ParseConversationMessageFromJson(serverResponse);

                int highestMessageNr = GetHighestMessageNumberOfConversation(conversationId);
                if (highestMessageNr + 1 != convMsg.MessageNumber)
                {
                    Debug.WriteLine("SendConversationMessageAsync: Seems there are more new messages on the server.");
                    List<ConversationMessage> conversationMessages = await GetConversationMessagesAsync(groupId, conversationId, highestMessageNr, false);
                    // Speichere die Nachrichten ab.
                    StoreConversationMessages(groupId, conversationId, conversationMessages);
                }
                else
                {
                    // Speichere die Nachricht ab.
                    StoreConversationMessage(convMsg);
                }
            }

            return true;
        }

        /// <summary>
        /// Führt eine Synchronisation der Konversationsnachrichten mit dem Server durch.
        /// Schickt einen Request an den Server, um die neuesten Nachrichten abzurufen und speichert diese lokal ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, für die die Synchronisation ausgeführt werden soll.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request erlaubt sein soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Fehler bei der Synchronisation auftritt.</exception>
        public async Task SynchronizeConversationMessagesWithServer(int groupId, int conversationId, bool withCaching)
        {
            int highestMessageNr = GetHighestMessageNumberOfConversation(conversationId);
            List<ConversationMessage> messages = await GetConversationMessagesAsync(
                groupId,
                conversationId,
                highestMessageNr,
                withCaching);

            if (messages != null && messages.Count > 0)
            {
                await Task.Run(() => StoreConversationMessages(
                    groupId,
                    conversationId,
                    messages));
            }
        }

        /// <summary>
        /// Löst fehlende Referenzen bezüglich des Autors bei Konversationsnachrichten für die
        /// angegebene Konversation auf. Fragt die entsprechenden Informationen vom Server ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <returns>Liefert true, wenn fehlende Referenzen aufgelöst werden konnten, ansonsten false.</returns>
        public async Task<bool> ResolveMissingAuthorReferencesAsync(int groupId, int conversationId)
        {
            bool resolvedCorrectly = false;

            List<ConversationMessage> messageList;
            try
            {
                messageList = groupDBManager.GetConversationMessagesWithUnresolvedAuthors(conversationId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("ResolveMissingAuthorReferencesAsync: Failed to retrieve conversation messages.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            if (messageList != null && messageList.Count > 0)
            {
                // Erstens, bringe Daten zu Gruppenmitgliedern auf den neuesten Stand.
                await SynchronizeGroupParticipantsAsync(groupId);

                // Zweitens, frage die Konversationsnachrichten für die Konversation vom Server ab.
                List<ConversationMessage> referenceList = await GetConversationMessagesAsync(groupId, conversationId, 0, false);

                // Drittens, Referenzen aktualisieren.
                foreach (ConversationMessage message in messageList)
                {
                    ConversationMessage referenceMessage = referenceList.Find(x => x.Id == message.Id);

                    // Führe Aktualisierung durch.
                    try
                    {
                        groupDBManager.UpdateAuthorReferenceOfConversationMessage(message.Id, referenceMessage.AuthorId);
                    }
                    catch (DatabaseException ex)
                    {
                        Debug.WriteLine("ResolveMissingAuthorReferencesAsync: Failed to update author reference.");
                        throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
                    }
                }

                resolvedCorrectly = true;
            }

            return resolvedCorrectly;
        }
        #endregion RemoteConversationMethods

        #region RemoteBallotMethods
        /// <summary>
        /// Ruft die Abstimmungen vom Server ab, die der Gruppe mit der angegebenen Id zugeordnet sind. Die Abstimmungen
        /// können einschließlich zugehöriger Subressourcen abgefragt werden (Options und Votes). 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmungen abgefragt werden sollen.</param>
        /// <param name="includingSubresources">Gibt an, ob die Subressourcen der einzelnen Abstimmungen ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request zugelassen werden soll. </param>
        /// <returns>Eine Liste von Objekten des Typs Ballot.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Request fehlschlägt, oder vom Server abgelehnt wird.</exception>
        public async Task<List<Ballot>> GetBallotsAsync(int groupId, bool includingSubresources, bool withCaching)
        {
            List<Ballot> ballots = new List<Ballot>();
            
            string serverResponse;
            try
            {
                serverResponse = await groupAPI.SendGetBallotsRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    includingSubresources,
                    withCaching);
            }   
            catch (APIException ex)
            {
                Debug.WriteLine("GetBallotsAsync: Request failed. Error code is {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("GetBallotsAsync: Group not found.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }        

            // Parse Liste von Ballots aus Serverantwort.
            if (serverResponse != null)
            {
                ballots = jsonParser.ParseBallotListFromJson(serverResponse);
            }

            return ballots;
        }

        /// <summary>
        /// Ruft die Daten zu einer bestimmten Abstimmung einer Gruppe vom Server ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die abgefragt werden soll.</param>
        /// <param name="includingSubressources">Gibt an, ob Daten bezüglich Subressourcen (Option und Votes) 
        ///     ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob bei der Abfrage Caching zugelassen werden soll.</param>
        /// <returns>Eine Instanz der Klasse Ballot mit den abgerufenen Daten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt, oder der
        ///     Server die Anfrage ablehnt.</exception>
        public async Task<Ballot> GetBallotAsync(int groupId, int ballotId, bool includingSubressources, bool withCaching)
        {
            Ballot ballot = null;

            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendGetBallotRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    ballotId,
                    includingSubressources,
                    withCaching);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("GetBallotAsync: Request failed. Error code is {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("GetBallotAsync: Group not found.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                // TODO Ballot not found

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                ballot = jsonParser.ParseBallotFromJson(serverResponse);
            }

            return ballot;
        }

        /// <summary>
        /// Ruft die Abstimmungsoptionen zu einer bestimmten Abstimmung vom Server ab. Die Abstimmungsoptionen können einschließlich
        /// zugehöriger Subressourcen (Votes) abgerufen werden. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die betroffene Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen abgefragt werden sollen.</param>
        /// <param name="includingSubresources">Gibt an, ob die zugehörigen Subressourcen ebenfalls abgefragt werden sollen.</param>
        /// <param name="withCaching">Gibt an, ob Caching bei diesem Request zugelassen werden soll.</param>
        /// <returns>Eine Liste von Instanzen der Klasse Option.</returns>
        /// <exception cref="ClientException">Wirft ClientException, falls Request fehlschlägt oder vom Server abgelehnt wurde.</exception>
        public async Task<List<Option>> GetOptionsForBallotAsync(int groupId, int ballotId, bool includingSubresources, bool withCaching)
        {
            List<Option> options = new List<Option>();

            string serverResponse;
            try
            {
                serverResponse = await groupAPI.SendGetOptionsRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    ballotId,
                    includingSubresources,
                    withCaching);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("GetOptionsForBallotAsync: Request failed. Error code is {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("GetBallotsAsync: Group not found.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                if (ex.ErrorCode == ErrorCodes.BallotNotFound)
                {
                    Debug.WriteLine("GetBallotsAsync: Ballot not found.");
                    DeleteBallot(ballotId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Parse Liste von Options aus Serverantwort.
            if (serverResponse != null)
            {
                options = jsonParser.ParseOptionListFromJson(serverResponse);
            }

            return options;
        }

        /// <summary>
        /// Führt eine Synchronisierung der Abstimmungen der angegebenen Gruppe mit den Daten vom Server durch.
        /// Die lokalen Datensätze werden auf die vom Server angepasst.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Synchronisation der Abstimmungen durchgeführt werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlschlägt.</exception>
        public async Task SynchronizeBallotsWithServerAsync(int groupId)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Debug.WriteLine("SynchronizeBallotsWithServerAsync: Start.");

            // Synchronisiere zunächst die Teilnehmer-Information, das es bei den Einfügeoperationen zu keinen Problemen kommt.
            await SynchronizeGroupParticipantsAsync(groupId);

            // Frage zunächst die Abstimmungen mit Subressourcen vom Server ab.
            List<Ballot> referenceBallots = await GetBallotsAsync(groupId, true, false);

            // Frage die lokal gespeicherten Abstimmungen mit Subressourcen ab.
            List<Ballot> localBallots = GetBallots(groupId, true);

            List<Ballot> updatableBallots = new List<Ballot>();
            List<Ballot> newBallots = new List<Ballot>();
            // Prüfe, ob neue Abstimmungen hinzugekommen sind, oder bestehende Abstimmungen aktualisiert werden müssen.
            foreach (Ballot refernceBallot in referenceBallots)
            {
                bool isContained = false;

                foreach (Ballot localBallot in localBallots)
                {
                    if (localBallot.Id == refernceBallot.Id)
                    {
                        isContained = true;
                        updatableBallots.Add(refernceBallot);                       
                    }
                }

                if (!isContained)
                {
                    newBallots.Add(refernceBallot);
                }
            }

            // Führe Einfüge-Operation bzw. Aktualisierungen aus.
            foreach (Ballot newBallot in newBallots)
            {
                bool successful = StoreBallot(groupId, newBallot);
                if (!successful)
                {
                    Debug.WriteLine("SynchronizeBallotsWithServerAsync: Couldn't store ballot. Missing references.");
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Couldn't store ballot probably due to missing references.");
                }
            }
            foreach (Ballot updatableBallot in updatableBallots)
            {
                bool successful = UpdateBallot(updatableBallot);
                if (!successful)
                {
                    Debug.WriteLine("SynchronizeBallotsWithServerAsync: Couldn't update ballot. Missing references.");
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Couldn't update ballot probably due to missing references.");
                }
            }

            // Entferne Abstimmungen, wenn sie auf dem Server nicht mehr verfügbar sind.
            foreach (Ballot localBallot in localBallots)
            {
                bool isContained = false;

                foreach (Ballot referenceBallot in referenceBallots)
                {
                    if (localBallot.Id == referenceBallot.Id)
                    {
                        isContained = true;
                    }
                }

                if (!isContained)
                {
                    // Entferne Abstimmung aus lokalen Datensätzen.
                    DeleteBallot(localBallot.Id);
                }
            }

            // Synchronisiere nun die Abstimmungsoptionen und Votes der Abstimmungen.
            Debug.WriteLine("SynchronizeBallotsWithServerAsync: Start processing options and votes.");
            foreach (Ballot referenceBallot in referenceBallots)
            {
                Debug.WriteLine("SynchronizeBallotsWithServerAsync: Start processing options and votes for ballot with id {0}.", referenceBallot.Id);
                if (referenceBallot.Options != null)
                {
                    // Synchronisiere Abstimmungsoptionen dieser Abstimmung.
                    SynchronizeLocalOptionsOfBallot(referenceBallot.Id, referenceBallot.Options);

                    // Synchronisiere Votes der Abstimmungsoptionen dieser Abstimmung.
                    foreach (Option refOption in referenceBallot.Options)
                    {
                        if (refOption.VoterIds == null)
                            refOption.VoterIds = new List<int>();

                        SynchronizeLocalVotesForOption(referenceBallot.Id, refOption.Id, refOption.VoterIds);
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine("SynchronizeBallotsWithServerAsync: Finished. Elapsed time: {0}.", sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Synchronisiert eine einzelne Abstimmung mit den Daten vom Server. Die lokalen
        /// Datensätze werden auf die des Servers angepasst.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die synchronisiert werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, falls Synchronisation fehlschlägt.</exception>
        public async Task SynchronizeBallotWithServerAsync(int groupId, int ballotId)
        {
            Stopwatch sw = Stopwatch.StartNew();
            Debug.WriteLine("SynchronizeBallotWithServerAsync: Start.");

            // Synchronisiere zunächst die Teilnehmer-Information, das es bei den Einfügeoperationen zu keinen Problemen kommt.
            await SynchronizeGroupParticipantsAsync(groupId);

            // Frage die Daten zu der Abstimmung ab.
            Ballot referenceBallot = await GetBallotAsync(groupId, ballotId, true, false);

            // Frage die Abstimmung aus den lokalen Datensätzen ab.
            Ballot localBallot = GetBallot(ballotId, false);

            if (localBallot == null && referenceBallot != null)
            {
                Debug.WriteLine("SynchronizeBallotWithServerAsync: Ballot seems to be missing in the local datasets.");
                bool successful = StoreBallot(groupId, referenceBallot);
                if (!successful)
                {
                    Debug.WriteLine("SynchronizeBallotWithServerAsync: Failed to store ballot.");
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Couldn't store ballot, probably due to missing references.");
                }
            }

            if (localBallot != null && referenceBallot != null)
            {
                // Aktualisere die Daten der lokalen Abstimmung.
                UpdateBallot(referenceBallot);
            }

            if (referenceBallot != null && referenceBallot.Options != null)
            {
                // Aktualisiere noch die Optionen und die Votes.
                SynchronizeLocalOptionsOfBallot(ballotId, referenceBallot.Options);

                foreach (Option option in referenceBallot.Options)
                {
                    if (option.VoterIds != null)
                    {
                        SynchronizeLocalVotesForOption(ballotId, option.Id, option.VoterIds);
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine("SynchronizeBallotWithServerAsync: Finished. Elapsed time is {0}.", sw.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// Erstellt die Verknüpfung zwischen lokalem Nutzer und der angegebenen
        /// Abstimmungsoption. Der lokale Nutzer hat dann für diese Abstimmungsoption
        /// abgestimmt. Erzeugt die Verknüpfung auf dem Server und in den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung.</param>
        /// <param name="optionId">Die Id der gewählten Abstimmungsoption.</param>
        /// <returns>Liefert true, wenn die Erstellung erfolgreich war. Liefert false,
        ///     wenn die Aktion nicht durchgeführt werden musste, z.B. wenn der Nutzer
        ///     bereits für diese Option gestimmt hat.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt, 
        ///     oder Anfrage vom Server abgelehnt wurde.</exception>
        public async Task<bool> PlaceVoteAsync(int groupId, int ballotId, int optionId)
        {
            bool successful = false;
            User localUser = getLocalUser();

            // Rufe lokale Instanz der Abstimmung ab.
            Ballot localBallot = GetBallot(ballotId, false);

            // Prüfe, ob der Nutzer bereits für genau diese Option abgestimmt hat.
            if (HasPlacedVoteForOption(optionId, localUser.Id))
            {
                Debug.WriteLine("PlaceVoteAsync: No need to place vote. User has already voted for this option.");
                return false;
            }

            // Sonderfall: Single-Choice Abstimmung.
            if (localBallot != null && 
                localBallot.IsMultipleChoice.HasValue && 
                localBallot.IsMultipleChoice.Value == false)
            {
                Debug.WriteLine("PlaceVoteAsync: Need to check special case Single-Choice ballot.");
                // Prüfe, ob der Nutzer bereits für eine Option dieser Abstimmung abgestimmt hat.
                if (HasPlacedVoteInBallot(ballotId, localUser.Id))
                {
                    List<Option> selectedOptions = GetSelectedOptionsInBallot(ballotId, localUser.Id);

                    // Entferne alle bisher getätigten Abstimmungen.
                    foreach (Option selectedOption in selectedOptions)
                    {
                        Debug.WriteLine("PlaceVoteAsync: Need to remove vote for option with id {0} and text '{1}' first.", 
                            selectedOption.Id, selectedOption.Text);
                        await RemoveVoteAsync(groupId, ballotId, selectedOption.Id);
                    }
                }
            }

            // Setze Request ab.
            try
            {
                await groupAPI.SendVoteForOptionRequest(
                    localUser.ServerAccessToken,
                    groupId,
                    ballotId,
                    optionId);

                successful = true;
            }
            catch (APIException ex)
            {
                Debug.WriteLine("PlaceVoteAsync: Failed to place vote. Request failed. Error code is: {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("RemoveVoteAsync: Group seems to be deleted on the server.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                if (ex.ErrorCode == ErrorCodes.BallotNotFound)
                {
                    Debug.WriteLine("PlaceVoteAsync: Ballot seems to be deleted on the server.");
                    DeleteBallot(ballotId);
                }

                // Weitere Fehlerbehandlungen für Vote-spezifische Fehler.
                if (ex.ErrorCode == ErrorCodes.BallotUserHasAlreadyVoted)
                {
                    Debug.WriteLine("RemoveVoteAsync: Server data sets have already linked this option and the user. " +
                        "Start updating the  local data sets.");
                    successful = true;
                }
                
                if (!successful)
                {
                    throw new ClientException(ex.ErrorCode, ex.Message);
                }
            }

            if (successful)
            {
                // Aktualisiere lokale Datensätze.
                AddVote(ballotId, optionId, localUser.Id);
            }

            return successful;
        }

        /// <summary>
        /// Entfernt die Verknüpfung zwischen lokalem Nutzer und der angegebenen
        /// Abstimmungsoption. Der lokale Nutzer hat dann nicht mehr für diese Abstimmungsoption
        /// abgestimmt. Entfernt die Verknüpfung auf dem Server und in den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption, die der Nutzer abwählt.</param>
        /// <returns>Liefert true, wenn Aktion erfolgreich. Liefert false wenn keine Aktion ausgeführt werden 
        ///     musste, z.B. wenn der Nutzer gar nicht für die angegebene Abstimmungsoption abgestimmt hatte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt, oder der Server
        ///     die Anfrage abgelehnt hat.</exception>
        public async Task<bool> RemoveVoteAsync(int groupId, int ballotId, int optionId)
        {
            bool successful = false;
            User localUser = getLocalUser();
            
            // Prüfe, ob der Nutzer überhaupt für genau diese Option abgestimmt hat.
            if (!HasPlacedVoteForOption(optionId, localUser.Id))
            {
                Debug.WriteLine("RemoveVoteAsync: No need to remove vote. User has not voted for this option.");
                return false;
            }

            try
            {
                await groupAPI.SendRemoveVoteRequest(
                    localUser.ServerAccessToken,
                    groupId,
                    ballotId,
                    optionId,
                    localUser.Id);

                successful = true;
            }
            catch (APIException ex)
            {
                Debug.WriteLine("RemoveVoteAsync: Failed to remove vote. Request failed. Error code is {0}.", ex.ErrorCode);

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("RemoveVoteAsync: Group seems to be deleted on the server.");
                    // Markiere Gruppe lokal als gelöscht.
                    MarkGroupAsDeleted(groupId);
                }

                // TODO Ballot not found
                // - eventuell weitere Fehlerbehandlungen für vote spezifische Fehler?

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (successful)
            {
                // Entferne aus lokalen Datensätzen.
                RemoveVote(optionId, localUser.Id);
            }

            return successful;
        }

        /// <summary>
        /// Sendet einen Request zum Anlegen einer neuen Abstimmung an den Server.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, in der die Abstimmung angelegt werden soll.</param>
        /// <param name="newBallot">Das Abstimmungsojekt mit den Daten der neuen Abstimmung.</param>
        /// <returns>Liefert true, wenn die Abstimmung erfolgreich angelegt werden konnte. Liefert false, 
        ///     wenn die Abstimmung nicht angelegt werden konnte, da z.B. die Validierung der Daten fehlgeschlagen ist.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Erzeugung der Abstimmung fehlgeschlagen ist, oder 
        ///     vom Server abgelehnt wurde.</exception>
        public async Task<bool> CreateBallotAsync(int groupId, Ballot newBallot)
        {
            bool successful = false;

            if (newBallot == null)
                return successful;

            // Führe Validierung der Abstimmungsdaten durch. Abbruch bei aufgetretenem Validierungsfehler.
            clearValidationErrors();
            newBallot.ClearValidationErrors();
            newBallot.ValidateAll();
            if (newBallot.HasValidationErrors())
            {
                reportValidationErrors(newBallot.GetValidationErrors());
                return successful;
            }

            // Die für die neue Abstimmung angegebenen Optionen.
            List<Option> ballotOptions = newBallot.Options;

            // Setze Optionen im Objekt selbst auf null zwecks Create Request.
            newBallot.Options = null;

            // Generiere JSON-Dokument.
            string jsonContent = jsonParser.ParseBallotToJson(newBallot);
            if (jsonContent == null)
            {
                Debug.WriteLine("CreateBallotAsync: Couldn't serialize ballot object to json. Cannot continue.");
                return successful;
            }

            // Setze Request zum Anlegen der Abstimmung ab.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendCreateBallotRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    jsonContent);

                successful = true;
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("CreateBallotAsync: The group with id {0} seems to be deleted on the server.", groupId);
                    MarkGroupAsDeleted(groupId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            Ballot serverBallot = null;
            if (serverResponse != null)
            {
                // Parse Server Antwort.
                serverBallot = jsonParser.ParseBallotFromJson(serverResponse);

                if (serverBallot != null)
                {
                    // Speichere Abstimmung ab.
                    if (!StoreBallot(groupId, serverBallot))
                    {
                        Debug.WriteLine("CreateBallotAsync: Failed to store ballot.");
                        throw new ClientException(ErrorCodes.LocalDatabaseException, "Failed to store ballot with id " + serverBallot.Id + ".");
                    }
                }
            }

            // Sende noch die Requests zum Anlegen der Abstimmungsoptionen.
            if (serverBallot != null && ballotOptions != null)
            {
                bool successfullyCreatedOptions = true;

                foreach (Option option in ballotOptions)
                {
                    try
                    {
                        await CreateBallotOptionAsync(groupId, serverBallot.Id, option);
                    }
                    catch (ClientException ex)
                    {
                        // Nicht die ganze Ausführung abbrechen bei fehlgeschlagenem Request.
                        Debug.WriteLine("CreateBallotAsync: Failed to store a ballot option. " + 
                            "The option with id {0} could not be created. Msg is: {1}.", option.Id, ex.Message);
                        successfullyCreatedOptions = false;
                    }
                }

                if (!successfullyCreatedOptions)
                {
                    // Werfe Fehler mit entsprechendem Fehlercode.
                    throw new ClientException(ErrorCodes.OptionCreationHasFailedInBallotCreationProcess, "Failed to store options.");
                }
            }

            return successful;
        }

        /// <summary>
        /// Sendet einen Request zum Anlegen einer neuen Abstimmungsoption an den Server.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung für die eine neue Option angelegt werden soll.</param>
        /// <param name="newOption">Das Objekt vom Typ Option mit den Daten der neuen Abstimmungsoption.</param>
        /// <returns>Liefert true, wenn die Abstimmungsoption erfolgreich angelegt werden konnte. Liefert false, 
        ///     wenn die Abstimmungsoption nicht angelegt werden konnte, da z.B. die Validierung der Daten fehlgeschlagen ist.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Erzeugung der Abstimmungsoption fehlschlägt, 
        ///     oder vom Server abgelehnt wird.</exception>
        public async Task<bool> CreateBallotOptionAsync(int groupId, int ballotId, Option newOption)
        {
            bool successful = false;

            if (newOption == null)
                return successful;

            // Führe Validierung der Abstimmungsoptionsdaten durch. Abbruch bei Validierungsfehler.
            clearValidationErrors();
            newOption.ClearValidationErrors();
            newOption.ValidateAll();
            if (newOption.HasValidationErrors())
            {
                reportValidationErrors(newOption.GetValidationErrors());
                return successful;
            }

            // Generiere JSON-Dokument.
            string jsonContent = jsonParser.ParseOptionToJson(newOption);
            if (jsonContent == null)
            {
                Debug.WriteLine("CreateBallotOptionAsync: Failed to serialize option to json. Cannot continue.");
                throw new ClientException(ErrorCodes.JsonParserError, "Failed to create json document.");
            }

            // Setze Request zum Anlegen der Option ab.
            string serverResponse = null;
            try
            {
                serverResponse = await groupAPI.SendCreateOptionRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    ballotId,
                    jsonContent);

                successful = true;
            }
            catch (APIException ex)
            {
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    Debug.WriteLine("CreateBallotOptionAsync: The group with id {0} seems to be deleted on the server.", groupId);
                    MarkGroupAsDeleted(groupId);
                }

                if (ex.ErrorCode == ErrorCodes.BallotNotFound)
                {
                    Debug.WriteLine("CreateBallotOptionAsync: There seems to be no ballot with the specified id on the server.");
                    DeleteBallot(ballotId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            if (serverResponse != null)
            {
                // Parse Server Antwort.
                Option serverOption = jsonParser.ParseOptionFromJson(serverResponse);
                if (serverOption != null)
                {
                    // Speichere Option lokal.
                    if (!StoreOption(ballotId, serverOption, false))
                    {
                        throw new ClientException(ErrorCodes.LocalDatabaseException, 
                            "CreateBallotOptionAsync: Failed to store option with id {0}." + serverOption.Id);
                    }
                }
            }

            return successful;
        }

        /// <summary>
        /// Sendet eine Anfrage zum Entfernene der angegebenen Abstimmungsoption an den Server.
        /// Die Abstimmungsoption wird in den Datensätzen des Servers und lokal entfernt.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, für die eine Abstimmungsoption entfernt wird.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption, die entfernt wird.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschvorgang fehlschlägt, oder
        ///     Server die Anfrage ablehnt.</exception>
        public async Task RemoveBallotOptionAsync(int groupId, int ballotId, int optionId)
        {
            // Setze Request zum Löschen der Abstimmungsoption ab.
            try
            {
                await groupAPI.SendDeleteOptionRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    ballotId,
                    optionId);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("RemoveBallotOptionAsync: Request failed. Error code is: {0} and msg is: {1}.", ex.ErrorCode, ex.Message);

                // TODO Group Not found

                // TODO Ballot Not found

                if (ex.ErrorCode == ErrorCodes.OptionNotFound)
                {
                    Debug.WriteLine("Option seems to be already deleted on the server.");
                    DeleteOption(optionId);
                }

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Lösche Abstimmungsoption aus den lokalen Datensätzen.
            DeleteOption(optionId);
        }

        /// <summary>
        /// Führt Aktualisierung der Abstimmung aus. Es wird ermittelt welche Eigenschaften eine Aktualisierung 
        /// erhalten haben. Die Aktualisierungen werden an den Server übermittelt, der die Aktualisierung auf
        /// dem Serverdatensatz ausführt und die Gruppenmitglieder über die Änderung informiert.
        /// Ebenso werden die Abstimmungsoptionen aktualisiert, falls Änderungen vorgenommen wurden.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="oldBallot">Der Datensatz der Abstimmung, vor der Aktualisierung.</param>
        /// <param name="newBallot">Der Datensatz mit den neu eingegebenen Daten. Enthält auch die festgelegten
        ///     Abstimmungsoptionen.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich war, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlgeschlagen ist, oder nur
        ///     in Teilen erfolgreich durchgeführt werden konnte.</exception>
        public async Task<bool> EditBallotAsync(int groupId, Ballot oldBallot, Ballot newBallot)
        {
            if (oldBallot == null || newBallot == null)
                return false;

            // Validiere zunächst die neu eingegebenen Daten. Bei Validierungsfehlern kann man hier gleich abbrechen.
            clearValidationErrors();
            newBallot.ClearValidationErrors();
            newBallot.ValidateAll();
            if (newBallot.HasValidationErrors())
            {
                reportValidationErrors(newBallot.GetValidationErrors());
                return false;
            }

            // Generiere Ballot Objekt für die Aktualisierung. Dieses Objekt enthält nur die aktualisierten Eigenschaften.
            bool requiresUpdate = true;
            Ballot updatableBallot = perpareUpdatableBallotInstance(oldBallot, newBallot);
            if (updatableBallot == null)
            {
                Debug.WriteLine("EditBallotAsync: No update required.");
                requiresUpdate = false;
            }

            if (requiresUpdate)
            {
                // Erstelle JSON-Dokument für die Aktualisierung.
                string jsonContent = jsonParser.ParseBallotToJson(updatableBallot);
                if (jsonContent == null)
                {
                    Debug.WriteLine("EditBallotAsync: Failed to generate json document.");
                    throw new ClientException(ErrorCodes.JsonParserError, "Failed to generate json document.");
                }

                // Setze Request zur Aktualisierung der Abstimmung ab.
                string serverResponse = null;
                try
                {
                    serverResponse = await groupAPI.SendUpdateBallotRequest(
                        getLocalUser().ServerAccessToken,
                        groupId,
                        oldBallot.Id,
                        jsonContent);
                }
                catch (APIException ex)
                {
                    Debug.WriteLine("EditBallotAsync: Failed request. Error code: {0}, msg is: '{1}'.", ex.ErrorCode, ex.Message);

                    // TODO - group not found, ballot not found, ...

                    throw new ClientException(ex.ErrorCode, ex.Message);
                }

                // Parse Antwort des Servers.
                Ballot serverBallot = jsonParser.ParseBallotFromJson(serverResponse);
                if (serverBallot != null)
                {
                    Debug.WriteLine("EditBallotAsync: Start local updating of ballot.");
                    // Aktualisiere die Abstimmung.
                    bool successful = UpdateBallot(serverBallot);
                    if (!successful)
                    {
                        throw new ClientException(ErrorCodes.LocalDatabaseException, "Failed to update local ballot");
                    }
                }
            }

            // Führe noch eine Aktualisierung der Abstimmungsoptionen durch.
            List<Option> currentOptions = GetOptions(oldBallot.Id, false);
            // Extrahiere die Liste von Abstimmungsoptionen der neuen Abstimmung.
            List<Option> newOptionList = newBallot.Options;

            await SynchronizeBallotOptionsAsync(groupId, oldBallot.Id, currentOptions, newOptionList);

            return true;
        }

        /// <summary>
        /// Bereitet ein Objekt vom Typ Ballot vor, welches alle Properties enthält, die sich geändert haben.
        /// Die Methode bekommt eine alte Version eines Ballot Objekts und eine neue Version und ermittelt 
        /// dann die Properties, die eine Aktualisierung erhalten haben und schreibt diese in eine neue Ballot
        /// Instanz. Die von der Methode zurückgelieferte Ballot Instanz kann dann direkt für die Aktualisierung auf
        /// dem Server verwendet werden. Achtung: Hat sich überhaupt keine Property geändert, so gibt die Methode null zurück.
        /// </summary>
        /// <param name="oldBallot">Das Ballot Objekt vor der Aktualisierung.</param>
        /// <param name="newBallot">Das Ballot Objekt mit den aktuellen Werten.</param>
        /// <returns>Ein Objekt der Klasse Ballot, bei dem nur die Eigenschaften mit neuem Wert gesetzt sind.
        ///     Hat sich keine Eigenschaft geändert, so wird null zurückgegeben.</returns>
        private Ballot perpareUpdatableBallotInstance(Ballot oldBallot, Ballot newBallot)
        {
            bool hasChanged = false;
            Ballot updatedBallot = new Ballot();

            if (oldBallot.Title != newBallot.Title)
            {
                hasChanged = true;
                updatedBallot.Title = newBallot.Title;
            }

            if (oldBallot.Description != newBallot.Description)
            {
                hasChanged = true;
                updatedBallot.Description = newBallot.Description;
            }

            if (oldBallot.IsClosed.HasValue && newBallot.IsClosed.HasValue && 
                oldBallot.IsClosed.Value != newBallot.IsClosed.Value)
            {
                hasChanged = true;
                updatedBallot.IsClosed = newBallot.IsClosed;
            }

            // Prüfe, ob sich überhaupt eine Property geändert hat.
            if (!hasChanged)
            {
                Debug.WriteLine("perpareUpdatableBallotInstance: No property of ballot has changed. Method will return null.");
                updatedBallot = null;
            }

            return updatedBallot;
        }

        /// <summary>
        /// Führt eine Synchronisation der Abstimmungsoptionen einer Abstimmung anhand der übergebenen 
        /// Listen durch. Hierbei dient die Liste der neuen Abstimmungsoptionen als Referenzwert.
        /// Die Abstimmungsoptionen der Liste mit aktuellen (alten) Abstimmungsoptionen werden entfernt, 
        /// sofern sie nicht mehr in der neuen Liste stehen. Neue Optionen, die nicht in der allten Liste stehen,
        /// werden hinzugefügt. Entsprechende Anfragen werden an den Server geschickt.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen gehören.</param>
        /// <param name="oldOptionList">Die Liste der aktuell gespeichterten (d.h. alten) Abstimmungsoptionen der Abstimmung.</param>
        /// <param name="newOptionList">Die Liste der neuen Abstimmungsoptionen. Dies ist die Referenz gegen die synchronisiert wird.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn nicht alle Abstimmungsoptionen erfolgreich 
        ///     synchronisiert werden konnten.</exception>
        public async Task SynchronizeBallotOptionsAsync(int groupId, int ballotId, List<Option> oldOptionList, List<Option> newOptionList)
        {
            List<Option> creatableOptions = new List<Option>();
            List<Option> deletableOptions = new List<Option>();

            if (oldOptionList != null && newOptionList != null)
            {
                foreach (Option newOption in newOptionList)
                {
                    bool isContained = false;

                    foreach (Option currentOption in oldOptionList)
                    {
                        if (newOption.Text == currentOption.Text)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Need to add option with text: '{0}'.", newOption.Text);
                        creatableOptions.Add(newOption);
                    }
                }

                foreach (Option currentOption in oldOptionList)
                {
                    bool isContained = false;

                    foreach (Option newOption in newOptionList)
                    {
                        if (newOption.Text == currentOption.Text)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Need to remove option with text: '{0}'.", currentOption.Text);
                        deletableOptions.Add(currentOption);
                    }
                }
            }

            // Führe Aktionen aus. Anlegen von neuen Optionen und löschen von entfernten Optionen.
            Task<bool> createOptionsResultTask = Task<bool>.Run(async () =>
            {
                bool optionInsertsSuccessful = true;
                foreach (Option creatableOption in creatableOptions)
                {
                    try
                    {
                        // Erzeuge Option auf Server und lokal.
                        bool successful = await CreateBallotOptionAsync(groupId, ballotId, creatableOption);
                        if (!successful)
                        {
                            optionInsertsSuccessful = false;
                        }                        
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Request to create new option with text {0} failed.", creatableOption.Text);
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Error code is: {0} and message: '{1}'.", ex.ErrorCode, ex.Message);
                        optionInsertsSuccessful = false;
                    }
                }
                return optionInsertsSuccessful;
            });
            Task<bool> deleteOptionsResultTask = Task<bool>.Run(async () =>
            {
                bool optionDeletionsSuccessful = true;
                foreach (Option deletableOption in deletableOptions)
                {
                    try
                    {
                        // Entferne Option auf Server und lokal.
                        await RemoveBallotOptionAsync(groupId, ballotId, deletableOption.Id);
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Request to delete option with id {0} failed.", deletableOption.Id);
                        Debug.WriteLine("SynchronizeBallotOptionsAsync: Error code is: {0} and message: '{1}'.", ex.ErrorCode, ex.Message);
                        optionDeletionsSuccessful = false;
                    }
                }
                return optionDeletionsSuccessful;
            });

            // Warte auf die Ergebnisse.
            bool optionActionsSuccessful = true;
            optionActionsSuccessful = optionActionsSuccessful && await createOptionsResultTask;
            optionActionsSuccessful = optionActionsSuccessful && await deleteOptionsResultTask;

            if (!optionActionsSuccessful)
            {
                // Werfe speziellen Fehler.
                throw new ClientException(ErrorCodes.OptionUpdatesErrorsOccurred, "Could not perform all updates successfully.");
            }
        }

        /// <summary>
        /// Löscht die Abstimmung, die durch die angegebene Id identifiziert ist. 
        /// Die Abstimmung wird auf dem Server und lokal gelöscht.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die gelöscht werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschvorgang fehlschlägt,
        ///     oder Server die Anfrage ablehnt.</exception>
        public async Task DeleteBallotAsync(int groupId, int ballotId)
        {
            Debug.WriteLine("DeleteBallotAsync: Start deleting ballot process: groupId {0} and ballotId {1}.", groupId, ballotId);

            // Setze Request zum Löschen der Abstimmung ab.
            try
            {
                await groupAPI.SendDeleteBallotRequest(
                    getLocalUser().ServerAccessToken,
                    groupId,
                    ballotId);
            }
            catch (APIException ex)
            {
                Debug.WriteLine("DeleteBallotAsync: Failed to delete ballot. Error code: {0} and msg: '{1}'.", 
                    ex.ErrorCode, ex.Message);

                // TODO 

                throw new ClientException(ex.ErrorCode, ex.Message);
            }

            // Lösche Abstimmung lokal.
            DeleteBallot(ballotId);
        }
        #endregion RemoteBallotMethods

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
        /// Hole Gruppe mit der angegebenen Id aus den lokalen Datensätzen
        /// und gib sie zurück.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <returns>Liefert eine Instanz der Klasse Group.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Abruf fehlschlägt.</exception>
        public Group GetGroup(int groupId)
        {
            Group group = null;
            try
            {
                // Rufe Gruppe ab, inklusive Informationen über Teilnehmer.
                group = groupDBManager.GetGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetGroup: Error occurred in DB. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return group;
        }

        /// <summary>
        /// Hole alle Gruppen, die mittels eines Dirty-Flag markiert sind.
        /// Diese Gruppen wurden markiert, da sich ihre Daten geändert haben seit dem
        /// letzten Abruf.
        /// </summary>
        /// <returns>Eine Liste von Instanzen von Group.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<Group> GetDirtyGroups()
        {
            List<Group> dirtyGroups = null;
            try
            {
                dirtyGroups = groupDBManager.GetDirtyGroups();
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetDirtyGroups: Error occurred in DB. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return dirtyGroups;
        }

        /// <summary>
        /// Setzt das Dirty-Flag auf allen Datensätzen von Gruppen zurück.
        /// </summary>
        /// <exception cref="ClientException">Wirft ClientException, wenn Reset fehlschlägt.</exception>
        public void ResetDirtyFlagsOnGroups()
        {
            try
            {
                groupDBManager.ResetDirtyFlagOnGroups();
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("ResetDirtyFlagsOnGroups: Error occurred in DB. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Liefert die Identifier aller lokal verwalteten Gruppen zurück.
        /// </summary>
        /// <returns>Eine Liste von Ids lokal verwalteter Gruppen.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public List<int> GetLocalGroupIdentifiers()
        {
            List<int> identifiers;
            try
            {
                identifiers = groupDBManager.GetLocalGroupIdentifiers();
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetLocalGroupIdentifiers: Error occurred in DB. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return identifiers;
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
                if (!groupDBManager.IsGroupStored(group.Id))
                {
                    groupDBManager.StoreGroup(group);
                }
                else
                {
                    Debug.WriteLine("StoreGroupLocally: Group is already locally stored.");
                }
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
            // Frage Konversationen von Gruppe ab.
            List<Conversation> delConversations = GetConversations(groupId);
            // Lösche alle Nachrichten manuell. Nachrichten werden durch kaskadierendes Löschen nicht vollständig gelöscht. 
            foreach (Conversation conversation in delConversations)
            {
                DeleteConversationMessages(conversation.Id);
            }

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
                participants = groupDBManager.GetActiveParticipantsOfGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetParticipantsOfGroup: Error during retrieval. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return participants;
        }

        /// <summary>
        /// Ruft ein Verzeichnis mit den Daten aller Teilnehmer der angegebenen Gruppe ab, d.h. inaktive und
        /// aktive Teilnehmer. In dem Verzeichnis können die einzelnen Nutzerdatensätze über die Ids der Teilnehmer
        /// abgerufen werden.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu dem das Verzeichnis abgerufen werden soll.</param>
        /// <returns>Ein Verzeichnis, welches Objekte vom Typ User auf deren Id abbildet.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public Dictionary<int, User> GetParticipantsLookupDirectory(int groupId)
        {
            Dictionary<int, User> participantDictionary = null;
            try
            {
                participantDictionary = groupDBManager.GetAllParticipantsOfGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetParticipantsLookupDirectory: The retrieval of the directory has failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return participantDictionary;
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
        /// Fügt den übergebenen Nutzer als aktiven Nutzer der Gruppe mit der angegebenen Id hinzu.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="user">Der hinzuzufügende Nutzer.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public void AddParticipantToGroup(int groupId, User user)
        {
            if (!groupDBManager.IsGroupStored(groupId))
            {
                Debug.WriteLine("AddParticipantToGroup: There is no group with id {0} in the local datasets.",
                    groupId);

                throw new ClientException(ErrorCodes.GroupNotFound, "Cannot continue without stored group.");
            }

            // Prüfe, ob der Nutzer schon in den lokalen Datensätzen gespeichert ist.
            bool stored = userController.IsUserLocallyStored(user.Id);
            if (!stored)
            {
                userController.StoreUserLocally(user);
            }

            // Füge Nutzer als aktiver Teilnehmer der Gruppe hinzu.
            try
            {
                bool? activeStatus = groupDBManager.RetrieveActiveStatusOfParticipant(groupId, user.Id);
                if (!activeStatus.HasValue)
                {
                    // Teilnehmer noch nicht in Gruppe.
                    groupDBManager.AddParticipantToGroup(groupId, user.Id, true);
                }
                else if (activeStatus.Value == false)
                {
                    // Setze Teilnehmer wieder in den aktiven Zustand.
                    groupDBManager.ChangeActiveStatusOfParticipant(groupId, user.Id, true);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("AddParticipantToGroup: Failed to add participant to group. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Ermittelt, ob der Nutzer mit der angegebnen Id ein aktiver Teilnehmer der 
        /// spezifizierten Gruppe ist.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <returns>Liefer true, wenn der Nutzer ein aktiver Teilnehmer ist, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public bool IsActiveParticipant(int groupId, int participantId)
        {
            bool isActiveParticipant = false;

            try
            {
                bool? status = groupDBManager.RetrieveActiveStatusOfParticipant(groupId, participantId);
                if (status.HasValue && status.Value == true)
                {
                    isActiveParticipant = true;
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("IsActiveParticipant: Failed to determine active status of participant.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return isActiveParticipant;
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

        /// <summary>
        /// Aktualisiert die Benachrichtigungseinstellungen für die angegebene Gruppe.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Benachrichtigungseinstellungen geändert werden sollen.</param>
        /// <param name="newSetting">Die neu gewählte Einstellung.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Änderung fehlschlägt.</exception>
        public void ChangeNotificationSettingsForGroup(int groupId, NotificationSetting newSetting)
        {
            try
            {
                // Frage Daten ab.
                Group group = GetGroup(groupId);

                // Setze neue Einstellungen.
                group.GroupNotificationSetting = newSetting;

                // Speichere neue Einstellungen.
                groupDBManager.UpdateGroup(group, true);

            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("ChangeNotificationSettingsForGroup: Failed to change notification settings of group.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Markiert eine Gruppe lokal als gelöscht. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die lokal als gelöscht markiert werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn das Markieren fehlschlägt.</exception>
        public void MarkGroupAsDeleted(int groupId)
        {
            try
            {
                Group group = GetGroup(groupId);

                // Setze Deleted Flag.
                group.Deleted = true;

                // Speichere Datensatz ab.
                groupDBManager.UpdateGroup(group, false);

            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("MarkGroupAsDeleted: Failed to mark group as deleted. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalGroupMethods

        #region LocalConversationMethods
        /// <summary>
        /// Liefert alle Konversationen zurück, die der Gruppe mit der angegebenen Id zugeordnet
        /// sind. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversationen abgefraft werden sollen.</param>
        /// <returns>Liefert eine Liste von Conversation Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn der Abruf fehlschlägt.</exception>
        public List<Conversation> GetConversations(int groupId)
        {
            List<Conversation> conversations = null;
            try
            {
                conversations = groupDBManager.GetConversations(groupId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetConversations: Retrieving conversations failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return conversations;
        }

        /// <summary>
        /// Ruft die Konversation mit der angegebenen Id ab.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, die abgerufen werden soll.</param>
        /// <param name="includingConversationMessages">Gibt an, ob die Konversationsnachrichten dieser Konversation
        ///     mit geladen werden sollen.</param>
        /// <returns>Ein Objekt vom Typ Conversation. </returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public Conversation GetConversation(int conversationId, bool includingConversationMessages)
        {
            Conversation conv = null;
            try
            {
                conv = groupDBManager.GetConversation(conversationId, includingConversationMessages);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetConversation: Retrieving conversation failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return conv;
        }

        /// <summary>
        /// Speichere die Konversation zu der angegebenen Gruppe in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversation">Die Daten der Konversation als Objekt vom Typ Conversation.</param>
        /// <returns>Liefert true, wenn die Speicherung erfolgreich ist. Liefert false, wenn Speicherung
        ///     aufgrund fehlender Datensätze (z.B. Administrator-Nutzer) fehlschlägt.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung aufgrund eines
        ///     Datenbankfehlers fehlschlägt.</exception>
        public bool StoreConversation(int groupId, Conversation conversation)
        {
            try
            {
                // Prüfe, ob Gruppe lokal vorhanden ist.
                if (!groupDBManager.IsGroupStored(groupId))
                {
                    // Kann Konversation so nicht einfügen.
                    return false;
                }

                // Prüfe zunächst, ob der Nutzer, der als Admin eingetragen ist, auch lokal gespeichert ist.
                if (!userController.IsUserLocallyStored(conversation.AdminId))
                {
                    // Kann Konversation so nicht einfügen.
                    return false;
                }

                // Füge Konversation ein.
                groupDBManager.StoreConversation(groupId, conversation);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreConversation: Storing conversation failed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Speichert die übergebene Konversationsnachricht in den lokalen Datensätzen ab. Dabei wird die
        /// Nachricht nicht erneut gespeichert, wenn sie lokal schon vorhanden ist.
        /// </summary>
        /// <param name="conversationMsg">Die Daten der ConversationMessage, in Form eines ConversationMessage Objekts.</param>
        /// <returns>Liefert true, wenn Speicherung erfolgreich war. Liefert false, wenn Speicherung aufgrund fehlender
        ///     Referenzen fehlgeschlagen ist (z.B. fehlender Nutzer, der als Autor eingetragen ist).</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung aufgrund eines unerwarteten
        ///     Fehlers fehlschlägt.</exception>
        public bool StoreConversationMessage(ConversationMessage conversationMsg)
        {
            try
            {
                int authorId = conversationMsg.AuthorId;
                if (!userController.IsUserLocallyStored(authorId))
                {
                    Debug.WriteLine("StoreConversationMessage: Cannot store conversation message without author reference.");
                    return false;
                }

                int highestMessageNr = GetHighestMessageNumberOfConversation(conversationMsg.ConversationId);
                if (highestMessageNr != conversationMsg.MessageNumber)
                {
                    groupDBManager.StoreConversationMessage(conversationMsg);
                }
                else
                {
                    Debug.WriteLine("StoreConversationMessage: Conversation message seems to be already stored.");
                }
               
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreConversationMessage: Failed to store conversation message.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Aktualisiert den Datensatz der angegebenen Konversation in den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der diese Konversation gehört.</param>
        /// <param name="updatedConversation">Objekt vom Typ Conversation mit den aktualisierten Daten der Konversation.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich war. Liefert false, wenn die Aktualisierung aufgrund
        ///     fehlender Daten (z.B. lokaler Nutzerdatensatz des Admin) nicht durchgeführt werden kann.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung aufgrund eines
        ///     unerwarteten Fehlers fehlschlägt.</exception>
        public bool UpdateConversation(int groupId, Conversation updatedConversation)
        {
            try
            {
                if (!userController.IsUserLocallyStored(updatedConversation.AdminId) || 
                    !IsActiveParticipant(groupId, updatedConversation.AdminId))
                {
                    // Fehlende Daten.
                    Debug.WriteLine("UpdateConversation: Cannot perform update due to missing reference data.");
                    return false;
                }

                groupDBManager.UpdateConversation(updatedConversation);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("UpdateConversation: Failed to update the conversation.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return true;
        }

        /// <summary>
        /// Markiert die angegebene Konversation als gelöscht. 
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktion fehlschlägt.</exception>
        public void MarkConversationAsClosed(int conversationId)
        {
            try
            {
                Conversation conv = GetConversation(conversationId, false);

                // Setze Closed Flag.
                conv.IsClosed = true;

                // Aktualisiere Datensatz.
                groupDBManager.UpdateConversation(conv);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("MarkConversationAsClosed: The conversation couldn't be closed.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Speichert die übergebene Menge von Konversationsnachrichten in den lokalen Datensätzen ab.
        /// Dabei werden Nachrichten nicht erneut gespeichert, wenn sie lokal schon vorhanden sind.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der diese Nachrichten gehören.</param>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten abgespeichert werden sollen.</param>
        /// <param name="conversationMessages">Die übergebene Menge von zu speichernden Konversationsnachrichten.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public void StoreConversationMessages(int groupId, int conversationId, List<ConversationMessage> conversationMessages)
        {
            // Prüfe zunächst, ob alle Autoren lokal vorhanden sind.
            Dictionary<int, User> participantDirectory = GetParticipantsLookupDirectory(groupId);

            foreach(ConversationMessage convMsg in conversationMessages)
            {
                if (!participantDirectory.ContainsKey(convMsg.AuthorId))
                {
                    // Bilde hier nur auf den Dummy Nutzer ab.
                    Debug.WriteLine("StoreConversationMessages: No participant with id {0} stored for the group. " + 
                        "Need to map to the dummy user.");
                    convMsg.AuthorId = 0;
                }
            }

            // Speichere Nachrichten lokal ab.
            try
            {
                groupDBManager.BulkInsertConversationMessages(conversationId, conversationMessages);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreConversationMessages: Couldn't store conversation messages.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Holt die Konversationsnachrichten für die angegebene Konversation aus den lokalen Datensätzen.
        /// Die Anfrage kann durch die Nachrichtennummer eingeschränkt werden. Es werden nur Nachrichten 
        /// zurück geliefert, die eine höhere Nachrichtennummer haben, als die angegebene.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten lokal abgefragt werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, ab der die Nachrichten abgerufen werden sollen.</param>
        /// <returns>Eine Liste von ConversationMessage Objekten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<ConversationMessage> GetConversationMessages(int conversationId, int messageNr)
        {
            List<ConversationMessage> conversationMessages = null;

            try
            {
                conversationMessages = groupDBManager.GetConversationMessages(conversationId, messageNr);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetConversationMessages: Failed to retrieve conversation messages.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return conversationMessages;
        }

        /// <summary>
        /// Gibt die Konversationsnachricht für die angegebene Konversation zurück, die zuletzt empfangen wurde.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <returns>Ein Objekt vom Typ ConversationMessage, oder null, wenn keine Nachricht in der Konversation existiert.</returns>
        public ConversationMessage GetLastConversationMessage(int conversationId)
        {
            ConversationMessage lastReceivedConversationMessage = null;

            try
            {
                List<ConversationMessage> messages = groupDBManager.GetLastestConversationMessages(conversationId, 1, 0);
                if (messages != null && messages.Count == 1)
                {
                    lastReceivedConversationMessage = messages[0];
                }
            }
            catch (DatabaseException ex)
            {
                // Gebe Exception nicht an den Aufrufer weiter.
                Debug.WriteLine("Retrieval of converstion message has failed. Message is {0}.", ex.Message);
            }

            return lastReceivedConversationMessage;
        }

        /// <summary>
        /// Ruft die Konversationsnachricht ab, die durch die angegebene Id eindeutig identifiziert ist.
        /// </summary>
        /// <param name="messageId">Die Id der Nachricht.</param>
        /// <returns>Liefert ein Objekt der Klasse ConversationMessage zurück.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public ConversationMessage GetConversationMessage(int messageId)
        {
            ConversationMessage convMessage = null;
            try
            {
                convMessage = groupDBManager.GetConversationMessage(messageId);
            } 
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetConversationMessage: Failed to get the conversation message with the specified id {0}.", messageId);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return convMessage;
        }

        /// <summary>
        /// Ermittelte die Anzahl an ungelesenen Konversationsnachrichten für die Gruppe,
        /// die durch die angegebene Id eindeutig identifiziert ist. Gibt ein Verzeichnis zurück, indem mit der Konversations-Id
        /// als Schlüssel die Anzahl an ungelesenen Konversationsnachrichten ermittelt werden kann.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die die Informationen abgerufen werden sollen.</param>
        /// <returns>Verzeichnis, indem die Anzahl an ungelesenen ConversationMessages für jede Konversation 
        ///     gespeichert ist. Die Konversationen-Id dient als Schlüssel.</returns>
        public Dictionary<int, int> GetAmountOfUnreadConversationMessagesForGroup(int groupId)
        {
            Dictionary<int, int> unreadMsgDictionary = null;
            try
            {
                unreadMsgDictionary = groupDBManager.DetermineAmountOfUnreadConvMsgForGroup(groupId);
            }
            catch (DatabaseException ex)
            {
                // Gebe DatabaseException nicht an Aufrufer weiter.
                // Seite kann ohne diese Information angezeigt werden.
                Debug.WriteLine("Could not retrieve amount of unread conversation messages for group with id {0}. " + 
                    "Message is {1}.", groupId, ex.Message);
            }
            return unreadMsgDictionary;
        }

        /// <summary>
        /// Markiert die Konversationsnachrichten der angegebenen Konversation als gelesen.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, für die die Nachrichten als
        ///     gelesen markiert werden sollen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Markierung fehlschlägt.</exception>
        public void MarkConversationMessagesAsRead(int conversationId)
        {
            try
            {
                groupDBManager.MarkConversationMessagesAsRead(conversationId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("MarkConversationMessagesAsRead: Couldn't mark messages as read.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Bestimmt die aktuell höchste Nachrichtennummer, die einer Nachricht der angegebenen Konversation in den lokalen Datensätzen
        /// zugeordnet ist. 
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, für die die höchste Nachrichtennummer ermittelt werden soll.</param>
        /// <returns>Die aktuell höchste Nachrichtennummer.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abfruf fehlschlägt.</exception>
        public int GetHighestMessageNumberOfConversation(int conversationId)
        {
            int highestNumber = 0;

            try
            {
                highestNumber = groupDBManager.GetHighestConversationMessageNumber(conversationId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetHighestMessageNumberOfConversation: The highest message number could not be extracted.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return highestNumber;
        }

        /// <summary>
        /// Löscht alle Konversationsnachrichten der angegebenen Konversation.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        public void DeleteConversationMessages(int conversationId)
        {
            try
            {
                groupDBManager.DeleteConversationMessages(conversationId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteConversationMessages: Failed to delete messages of conversation with id {0}.", conversationId);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Prüft, ob es für die angegebene Konversation Nachrichten gibt, die keine
        /// gültige Autorenreferenz besitzen.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        /// <returns>Liefert true, wenn solche Nachrichten exisitieren, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Prüfung fehlschlägt.</exception>
        public bool HasUnresolvedAuthors(int conversationId)
        {
            bool hasUnresolvedAuthors = false;

            try
            {
                hasUnresolvedAuthors = groupDBManager.HasUnresolvedAuthors(conversationId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("HasUnresolvedAuthors: Failed to dermine whether conversation has messages with unresolved authors.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return hasUnresolvedAuthors;
        }
        #endregion LocalConversationMethods

        #region LocalBallotMethods

        /// <summary>
        /// Führt eine Synchronisierung der lokalen Abstimmungsoptionen durch, die zu der
        /// angegebenen Abstimmung gehören. Die Abstimmungsoptionen werden gegen die übergebene
        /// Referenzliste synchronisiert. 
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen gehören.</param>
        /// <param name="referenceOptions">Die Liste von Abstimmungsoptionen, die als Referenzwert dienen, und gegen die
        ///     synchronisiert wird.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisierung fehlschlägt.</exception>
        /// <exception cref="ArgumentNullException">Wirft ArgumentNullException, wenn Liste null ist.</exception>
        public void SynchronizeLocalOptionsOfBallot(int ballotId, List<Option> referenceOptions)
        {
            if (referenceOptions == null)
            {
                Debug.WriteLine("SynchronizeLocalOptionsOfBallot: Invalid parameter. referenceList is null.");
                throw new ArgumentNullException("referenceList is null");
            }

            // Frage alle lokalen Optionen dieser Abstimmung ab.
            List<Option> localOptions = GetOptions(ballotId, false);

            List<Option> updatabaleOptions = new List<Option>();
            List<Option> newOptions = new List<Option>();

            foreach (Option referenceOption in referenceOptions)
            {
                bool isContained = false;

                foreach (Option localOption in localOptions)
                {
                    if (referenceOption.Id == localOption.Id)
                    {
                        isContained = true;

                        updatabaleOptions.Add(referenceOption);
                    }
                }

                if (!isContained)
                {
                    newOptions.Add(referenceOption);
                }
            }

            // Speichere neue Optionen ab und aktualisiere bestehende Optionen.
            UpdateOptions(updatabaleOptions);
            StoreOptions(ballotId, newOptions);                

            // Entferne Optionen, die auf dem Server nicht mehr gelistet werden.
            foreach (Option localOption in localOptions)
            {
                bool isContained = false;

                foreach (Option referenceOption in referenceOptions)
                {
                    if (localOption.Id == referenceOption.Id)
                    {
                        isContained = true;
                    }
                }

                if (!isContained)
                {
                    DeleteOption(localOption.Id);
                }
            }
        }

        /// <summary>
        /// Führt eine Synchronisation der lokal gespeicherten Votes für eine Abstimmungsoption durch. Die
        /// Votes werden gegen die übergebene Referenzliste synchronisiert.
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoption gehört.</param>
        /// <param name="optionId">Die Id der Abstimmungsoption für die die Votes synchronisiert werden soll.</param>
        /// <param name="referenceVotes">Die Referenzliste an Votes, gegen die die Synchronisation durchgeführt wird.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisiation fehlschlägt.</exception>
        public void SynchronizeLocalVotesForOption(int ballotId, int optionId, List<int> referenceVotes)
        {
            if (referenceVotes == null)
            {
                Debug.WriteLine("SynchronizeLocalVotesForOption: No valid data passed to the method.");
                throw new ArgumentNullException("reference list is null.");
            }

            // Frage zunächst die Votes ab für diese Abstimmungsoption.
            List<User> localVoters = GetVotersForOption(optionId);

            // Prüfe, ob neue Votes hinzugekommen sind.
            foreach (int referenceVote in referenceVotes)
            {
                bool isContained = false;

                foreach (User localVoter in localVoters)
                {
                    if (localVoter.Id == referenceVote)
                    {
                        isContained = true;
                    }
                }

                if (!isContained)
                {
                    Debug.WriteLine("SynchronizeLocalVotesForOption: Need to add vote for option with id {0} " + 
                        "the added vote is {1}.", optionId, referenceVote);
                    AddVote(ballotId, optionId, referenceVote);
                }
            }

            // Prüfe, ob Votes entfernt wurden.
            foreach (User localVoter in localVoters)
            {
                bool isContained = false;

                foreach (int referenceVote in referenceVotes)
                {
                    if (referenceVote == localVoter.Id)
                    {
                        isContained = true;
                    }
                }

                if (!isContained)
                {
                    Debug.WriteLine("SynchronizeLocalVotesForOption: Need to remove vote for option with id {0} " + 
                        "the remove vote is {1}.", optionId, localVoter.Id);
                    RemoveVote(optionId, localVoter.Id);
                }
            }
        }

        /// <summary>
        /// Speichert die Abstimmung in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, der die Abstimmung zugeordnet ist.</param>
        /// <param name="ballot">Die zu speichernde Abstimmung in Form eines Ballot Objekts.</param>
        /// <returns>Liefert true, falls Speicherung erfolgreich, liefert false, falls Speicherung aufgrund fehlender
        ///     Referenzen (z.B. fehlender Nutzerdatensatz in der Datenbank) nicht ausgeführt werden konnte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn bei der Speicherung ein unerwarteter Fehler auftritt.</exception>
        public bool StoreBallot(int groupId, Ballot ballot)
        {
            bool insertedSuccessfully = false;
            bool fulfillsConstraints = true;

            try
            {
                if (groupDBManager.IsGroupStored(groupId) && !groupDBManager.IsBallotStored(ballot.Id))
                {
                    if (!userController.IsUserLocallyStored(ballot.AdminId))
                    {
                        Debug.WriteLine("StoreBallot: The ballot's admin (id: {0}) is not locally stored. Cannot continue.", ballot.AdminId);
                        fulfillsConstraints = false;
                    }

                    if (ballot.Options != null)
                    {
                        Dictionary<int, bool> userStoredStatus = new Dictionary<int, bool>();

                        foreach (Option option in ballot.Options)
                        {
                            // Falls Voter gesetzt, so müssen diese geprüft werden.
                            if (option.VoterIds != null)
                            {
                                foreach (int voter in option.VoterIds)
                                {
                                    if (!userStoredStatus.ContainsKey(voter))
                                    {
                                        userStoredStatus.Add(voter, userController.IsUserLocallyStored(voter));
                                    }
                                }
                            }
                        }

                        // Prüfe, ob alle Bedingungen für Insert erfüllt sind.
                        foreach (bool status in userStoredStatus.Values)
                        {
                            if (!status)
                            {
                                Debug.WriteLine("StoreBallot: There are missing users. Cannot insert ballot.");
                                fulfillsConstraints = false;
                            }
                        }
                    }

                    if (fulfillsConstraints)
                    {
                        // Speichere Abstimmung ab.
                        groupDBManager.StoreBallot(groupId, ballot);
                        insertedSuccessfully = true;
                    }

                }
                else
                {
                    Debug.WriteLine("StoreBallot: Is Ballot already stored?");
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreBallot: Storing failed. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            Debug.WriteLine("StoreBallot: Returns {0}.", insertedSuccessfully);
            return insertedSuccessfully;
        }

        /// <summary>
        /// Speichert eine Menge von Abstimmungen, die zu derselben Gruppe gehören, in den lokalen Datensätzen ab. Diese Methode
        /// führt keine nennenswerten Prüfungen vor der Einfügeoperation durch. Inkonsistente oder
        /// widersprüchliche Datensätze können daher zu einem Abbruch der Methode führen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, der die Abstimmungen zugeordnet werden.</param>
        /// <param name="ballots">Liste von Abstimmungen, die lokal gespeichert werden sollen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public void StoreBallots(int groupId, List<Ballot> ballots)
        {
            try
            {
                if (groupDBManager.IsGroupStored(groupId))
                {
                    groupDBManager.BulkInsertBallots(groupId, ballots);
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreBallots: Storing failed. Message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Fragt die Abstimmungen aus den lokalen Datensätzen ab, die der Gruppe mit der spezifizierten Id zugeordnet sind.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, der die Abstimmungen zugeordnet sind.</param>
        /// <param name="includingSubresources">Gibt an, ob die Subressourcen (Options und Votes) ebenfalls abgerufen werden sollen.</param>
        /// <returns>Eine Liste von Objekten des Typs Ballot.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<Ballot> GetBallots(int groupId, bool includingSubresources)
        {
            List<Ballot> ballots = null;
            try
            {
                ballots = groupDBManager.GetBallots(groupId, includingSubresources);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetBallots: Failed to retrieve ballots from local datasets.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return ballots;
        }

        /// <summary>
        /// Liefert die Abstimmung zurück, die durch die angegebene Id eindeutig identifiziert ist.
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung. </param>
        /// <param name="includingSubresources">Gibt an, ob Subressourcen der Abstimmung enthalten sein sollen.</param>
        /// <returns>Eine Instanz der Klasse Ballot.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abstimmung nicht abgerufen werden konnte.</exception>
        public Ballot GetBallot(int ballotId, bool includingSubresources)
        {
            Ballot ballot = null;
            try
            {
                ballot = groupDBManager.GetBallot(ballotId, includingSubresources);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetBallot: Failed to retrieve ballot from local datasets.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return ballot;
        }

        /// <summary>
        /// Aktualisiert die lokal gespeicherten Datensätze der übergebenen Abstimmung.
        /// Gibt an, ob die Aktualisierung erfolgreich war, oder ob fehlende Referenzen (z.B. 
        /// der lokale Datensatz des Administrators) eine Aktualisierung verhindert haben.
        /// Methode aktualisisert nur die Abstimmungsdaten selbst, Subressourcen werden nicht aktualisiert,
        /// d.h. keine Optionen und Abstimmungsergebnisse werden aktualisiert.
        /// </summary>
        /// <param name="newBallot">Das Objekt mit den neuen Abstimmungsdaten.</param>
        /// <returns>Liefert true, wenn die Aktualisierung erfolgreich war, false, wenn die
        ///     Aktualisierung aufgrund fehlender Referenzen nicht durchgeführt werden konnte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlgeschlagen ist.</exception>
        public bool UpdateBallot(Ballot newBallot)
        {
            bool updatedSuccessfully = false;

            try
            {
                if (newBallot != null && userController.IsUserLocallyStored(newBallot.AdminId)
                    && groupDBManager.IsBallotStored(newBallot.Id))
                {
                    groupDBManager.UpdateBallot(newBallot);
                    updatedSuccessfully = true;
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("UpdateBallot: Failed to update ballot with id {0} in local datasets.", newBallot.Id);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return updatedSuccessfully;
        }

        /// <summary>
        /// Lösche die Abstimmung mit der angegebenen Id aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="ballotId">Die Abstimmung mit der angegebenen Id.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschung nicht ausgeführt werden konnte.</exception>
        public void DeleteBallot(int ballotId)
        {
            try
            {
                groupDBManager.DeleteBallot(ballotId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteBallot: Failed to delete ballot with id {0} in local datasets.", ballotId);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Liefert die Abstimmungsressourcen, die der angegebenen Abstimmung zugeordnet sind. 
        /// Die Abstimmungsoptionen können inklusiver ihrer zugeordneten Subressourcen (Votes) 
        /// abgerufen werden. 
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung zu der die Abstimmungsoptionen abgefragt werden sollen.</param>
        /// <param name="includingSubressources">Gibt an, ob die </param>
        /// <returns>Liefert Liste von Objekten der Klasse Option. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abfruf fehlschlägt.</exception>
        public List<Option> GetOptions(int ballotId, bool includingSubressources)
        {
            List<Option> options = null;
            try
            {
                options = groupDBManager.GetOptions(ballotId, includingSubressources);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetOptions: Failed to retrieve options for the ballot with id {0}.", ballotId);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return options;
        }

        /// <summary>
        /// Aktualisiert die lokalen Datensätze der übergebenen Abstimmungsoptionen.
        /// </summary>
        /// <param name="options">Die Liste von Abstimmungsoptionen mit aktualisierten Datensätzen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateOptions(List<Option> options)
        {
            try
            {
                groupDBManager.UpdateOptions(options);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("UpdateOptions: Failed to update the given options.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Speichert eine Menge von Abstimmungsoptionen, die zu einer gegebenen Abstimmungsoption gehören
        /// in den lokalen Datensätzen ab. Die Abstimmungsoptionen werden ohne Daten bezüglich Subressourcen (Votes) 
        /// abgespeichert, d.h. rein die Daten der Klasse Option (Id und Text).
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen gehören.</param>
        /// <param name="options">Die Liste an abzuspeichernden Datensätzen.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public void StoreOptions(int ballotId, List<Option> options)
        {
            try
            {
                groupDBManager.BulkInsertOptions(ballotId, options, false);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreOptions: Failed to store the given options.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Speichert die übergebene Abstimmungsoption in den lokalen Datensätzen.
        /// Die übergebene Abstimmungsoption kann inklusive Informationen über die abgegebenen
        /// Stimmen gespeichert werden.
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung zu der die Abstimmungsoption gehört.</param>
        /// <param name="option">Das Objekt vom Typ Option mit den Daten der Abstimmungsoption.</param>
        /// <param name="includingVoters">Gibt an, ob die Informationen über die abgegebenen Stimmen ebenfalls
        ///     gespeichert werden sollen.</param>
        /// <returns>Liefert true, wenn Speicherung erfolgreich. Liefert false, wenn Option nicht gespeichert
        ///     werden konnte aufgrund fehlender Abhängigkeiten.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public bool StoreOption(int ballotId, Option option, bool includingVoters)
        {
            bool successful = false;
            try
            {
                if (includingVoters)
                {
                    if (option != null && option.VoterIds != null)
                    {
                        foreach (int voter in option.VoterIds)
                        {
                            if (!userController.IsUserLocallyStored(voter))
                            {
                                // Abbruch, da Einfügeoperation sonst fehlschlägt.
                                return false;
                            }
                        }
                    }
                }

                if (groupDBManager.IsBallotStored(ballotId))
                {
                    groupDBManager.InsertOption(ballotId, option, includingVoters);
                    successful = true;
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreOption: Failed to store the given option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return successful;
        }

        /// <summary>
        /// Speichert die übergebene Menge an Abstimmungsoption für die angegebene Abstimmung ab.
        /// Die Optionen werden einschließlich Subressourcen (Votes) abgespeichert. Bei Aufruf sollte
        /// man sicherstellen, dass alle Nutzer, die abgestimmt haben, auch bereits lokal als Datensätze in der 
        /// Datenbank vorhanden sind. Ist das nicht der Fall fehlen notwendige Abhängikeiten.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, zu der die Abstimmungsoptionen gespeichert werden sollen.</param>
        /// <param name="options">Die Liste an Objekten der Klasse Option.</param>
        /// <returns>Liefert true, falls die Speicherung erfolgreich war, und false, falls die Speicherung aufgrund
        ///     fehlender Abhängigkeiten nicht durchgeführt werden konnte.</returns>
        /// <exception cref="ClientException">Wirft ClientException, falls Speicherung fehlschlägt. </exception>
        public bool StoreOptionsIncludingVoters(int groupId, int ballotId, List<Option> options)
        {
            bool storedSuccessfully = false;
            bool fulfillsConstraints = true;

            try
            {
                if (groupDBManager.IsBallotStored(ballotId))
                {
                    // Frage Gruppenteilnehmer ab.
                    Dictionary<int, User> participants = groupDBManager.GetAllParticipantsOfGroup(groupId);

                    foreach (Option option in options)
                    {
                        if (option.VoterIds != null)
                        {
                            foreach (int voter in option.VoterIds)
                            {
                                if (!participants.ContainsKey(voter))
                                {
                                    Debug.WriteLine("StoreOptions: Voter with id {0} seems not to be stored, cannot continue.", voter);
                                    fulfillsConstraints = false;
                                }
                            }
                        }
                    }

                    if (fulfillsConstraints)
                    {
                        groupDBManager.BulkInsertOptions(ballotId, options, true);
                        storedSuccessfully = true;
                    }
                }
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreOptions: Failed to store the given options.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return storedSuccessfully;
        }

        /// <summary>
        /// Löscht die Abstimmungsoption mit der angegebenen Id aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="optionId">Die Id der Abstimmunsoption, die gelöscht werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Löschvorgang fehlschlagen sollte.</exception>
        public void DeleteOption(int optionId)
        {
            try
            {
                groupDBManager.DeleteOption(optionId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("DeleteOption: Failed to delete the specified option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Gibt alle Nutzer zurück, die für die angegebenen Abstimmungsoption abgestimmt haben.
        /// </summary>
        /// <param name="optionId">Die Id der Abstimmungsoption.</param>
        /// <returns>Liste von User Objekten, die alle Nutzer beinhaltet, die für die Abstimmungsoption gestimmt haben.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<User> GetVotersForOption(int optionId)
        {
            List<User> voters = null;
            try
            {
                voters = groupDBManager.GetVotersForOption(optionId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetVotersForOption: Failed to retrieve the voters for the option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return voters;
        }

        /// <summary>
        /// Ruft die Abstimmungsoptionen ab, für die der angegebene Nutzer in der 
        /// Abstimmung abgestimmt hat.
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung.</param>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Eine Liste von Instanzen der Klasse Option. Die Liste kann auch leer sein.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public List<Option> GetSelectedOptionsInBallot(int ballotId, int userId)
        {
            List<Option> selectedOptions = null;
            try
            {
                selectedOptions = groupDBManager.GetSelectedOptionsInBallot(ballotId, userId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetSelectedOptionsInBallot: Failed to retrieve selected options.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return selectedOptions;
        }

        /// <summary>
        /// Fügt eine Verknüfpung von Nutzer-Id zu der angegebenen Abstimmungsoption den lokalen
        /// Datensätzen hinzu. Der angegebene Nutzer hat dann für diese Abstimmungsoption abgestimmt.
        /// </summary>
        /// <param name="optionId">Die Id der Abstimmungsoption, für die der Nutzer abstimmt.</param>
        /// <param name="userId">Die Id des entsprechenden Nutzers.</param>
        /// <returns>Liefert true, wenn Aktion erfolgreich ausgeführt wurde. Liefert false, 
        ///     wenn eine fehlende Referenz (z.B. betroffener Nutzer nicht lokal gespeichert) die Ausführung verhindert.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public bool AddVote(int ballotId, int optionId, int userId)
        {
            bool successful = false;
            Ballot affectedBallot = GetBallot(ballotId, false);

            try
            {
                if (userController.IsUserLocallyStored(userId))
                {
                    if (affectedBallot.IsMultipleChoice.HasValue && affectedBallot.IsMultipleChoice == false)
                    {
                        if (groupDBManager.HasVotedForBallot(ballotId, userId))
                        {
                            Debug.WriteLine("AddVote: Ballot not multiple choice and user has already voted.");
                            Debug.WriteLine("AddVote: Deleting all votes before adding the new one.");
                            groupDBManager.RemoveAllVotesForBallot(ballotId, userId);
                        }
                    }

                    if (!groupDBManager.HasVotedForOption(optionId, userId))
                    {
                        groupDBManager.AddVote(optionId, userId);
                        successful = true;
                    }
                }
                else
                {
                    Debug.WriteLine("AddVote: Failed. No such user stored in database.");
                }                
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("AddVote: Failed to add vote for the specified option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return successful;
        }

        /// <summary>
        /// Entfernt die Verknüpfung zwischen dem Nutzer und der angegebenen Abstimmungsoption.
        /// Der Nutzer hat dann nicht mehr für diese Abstimmungsoption abgestimmt.
        /// </summary>
        /// <param name="optionId">Die Id der Abstimmungsoption.</param>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <exception cref="ClientException">Wirft ClientException, falls Löschung fehlschlägt.</exception>
        public void RemoveVote(int optionId, int userId)
        {
            try
            {
                groupDBManager.DeleteVote(optionId, userId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("RemoveVote: Failed to remove vote for the specified option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Gibt an, ob der Nutzer, der durch die angegeben Id identifiziert wird, bereits
        /// für die angegebene Abstimmung abgestimmt hat.
        /// </summary>
        /// <param name="ballotId">Die Id der Abstimmung.</param>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Liefert true, wenn der Nutzer bereits abgestimmt hat, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Prüfung fehlschlägt.</exception>
        public bool HasPlacedVoteInBallot(int ballotId, int userId)
        {
            bool hasVoted = false;

            try
            {
                hasVoted = groupDBManager.HasVotedForBallot(ballotId, userId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("HasPlacedVoteInBallot: Failed to check voting status.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return hasVoted;
        }

        /// <summary>
        /// Gibt an, ob der Nutzer für die angegebene Abstimmungsoption abgestimmt hat.
        /// </summary>
        /// <param name="optionId">Die Id der Abstimmungsoption.</param>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Liefert true, wenn der Nutzer für diese Abstimmungsoption abgestimmt hat, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Prüfung fehlschlägt.</exception>
        public bool HasPlacedVoteForOption(int optionId, int userId)
        {
            bool hasVoted = false;

            try
            {
                hasVoted = groupDBManager.HasVotedForOption(optionId, userId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("HasPlacedVoteForOption: Failed to check voting status for option.");
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return hasVoted;
        }
        #endregion LocalBallotMethods
    }
}
