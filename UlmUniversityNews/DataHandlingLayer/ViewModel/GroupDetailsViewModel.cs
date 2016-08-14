using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class GroupDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der GroupController Klasse.
        /// </summary>
        private GroupController groupController;

        /// <summary>
        /// Eine Referenz auf den lokalen Nutzer.
        /// </summary>
        private User localUser;
        #endregion Fields

        #region Properties
        private int selectedPivotItemIndex;
        /// <summary>
        /// Repräsentiert den Index des Pivotelements, welches gerade aktiv ist.
        /// </summary>
        public int SelectedPivotItemIndex
        {
            get { return selectedPivotItemIndex; }
            set
            {
                selectedPivotItemIndex = value;
                checkCommandExecution();
            }
        }

        private string selectedPitvotItemName;
        /// <summary>
        /// Der Name des gwählten Pivotelements.
        /// </summary>
        public string SelectedPivotItemName
        {
            get { return selectedPitvotItemName; }
            set
            {
                selectedPitvotItemName = value;
                checkCommandExecution();
            }
        }
        
        private Group selectedGroup;
        /// <summary>
        /// Die gewählte Gruppe, zu der Details angezeigt werden sollen.
        /// </summary>
        public Group SelectedGroup
        {
            get { return selectedGroup; }
            set { this.setProperty(ref this.selectedGroup, value); }
        }

        private bool groupParticipant;
        /// <summary>
        /// Gibt an, ob der Nutzer ein Teilnehmer der geladenen Gruppe ist.
        /// </summary>
        public bool IsGroupParticipant
        {
            get { return groupParticipant; }
            set { this.setProperty(ref this.groupParticipant, value); }
        }

        private bool removedFromGroup;
        /// <summary>
        /// Gibt an, ob der Nutzer von der Gruppe entfernt wurde. In diesem
        /// Fall ist der Nutzer nicht selbstständig ausgetreten, sondern wurde
        /// vom Gruppenadministrator entfernt. Dieser Fall muss dem Nutzer angezeigt werden.
        /// </summary>
        public bool IsRemovedFromGroup
        {
            get { return removedFromGroup; }
            set { this.setProperty(ref this.removedFromGroup, value); }
        }

        private bool hasLeaveOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglichkeit hat die Gruppe zu verlassen.
        /// </summary>
        public bool HasLeaveOption
        {
            get { return hasLeaveOption; }
            set { this.setProperty(ref this.hasLeaveOption, value); }
        }

        private bool hasDeleteOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglichkeit hat, die Gruppe zu löschen.
        /// </summary>
        public bool HasDeleteOption
        {
            get { return hasDeleteOption; }
            set { this.setProperty(ref this.hasDeleteOption, value); }
        }

        private bool hasDeleteLocallyOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglichkeit hat, die Gruppe lokal zu löschen.
        /// </summary>
        public bool HasDeleteLocallyOption
        {
            get { return hasDeleteLocallyOption; }
            set { this.setProperty(ref this.hasDeleteLocallyOption, value); }
        }

        private bool isDeletionOrRemovalNotificationShown;
        /// <summary>
        /// Gibt an, ob der Hinweis bezüglich einer Löschung der Gruppe oder 
        /// bezüglich der Entfernung des lokalen Nutzer aus der Gruppe durch den Admin 
        /// aktuell angezeigt wird.
        /// </summary>
        public bool IsDeletionOrRemovalNotificationShown
        {
            get { return isDeletionOrRemovalNotificationShown; }
            set { this.setProperty(ref this.isDeletionOrRemovalNotificationShown, value); }
        }

        private string enteredPassword;
        /// <summary>
        /// Das vom Nutzer eingegebene Passwort.
        /// </summary>
        public string EnteredPassword
        {
            get { return enteredPassword; }
            set { enteredPassword = value; }
        }

        private ObservableCollection<Conversation> conversationCollection;
        /// <summary>
        /// Eine Menge von Conversation Objekten, die dieser Gruppe zugeordnet sind.
        /// </summary>
        public ObservableCollection<Conversation> ConversationCollection
        {
            get { return conversationCollection; }
            set { this.setProperty(ref this.conversationCollection, value); }
        }

        private ObservableCollection<Ballot> ballotCollection;
        /// <summary>
        /// Eine Menge von Ballot Objekten, die dieser Gruppe zugeordnet sind.
        /// </summary>
        public ObservableCollection<Ballot> BallotCollection
        {
            get { return ballotCollection; }
            set { this.setProperty(ref this.ballotCollection, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand joinGroupCommand;
        /// <summary>
        /// Befehl, der den Prozess zum Beitritt zu einer Grupper anstößt.
        /// </summary>
        public AsyncRelayCommand JoinGroupCommand
        {
            get { return joinGroupCommand; }
            set { joinGroupCommand = value; }
        }

        private AsyncRelayCommand leaveGroupCommand;
        /// <summary>
        /// Befehl, der genutzt werden kann, um aus einer Gruppe auszutreten.
        /// </summary>
        public AsyncRelayCommand LeaveGroupCommand
        {
            get { return leaveGroupCommand; }
            set { leaveGroupCommand = value; }
        }

        private RelayCommand editGroupCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Bearbeitungsdialog für die Gruppe.
        /// </summary>
        public RelayCommand EditGroupCommand
        {
            get { return editGroupCommand; }
            set { editGroupCommand = value; }
        }

        private RelayCommand conversationSelectedCommand;
        /// <summary>
        /// Befehl, der ausgeführt wird, sobald eine Konversation angewählt wurde.
        /// Wechsel auf die Detailansicht der Konversation.
        /// </summary>
        public RelayCommand ConversationSelectedCommand
        {
            get { return conversationSelectedCommand; }
            set { conversationSelectedCommand = value; }
        }

        private AsyncRelayCommand synchronizeDataCommand;
        /// <summary>
        /// Befehl zum Synchronisieren der angezeigten Daten mit dem Server.
        /// </summary>
        public AsyncRelayCommand SynchronizeDataCommand
        {
            get { return synchronizeDataCommand; }
            set { synchronizeDataCommand = value; }
        }

        private AsyncRelayCommand deleteGroupCommand;
        /// <summary>
        /// Befehl zum Löschen der Gruppe.
        /// </summary>
        public AsyncRelayCommand DeleteGroupCommand
        {
            get { return deleteGroupCommand; }
            set { deleteGroupCommand = value; }
        }

        private RelayCommand deleteGroupLocallyCommand;
        // Befehl zum Löschen der Gruppe aus den lokalen Datensätzen.
        public RelayCommand DeleteGroupLocallyCommand
        {
            get { return deleteGroupLocallyCommand; }
            set { deleteGroupLocallyCommand = value; }
        }

        private RelayCommand changeSettingsCommand;
        /// <summary>
        /// Befehl zum Ändern der Gruppeneinstellungen (bezüglich Benachrichtigungen).
        /// </summary>
        public RelayCommand ChangeToGroupSettingsCommand
        {
            get { return changeSettingsCommand; }
            set { changeSettingsCommand = value; }
        }

        private RelayCommand changeToAddConversationDialog;
        /// <summary>
        /// Befehl zum Wechsel auf den Dialog zum Hinzufügen einer neuen Konversation.
        /// </summary>
        public RelayCommand ChangeToAddConversationDialog
        {
            get { return changeToAddConversationDialog; }
            set { changeToAddConversationDialog = value; }
        }

        private RelayCommand ballotSelectedCommand;
        /// <summary>
        /// Befehl, der ausgeführt wird sobald eine Abstimmung in der View gewählt wurde.
        /// </summary>
        public RelayCommand BallotSelectedCommand
        {
            get { return ballotSelectedCommand; }
            set { ballotSelectedCommand = value; }
        }

        private RelayCommand switchToCreateBallotDialogCommand;
        /// <summary>
        /// Befehl zum Wechsel auf den Dialog für die Erstellung einer neuen Abstimmung.
        /// </summary>
        public RelayCommand SwitchToCreateBallotDialogCommand
        {
            get { return switchToCreateBallotDialogCommand; }
            set { switchToCreateBallotDialogCommand = value; }
        }

        private RelayCommand swichToGroupParticipantsViewCommand;
        /// <summary>
        /// Befehl zum Wechsel auf die Verwaltungsseite für Gruppenteilnehmer.
        /// </summary>
        public RelayCommand SwichToGroupParticipantsViewCommand
        {
            get { return swichToGroupParticipantsViewCommand; }
            set { swichToGroupParticipantsViewCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public GroupDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            // Erzeuge Controller-Instanz.
            groupController = new GroupController(this);

            IsGroupParticipant = false;
            HasLeaveOption = false;

            localUser = groupController.GetLocalUser();

            if (BallotCollection == null)
                BallotCollection = new ObservableCollection<Ballot>();

            if (ConversationCollection == null)
                ConversationCollection = new ObservableCollection<Conversation>();

            // Erzeuge Befehle.
            JoinGroupCommand = new AsyncRelayCommand(
                param => executeJoinGroupCommandAsync(),
                param => canJoinGroup());
            LeaveGroupCommand = new AsyncRelayCommand(
                param => executeLeaveGroupCommandAsync(),
                param => canLeaveGroup());
            EditGroupCommand = new RelayCommand(
                param => executeEditGroupCommand(),
                param => canEditGroup());
            ConversationSelectedCommand = new RelayCommand(
                param => executeConversationSelectedCommand(param));
            SynchronizeDataCommand = new AsyncRelayCommand(
                param => executeSynchronizeDataCommandAsync(),
                param => canSynchronizeData());
            DeleteGroupCommand = new AsyncRelayCommand(
                param => executeDeleteGroupAsync(),
                param => canDeleteGroup());
            DeleteGroupLocallyCommand = new RelayCommand(
                param => executeDeleteGroupLocallyCommand(),
                param => canDeleteGroupLocally());
            ChangeToGroupSettingsCommand = new RelayCommand(
                param => executeChangeToGroupSettingsCommand(),
                param => canChangeToGroupSettings());
            ChangeToAddConversationDialog = new RelayCommand(
                param => executeChangeToAddConversationDialog(),
                param => canChangeToAddConversationDialog());
            BallotSelectedCommand = new RelayCommand(
                param => executeBallotSectedCommand(param));
            SwitchToCreateBallotDialogCommand = new RelayCommand(
                param => executeSwitchToCreateBallotDialogCommand(),
                param => canSwitchToCreateBallotDialog());
            SwichToGroupParticipantsViewCommand = new RelayCommand(
                param => executeSwitchToGroupParticipantsView(),
                param => canSwitchToGroupParticipantsView());
        }

        /// <summary>
        /// Lädt eine Gruppe aus dem temporären Verzeichnis unter Verwendung des übergebenen 
        /// Schlüssel. Die Gruppe wird zur Anzeige in der View geladen. Es wird geprüft, ob
        /// der Nutzer bereits Teilnehmer der Gruppe ist. 
        /// </summary>
        /// <param name="key">Der Schlüssel, der für den Abruf verwendet wird.</param>
        public async Task LoadGroupFromTemporaryCacheAsync(string key)
        {
            Group loadedGroup = await Common.TemporaryCacheManager.RetrieveObjectFromTmpCacheAsync<Group>(key);

            Debug.WriteLine("Tested group loading from temporary dictionary.");
            
            if (loadedGroup != null)
            {
                Debug.WriteLine("Loaded group is: " + loadedGroup.Name);

                try
                {
                    if (groupController.GetGroup(loadedGroup.Id) != null)
                    {
                        Debug.WriteLine("The group exists in the local datasets.");
                        IsGroupParticipant = true;

                        if (groupController.IsActiveParticipant(loadedGroup.Id, groupController.GetLocalUser().Id))
                        {
                            Debug.WriteLine("User seems to be an active participant of the group.");
                            IsRemovedFromGroup = false;
                        }
                        else
                        {
                            Debug.WriteLine("User seems to be an inactive participant of the group.");
                            IsRemovedFromGroup = true;
                        }

                        // Frage noch die Teilnehmer ab.
                        List<User> participants = await Task.Run(() => groupController.GetActiveParticipantsOfGroup(loadedGroup.Id));
                        loadedGroup.Participants = participants;

                        // Lade die Konversationen und Abstimmungen.
                        await LoadConversationsAsync(loadedGroup.Id);
                        await LoadBallotsAsync(loadedGroup.Id);
                    }
                    else
                    {
                        Debug.WriteLine("User is not an active participant of the group.");
                        IsGroupParticipant = false; // Nicht Teilnehmer dieser Gruppe.
                    }
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("LoadGroupFromTemporaryCache: Execution failed.");
                    displayError(ex.ErrorCode);
                }

                // Setze die geladenen Daten.
                SelectedGroup = loadedGroup;

                checkCommandExecution();
            }
        }

        /// <summary>
        /// Lade Gruppe aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der zu ladenden Gruppe.</param>
        public async Task LoadGroupFromLocalStorageAsync(int groupId)
        {
            IsGroupParticipant = true;      // Wenn Gruppe im lokalen Speicher, dann gilt das.
            Debug.WriteLine("LoadGroupFromLocalStorageAsync: Load group from local datasets.");

            try
            {
                Group loadedGroup = await Task.Run(() => groupController.GetGroup(groupId));

                SelectedGroup = loadedGroup;  
                
                if (SelectedGroup != null)
                {
                    if (groupController.IsActiveParticipant(loadedGroup.Id, groupController.GetLocalUser().Id))
                    {
                        IsRemovedFromGroup = false;
                        Debug.WriteLine("LoadGroupFromLocalStorageAsync: local user still active in this group.");

                        if (SelectedGroup.Deleted)
                        {
                            Debug.WriteLine("LoadGroupFromLocalStorageAsync: Group is marked as deleted.");
                            displayDeletionNotification();
                        }
                    }
                    else
                    {
                        // Setze nur IsRemoveFromGroup, wenn Gruppe nicht sowieso schon gelöscht ist.
                        if (!SelectedGroup.Deleted)
                        {
                            IsRemovedFromGroup = true;
                            Debug.WriteLine("LoadGroupFromLocalStorageAsync: local user seems to be removed from this group.");
                            displayRemovedFromGroupNotification();
                        }
                        else
                        {
                            Debug.WriteLine("LoadGroupFromLocalStorageAsync: Group is already deleted.");
                            displayDeletionNotification();
                        }
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadGroupFromLocalStorageAsync: Execution failed.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        /// <summary>
        /// Lade die Konversationen der Gruppe.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        public async Task LoadConversationsAsync(int groupId)
        {
            try
            {
                // Lade die Konversationen aus der Datenbank.
                List<Conversation> conversations = await Task.Run(() => groupController.GetConversations(groupId));
                if (conversations != null)
                {
                    // Sortieren.
                    conversations = sortConversationsByApplicationSettings(conversations);

                    if (ConversationCollection == null)
                    {
                        ConversationCollection = new ObservableCollection<Conversation>();
                    }

                    foreach (Conversation conversation in conversations)
                    {
                        if (conversation != null)
                        {
                            ConversationCollection.Add(conversation);
                        }
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadConversationsAsync: Execution failed.");
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Lade die Abstimmungen der Gruppe.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        public async Task LoadBallotsAsync(int groupId)
        {
            try
            {
                // Lade die lokal gespeicherten Abstimmungen.
                List<Ballot> ballots = await Task.Run(() => groupController.GetBallots(groupId, false));
                if (ballots != null)
                {
                    // Sortieren.
                    ballots = sortBallotsByApplicationSettings(ballots); 

                    if (BallotCollection == null)
                    {
                        BallotCollection = new ObservableCollection<Ballot>();
                    }                        

                    foreach (Ballot ballot in ballots)
                    {
                        if (ballot != null)
                        {
                            BallotCollection.Add(ballot);
                        }
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadBallotsAsync: Loading failed.");
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Prüft, ob eine Synchronisation der Gruppendaten automatisch vom System ausgeführt werden soll.
        /// Falls ja, wird die automatische Synchronisation der Gruppe ausgeführt.
        /// </summary>
        public async Task PerformAutoSync()
        {
            if (SelectedGroup == null)
                return;

            if (IsGroupParticipant && !IsRemovedFromGroup && !SelectedGroup.Deleted)
            {
                try
                {
                    DateTimeOffset currentDate = DateTimeOffset.Now;
                    DateTimeOffset lastSyncDate = groupController.GetLastAutoSyncDate(SelectedGroup.Id);
                    Debug.WriteLine("PerformAutoSync: Last sync date: {0}, current date: {1}.", currentDate, lastSyncDate);

                    if (currentDate.Day != lastSyncDate.Day ||
                       currentDate.Month != lastSyncDate.Month ||
                       currentDate.Year != lastSyncDate.Year)
                    {
                        displayIndeterminateProgressIndicator();

                        Debug.WriteLine("PerformAutoSync: Need to perform auto sync for group with id {0}.", SelectedGroup.Id);
                        // Setzte Datum der letzten Synchronisation neu.
                        groupController.SetLastAutoSyncDate(SelectedGroup.Id, currentDate);

                        // Synchronisiere Gruppeninformation zuerst, um die Teilnehmerinfo als erstes zu aktualisieren.
                        await SynchronizeGroupInformationAsync(false);

                        // Sychronisiere Konversationen.
                        await SynchronizeConversationsAsync(false);

                        // Synchronisiere Abstimmungen.
                        await SynchronizeBallotsAsync(false);
                    }
                    else
                    {
                        Debug.WriteLine("PerformAutoSync: No need for the auto sync of group.");
                    }
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("PerformAutoSync: Sync failed. Msg is {0}.", ex.Message);
                    Debug.WriteLine("PerformAutoSync: Error code is: {0}.", ex.ErrorCode);
                    // Zeige Fehler nicht an.
                }
                finally
                {
                    hideIndeterminateProgressIndicator();
                }
            }
            else
            {
                Debug.WriteLine("PerformAutoSync: Conditions for auto sync not met.");
            }            
        }

        /// <summary>
        /// Stößt eine Synchronisation der Konversationsressourcen dieser Gruppe mit dem Server an.
        /// Aktualisiert anschließend die Anzeige.
        /// </summary>
        /// <param name="showError">Gibt an, ob Fehler dem Nutzer angezeigt werden sollen.</param>
        public async Task SynchronizeConversationsAsync(bool showError)
        {
            if (SelectedGroup == null)
                return;

            try
            {
                // Führe Synchronisation durch.
                await Task.Run(() => groupController.SynchronizeConversationsWithServerAsync(SelectedGroup.Id, true));

                // Aktualisere Anzeige. Rufe synchronisierte Daten ab.
                List<Conversation> conversations = await Task.Run(() => groupController.GetConversations(SelectedGroup.Id));

                if (conversations != null)
                {
                    // Sortieren.
                    conversations = sortConversationsByApplicationSettings(conversations);

                    if (ConversationCollection != null)
                    {
                        ConversationCollection.Clear();
                        foreach (Conversation conv in conversations)
                        {
                            ConversationCollection.Add(conv);
                        }
                    }
                }                
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("SynchronizeConversations: Execution failed.");

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    // Aktualisiere View.
                    SelectedGroup.Deleted = true;
                }

                if (ex.ErrorCode == ErrorCodes.GroupParticipantNotFound)
                {
                    // Aktualisiere View.
                    IsRemovedFromGroup = true;
                }

                if (showError)
                    displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Stößt eine Synchronisation der Abstimmungsressourcen dieser Gruppe mit dem Server an.
        /// Aktualisiert anschließend die Anzeige.
        /// </summary>
        /// <param name="showErrors">Gibt an, ob mögliche Fehler dem Nutzer angezeigt werden sollen.</param>
        public async Task SynchronizeBallotsAsync(bool showErrors)
        {
            if (SelectedGroup == null)
                return;

            try
            {
                // Führe Synchronisation durch.
                await Task.Run(() => groupController.SynchronizeBallotsWithServerAsync(SelectedGroup.Id, true));

                // Aktualisere Anzeige. Rufe synchronisierte Daten ab.
                List<Ballot> ballots = await Task.Run(() => groupController.GetBallots(SelectedGroup.Id, false));

                if (ballots != null)
                {
                    // Sortieren.
                    ballots = sortBallotsByApplicationSettings(ballots);

                    if (BallotCollection != null)
                    {
                        BallotCollection.Clear();
                        foreach (Ballot ballot in ballots)
                        {
                            BallotCollection.Add(ballot);
                        }
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("SynchronizeBallotsAsync: Execution failed.");

                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    // Aktualisiere View.
                    SelectedGroup.Deleted = true;
                }

                if (ex.ErrorCode == ErrorCodes.GroupParticipantNotFound)
                {
                    // Aktualisiere View.
                    IsRemovedFromGroup = true;
                }

                if (showErrors)
                    displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Stößt eine Synchronisation der Gruppendaten dieser Gruppe mit dem Server an.
        /// Aktualisiert anschließend die Anzeige.
        /// </summary>
        /// <param name="showErrors">Gibt an, ob mögliche Fehler dem Nutzer angezeigt werden sollen.</param>
        public async Task SynchronizeGroupInformationAsync(bool showErrors)
        {
            if (SelectedGroup == null)
                return;

            try
            {
                // Synchronisiere Daten.
                await Task.Run(() => groupController.SynchronizeGroupDetailsWithServerAsync(SelectedGroup.Id, true));

                // Rufe synchronisierte Daten ab.
                Group group = groupController.GetGroup(SelectedGroup.Id);

                if (group != null)
                {
                    // Aktualisiere Anzeige.
                    updateViewRelatedGroupProperties(SelectedGroup, group);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("SynchronizeGroupInformationAsync: Failed to synchronize group info.");

                if (ex.ErrorCode == ErrorCodes.GroupParticipantNotFound)
                {
                    // Aktualisiere View
                    IsRemovedFromGroup = true;
                }
                if (ex.ErrorCode == ErrorCodes.GroupNotFound)
                {
                    // Aktualisiere View
                    SelectedGroup.Deleted = true;
                }

                if (showErrors)
                    displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiere die Werte für die noch ungelesenen Konversationsnachrichten in jeder Konversation aus 
        /// der "ConversationCollection" Liste.
        /// </summary>
        public async Task UpdateNumberOfUnreadConversationMessagesAsync()
        {
            if (SelectedGroup == null)
                return;

            // Rufe Verzeichnis mit Informationen über die ungelesenen Nachrichten ab.
            Dictionary<int, int> unreadMessagesMap =
                await Task.Run(() => groupController.GetAmountOfUnreadConversationMessagesForGroup(SelectedGroup.Id));

            if (unreadMessagesMap != null)
            {
                // Aktualisiere Anzeige.
                foreach (Conversation conversation in ConversationCollection)
                {
                    if (unreadMessagesMap.ContainsKey(conversation.Id))
                    {
                        // Speichere Wert aus Verzeichnis als neuen Wert für die ungelesenen Nachrichten.
                        conversation.AmountOfUnreadMessages = unreadMessagesMap[conversation.Id];
                    }
                    else
                    {
                        // Keine ungelesenen Nachrichten.
                        conversation.AmountOfUnreadMessages = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Setzt das Flag HasNewEvent für die gewählte Gruppe zurück.
        /// </summary>
        public void ResetHasNewEventFlag()
        {
            if (SelectedGroup != null)
            {
                groupController.SetHasNewEventFlag(SelectedGroup.Id, false);
            }
        }

        /// <summary>
        /// Aktualisiert die Daten der Gruppe. Holt die neusten
        /// Daten aus der lokalen Datenbank und aktualisiert die View entsprechend.
        /// Methode muss im UI-Thread ausgeführt werden.
        /// </summary>
        public void ReloadGroupDetails()
        {
            if (SelectedGroup != null)
            {
                try
                {
                    Group group = groupController.GetGroup(SelectedGroup.Id);

                    if (group != null)
                    {
                        // Aktualisiere Eigenschaften.
                        updateViewRelatedGroupProperties(SelectedGroup, group);
                    }
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("ReloadGroupDetails: Failed to reload group details. " + 
                        "Error code is: {0}.", ex.ErrorCode);
                }
            }
        }

        /// <summary>
        /// Lädt die Collection von Konversationen neu.
        /// Diese Methode muss auf dem UI-Thread ausgeführt werden.
        /// </summary>
        public void ReloadConversationCollection()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                // Aktualisere Anzeige. Rufe neuste Daten ab.
                List<Conversation> conversations = groupController.GetConversations(SelectedGroup.Id);

                // Sortieren.
                conversations = sortConversationsByApplicationSettings(conversations);

                if (ConversationCollection != null && conversations != null)
                {
                    ConversationCollection.Clear();
                    foreach (Conversation conv in conversations)
                    {
                        ConversationCollection.Add(conv);
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("ReloadConversationCollection: Execution failed." + 
                    "Error code is: {0}.", ex.ErrorCode);
            }
        }

        /// <summary>
        /// Lädt die Collection von Abstimmungen neu.
        /// Diese Methode muss auf dem UI-Thread ausgeführt werden.
        /// </summary>
        public void ReloadBallotCollection()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                // Aktualisiere Anzeige. Rufe neuste Daten ab.
                List<Ballot> ballots = groupController.GetBallots(SelectedGroup.Id, false);

                // Sortieren.
                ballots = sortBallotsByApplicationSettings(ballots);

                if (BallotCollection != null && ballots != null)
                {
                    BallotCollection.Clear();
                    foreach (Ballot ballot in ballots)
                    {
                        BallotCollection.Add(ballot);
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("ReloadBallotCollection: Execution failed." +
                    "Error code is: {0}.", ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiert den Zustand der View, wenn die App aus dem Suspension Zustand kommt.
        /// Diese Methode kann dann gerufen werden, wenn die GroupDetails Seite vor dem Suspension 
        /// Zustand die letzte aktive Seite war.
        /// </summary>
        public void UpdateViewOnAppResume()
        {
            if (SelectedGroup != null && 
                IsGroupParticipant && 
                !IsRemovedFromGroup && 
                !SelectedGroup.Deleted)
            {
                Debug.WriteLine("UpdateViewOnAppResume: Performing update.");

                // Lade Gruppendaten neu.
                ReloadGroupDetails();

                // Lade Abstimmungen neu.
                ReloadBallotCollection();

                // Lade Konversationen neu.
                ReloadConversationCollection();

                Debug.WriteLine("UpdateViewOnAppResume: View reloaded.");
            }
            else
            {
                Debug.WriteLine("UpdateViewOnAppResume: No update for view will be performed.");
            }
        }

        /// <summary>
        /// Aktualisiert die für die View relevanten Eigenschaften der Gruppeninstanzen.
        /// Führt somit eine Aktualisierung der Anzeige aus.
        /// Muss im UI-Thread ausgeführt werden.
        /// </summary>
        /// <param name="oldGroup">Das derzeit an die View gebundenen Gruppen Objekt.</param>
        /// <param name="newGroup">Das Gruppen Objekt mit den aktuellen Daten.</param>
        private void updateViewRelatedGroupProperties(Group oldGroup, Group newGroup)
        {
            oldGroup.Name = newGroup.Name;
            oldGroup.Description = newGroup.Description;
            oldGroup.Term = newGroup.Term;
            oldGroup.CreationDate = newGroup.CreationDate;
            oldGroup.ModificationDate = newGroup.ModificationDate;
            oldGroup.GroupAdmin = newGroup.GroupAdmin;
            oldGroup.Participants = newGroup.Participants;

            if (oldGroup.Deleted != newGroup.Deleted)
            {
                oldGroup.Deleted = newGroup.Deleted;
                checkCommandExecution();
            }
        }

        /// <summary>
        /// Sortiert die Liste von Konversationen anhand der aktuell gültigen Anwendungseinstellungen.
        /// </summary>
        /// <param name="conversations">Die zu sortierende Liste von Konversationen.</param>
        /// <returns>Die sortierte Liste von Konversationen.</returns>
        private List<Conversation> sortConversationsByApplicationSettings(List<Conversation> conversations)
        {
            AppSettings appSettings = groupController.GetApplicationSettings();

            switch (appSettings.ConversationOrderSetting)
            {
                case DataModel.Enums.OrderOption.ALPHABETICAL:
                    conversations = new List<Conversation>(
                        from item in conversations
                        orderby item.Title ascending 
                        select item);
                    break;
                case DataModel.Enums.OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    conversations = new List<Conversation>(
                        from item in conversations
                        orderby item.AmountOfUnreadMessages descending, item.Title ascending
                        select item);
                    break;
                default:
                    conversations = new List<Conversation>(
                        from item in conversations
                        orderby item.Title ascending
                        select item);
                    break;
            }

            return conversations;
        }

        /// <summary>
        /// Sortiert die Abstimmungen anhand der aktuell gültigen Anwendungseinstellungen.
        /// </summary>
        /// <param name="ballots">Die zu sortierende Liste von Abstimmungen.</param>
        /// <returns>Die sortierte Liste von Abstimmungen.</returns>
        private List<Ballot> sortBallotsByApplicationSettings(List<Ballot> ballots)
        {
            AppSettings appSettings = groupController.GetApplicationSettings();

            switch (appSettings.BallotOrderSetting)
            {
                case DataModel.Enums.OrderOption.ALPHABETICAL:
                    ballots = new List<Ballot>(
                        from item in ballots
                        orderby item.Title ascending
                        select item);
                    break;
                case DataModel.Enums.OrderOption.BY_TYPE:
                    ballots = new List<Ballot>(
                        from item in ballots
                        where item.IsMultipleChoice.HasValue && item.HasPublicVotes.HasValue
                        orderby item.IsMultipleChoice descending, item.HasPublicVotes descending
                        select item);
                    break;
                default:
                    ballots = new List<Ballot>(
                        from item in ballots
                        orderby item.Title ascending
                        select item);
                    break;
            }

            return ballots;
        }

        /// <summary>
        /// Prüft, ob der Hinweis bezüglich der Löschung der Gruppe angezeigt werden muss.
        /// Falls das der Fall ist, wird der Hinweis angezeigt.
        /// </summary>
        private void displayDeletionNotification()
        {
            if (SelectedGroup == null)
                return;

            if (!groupController.IsDeletionNoticed(SelectedGroup.Id))
            {
                // Öffne Flyout
                IsDeletionOrRemovalNotificationShown = true;

                // Setze Flag auf true. Nutzer hat den Hinweis gesehen.
                groupController.SetDeletionNoticed(SelectedGroup.Id, true);
            }
        }

        /// <summary>
        /// Prüft, ob der Hinweis, dass der lokale Nutzer aus der Gruppe entfernt wurde, angezeigt werden muss.
        /// Falls das der Fall ist, wird der Hinweis angezeigt.
        /// </summary>
        private void displayRemovedFromGroupNotification()
        {
            if (SelectedGroup == null)
                return;

            if (!groupController.IsRemovalFromGroupNoticed(SelectedGroup.Id))
            {
                // Öffne Flyout
                IsDeletionOrRemovalNotificationShown = true;

                // Setze Flag auf true. Nutzer hat den Hinweis gesehen.
                groupController.SetRemovedFromGroupNoticed(SelectedGroup.Id, true);
            }
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsmethode, welche die Prüfung der Ausführbarkeit von Befehlen
        /// anstößt.
        /// </summary>
        private void checkCommandExecution()
        {
            JoinGroupCommand.OnCanExecuteChanged();
            LeaveGroupCommand.OnCanExecuteChanged();
            if (canLeaveGroup())
                HasLeaveOption = true;
            else
                HasLeaveOption = false;
            EditGroupCommand.RaiseCanExecuteChanged();
            SynchronizeDataCommand.OnCanExecuteChanged();
            DeleteGroupCommand.OnCanExecuteChanged();
            if (canDeleteGroup())
                HasDeleteOption = true;
            else
                HasDeleteOption = false;
            DeleteGroupLocallyCommand.RaiseCanExecuteChanged();
            if (canDeleteGroupLocally())
                HasDeleteLocallyOption = true;
            else
                HasDeleteLocallyOption = false;
            ChangeToGroupSettingsCommand.RaiseCanExecuteChanged();
            ChangeToAddConversationDialog.RaiseCanExecuteChanged();
            SwitchToCreateBallotDialogCommand.RaiseCanExecuteChanged();
            SwichToGroupParticipantsViewCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Beitritt zu einer Gruppe zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canJoinGroup()
        {
            if (SelectedGroup != null && !IsGroupParticipant)
                return true;
            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Beitritt zu einer Gruppe aus. 
        /// Stößt die entsprechende Funktionalität an.
        /// </summary>
        /// <returns></returns>
        private async Task executeJoinGroupCommandAsync()
        {
            if (SelectedGroup == null)
            {
                return;
            }

            Debug.WriteLine("Execute join group command called.");

            try
            {
                displayIndeterminateProgressIndicator("GroupDetailsJoinGroupStatus");

                bool successful = await groupController.JoinGroupAsync(SelectedGroup.Id, EnteredPassword);

                if (successful)
                {
                    Debug.WriteLine("Successfully joined group with id {0}.", SelectedGroup.Id);

                    // Lade neue Gruppendaten.
                    SelectedGroup = groupController.GetGroup(SelectedGroup.Id);
                    
                    if (SelectedGroup != null)
                        Debug.WriteLine("The new selected group is: {0}.", SelectedGroup.Name);

                    IsGroupParticipant = true;

                    // Lade Konversationsdaten.
                    await LoadConversationsAsync(SelectedGroup.Id);
                    // Lade Abstimmungen.
                    await LoadBallotsAsync(SelectedGroup.Id);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Joining group has failed. Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Verlassen der Gruppe zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canLeaveGroup()
        {
            // Steht nur im Gruppendetails PivotItem zur Verfügung (Index 2).
            if (SelectedGroup != null && !SelectedGroup.Deleted &&
                IsGroupParticipant && 
                !IsRemovedFromGroup)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Verlassen der Gruppe aus.
        /// </summary>
        private async Task executeLeaveGroupCommandAsync()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator("GroupDetailsLeaveGroupStatus");

                await groupController.LeaveGroupAsync(SelectedGroup.Id);

                if (_navService != null && _navService.CanGoBack())
                {
                    _navService.GoBack();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeLeaveGroupCommandAsync: Leaving group failed.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Wechsel auf den Dialog zur Aktualisierung der Gruppendaten 
        /// ausgeführt werden kann.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canEditGroup()
        {
            // Nur möglich für Administrator von Gruppe. Außerdem nur auf dem "Details" Pivot Item.
            if (SelectedGroup != null && !SelectedGroup.Deleted &&
                localUser.Id == SelectedGroup.GroupAdmin && 
                !IsRemovedFromGroup &&
                SelectedPivotItemName == "GroupDetailsPivotItem")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Wechseln auf den Dialog zur Änderung der Gruppendaten
        /// aus. 
        /// </summary>
        private void executeEditGroupCommand()
        {
            if (SelectedGroup !=null && _navService != null)
            {
                _navService.Navigate("AddAndEditGroup", SelectedGroup.Id);
            }
        }

        /// <summary>
        /// Führt den Befehl ConversationSelectedCommand aus. Stößt den Wechsel
        /// auf die Detailansicht der Konversation an.
        /// </summary>
        /// <param name="selectedItem">Der gewählte Listeneintrag. Hier die gewählte Konversation.</param>
        private void executeConversationSelectedCommand(object selectedItem)
        {
            Conversation conversation = selectedItem as Conversation;
            if (conversation != null)
            {
                _navService.Navigate("ConversationDetails", conversation.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Synchronisieren der Daten 
        /// aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeData()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted &&
                IsGroupParticipant && 
                !IsRemovedFromGroup)
                return true;

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Aktualisieren der Daten aus.
        /// </summary>
        private async Task executeSynchronizeDataCommandAsync()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                switch (SelectedPivotItemName)
                {
                    case "ConversationPivotItem":
                        displayIndeterminateProgressIndicator("GroupDetailsSynchronizeConversationStatus");
                        await SynchronizeConversationsAsync(true);
                        break;
                    case "BallotsPivotItem":
                        displayIndeterminateProgressIndicator("GroupDetailsSynchronizeBallotStatus");
                        await SynchronizeBallotsAsync(true);
                        break;
                    case "GroupDetailsPivotItem":
                        displayIndeterminateProgressIndicator("GroupDetailsSynchronizeGroupDetailsStatus");
                        await SynchronizeGroupInformationAsync(true);
                        break;
                    case "EventsPivotItem":
                        break;
                    default:
                        break;
                }

                // Lade Teilnehmer-Informationen erneut, da diese durch die Synchronisation möglicherweise 
                // geändert wurden.
                List<User> participants = groupController.GetActiveParticipantsOfGroup(SelectedGroup.Id);
                // Prüfe, ob lokaler Nutzer noch in der Liste ist.
                User localUser = groupController.GetLocalUser();
                int listIndex = participants.FindIndex(item => item.Id == localUser.Id);
                if (listIndex >= 0)
                {
                    IsRemovedFromGroup = false;
                }
                else
                {
                    Debug.WriteLine("executeSynchronizeDataCommandAsync: Participant now seems to be removed from the group.");
                    IsRemovedFromGroup = true;
                    checkCommandExecution();
                }
                SelectedGroup.Participants = participants;
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeDataCommandAsync: Synchronization of the data has failed.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Löschen der Gruppe aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canDeleteGroup()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted &&
                localUser.Id == SelectedGroup.GroupAdmin &&
                !IsRemovedFromGroup && 
                SelectedPivotItemName == "GroupDetailsPivotItem")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl DeleteGroupCommand aus. Stößt die Löschung
        /// der Gruppe an.
        /// </summary>
        private async Task executeDeleteGroupAsync()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator("GroupDetailsDeleteGroupStatus");

                await groupController.DeleteGroupAsync(SelectedGroup.Id);

                if (_navService.CanGoBack())
                    _navService.GoBack();
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeDeleteGroupAsync: Failed to delete the group.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Löschen der Gruppe aus den lokalen Datensätzen aktuell
        /// zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canDeleteGroupLocally()
        {
            if (SelectedGroup != null && 
                IsGroupParticipant && 
                (IsRemovedFromGroup || SelectedGroup.Deleted))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum lokalen Löschen der Gruppe aus.
        /// </summary>
        private void executeDeleteGroupLocallyCommand()
        {
            try
            {
                groupController.DeleteGroupLocally(SelectedGroup.Id);

                if (_navService.CanGoBack())
                    _navService.GoBack();
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeDeleteGroupLocallyCommand: Failed to delete group locally.");
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Gibt an, ob der Wechsel auf die Gruppeneinstellungen aktuell möglich ist.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canChangeToGroupSettings()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted && 
                IsGroupParticipant && 
                !IsRemovedFromGroup)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl ChangeToGroupSettingsCommand aus. Wechselt auf die Gruppeneinstellungs-Seite.
        /// </summary>
        private void executeChangeToGroupSettingsCommand()
        {
            if (SelectedGroup != null)
            {
                _navService.Navigate("GroupSettings", SelectedGroup.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Wechsel auf den Dialog zur Erstellung
        /// neuer Konversationen aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canChangeToAddConversationDialog()
        {
            if (SelectedGroup != null && 
                !SelectedGroup.Deleted && 
                !IsRemovedFromGroup && 
                SelectedPivotItemName == "ConversationPivotItem")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Wechseln auf den Dialog zur Erstellung neuer Konversationen aus.
        /// </summary>
        private void executeChangeToAddConversationDialog()
        {
            _navService.Navigate("AddAndEditConversation", SelectedGroup.Id);
        }

        /// <summary>
        /// Führt den Befehl BallotSelectedCommand aus. Stößt den Wechsel
        /// auf die Detailansicht der Abstimmung an. 
        /// </summary>
        /// <param name="selectedItem">Der gewählte Listeintrag. Hier die gewählte Abstimmung.</param>
        private void executeBallotSectedCommand(object selectedItem)
        {
            Ballot ballot = selectedItem as Ballot;
            if (ballot != null && SelectedGroup != null)
            {
                string navigationParameter = "navParam?groupId=" + SelectedGroup.Id + "?ballotId=" + ballot.Id;
                _navService.Navigate("BallotDetails", navigationParameter);
            }
        }

        /// <summary>
        /// Prüft, ob der Befehl zum Wechseln auf den Erstellungdialog einer neuen Abstimmung 
        /// aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSwitchToCreateBallotDialog()
        {
            if (SelectedGroup != null &&
                !SelectedGroup.Deleted && 
                !IsRemovedFromGroup &&
                SelectedPivotItemName == "BallotsPivotItem")
            {
                if (SelectedGroup.Type == DataModel.Enums.GroupType.TUTORIAL)
                {
                    // Spezialfall Tutoriumsgruppe. Hier kann nur der Administrator eine Gruppe erstellen.
                    if (localUser.Id != SelectedGroup.GroupAdmin)
                    {
                        return false;
                    }
                }

                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToCreateBallotDialogCommand aus. Wechselt auf
        /// den Erstellungsdialog für Abstimmungen.
        /// </summary>
        private void executeSwitchToCreateBallotDialogCommand()
        {
            string navigationParameter = "navParam?groupId=" + SelectedGroup.Id;
            _navService.Navigate("AddAndEditBallot", navigationParameter);
        }

        /// <summary>
        /// Gibt an, ob der Befehl zur Verwaltung der Mitglieder aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSwitchToGroupParticipantsView()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted && 
                IsGroupParticipant && !IsRemovedFromGroup && 
                localUser.Id == SelectedGroup.GroupAdmin)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToGroupParticipantsView aus. Stößt den
        /// Wechsel auf die Verwaltungsseite für Gruppenteilnehmer an.
        /// </summary>
        private void executeSwitchToGroupParticipantsView()
        {
            _navService.Navigate("GroupParticipants", SelectedGroup.Id);
        }
        #endregion CommandFunctionality

    }
}
