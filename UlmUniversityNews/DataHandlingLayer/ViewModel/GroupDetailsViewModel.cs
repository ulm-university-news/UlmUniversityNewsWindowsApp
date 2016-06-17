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
        /// Gibt an, ob der Nutzer die Möglichkeit hat die Gruppe zu löschen.
        /// </summary>
        public bool HasDeleteOption
        {
            get { return hasDeleteOption; }
            set { this.setProperty(ref this.hasDeleteOption, value); }
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
                    if (groupController.IsActiveParticipant(loadedGroup.Id, groupController.GetLocalUser().Id))
                    {
                        Debug.WriteLine("User seems to be an active participant of the group.");
                        IsGroupParticipant = true;
                        // Frage noch die Teilnehmer ab.
                        List<User> participants = await Task.Run(() => groupController.GetActiveParticipantsOfGroup(loadedGroup.Id));
                        loadedGroup.Participants = participants;
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
                Debug.WriteLine("Loaded group is: " + loadedGroup.Name);

                SelectedGroup = loadedGroup;  
                
                if (SelectedGroup != null)
                {
                    if (groupController.IsActiveParticipant(loadedGroup.Id, groupController.GetLocalUser().Id))
                    {
                        IsRemovedFromGroup = false;
                        Debug.WriteLine("LoadGroupFromLocalStorageAsync: local user still active in this group.");
                    }
                    else
                    {
                        IsRemovedFromGroup = true;
                        Debug.WriteLine("LoadGroupFromLocalStorageAsync: local user seems to be removed from this group.");
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
                    // TODO sortieren

                    if (ConversationCollection == null)
                        ConversationCollection = new ObservableCollection<Conversation>();

                    foreach (Conversation conversation in conversations)
                    {
                        ConversationCollection.Add(conversation);
                    }

                    // Test: Führe Synchronisation durch.
                    // await SynchronizeConversations();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadConversationsAsync: Execution failed.");
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Stößt eine Synchronisation der Konversationsressourcen dieser Gruppe mit dem Server an.
        /// Aktualisiert anschließend die Anzeige.
        /// </summary>
        public async Task SynchronizeConversations()
        {
            if (SelectedGroup == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();
                // Führe Synchronisation durch.
                await Task.Run(() => groupController.SynchronizeConversationsWithServerAsync(SelectedGroup.Id));

                // Aktualisere Anzeige. Rufe synchronisierte Daten ab.
                List<Conversation> conversations = await Task.Run(() => groupController.GetConversations(SelectedGroup.Id));

                // TODO Sortieren.

                ConversationCollection.Clear();
                foreach (Conversation conv in conversations)
                {
                    ConversationCollection.Add(conv);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("SynchronizeConversations: Execution failed.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
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
            {
                HasLeaveOption = true;
            }
            else
            {
                HasLeaveOption = false;
            }
            EditGroupCommand.RaiseCanExecuteChanged();
            SynchronizeDataCommand.OnCanExecuteChanged();
            DeleteGroupCommand.OnCanExecuteChanged();
            if (canDeleteGroup())
            {
                HasDeleteOption = true;
            }
            else
            {
                HasDeleteOption = false;
            }
            DeleteGroupLocallyCommand.RaiseCanExecuteChanged();
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
            Debug.WriteLine("Entered password is " + EnteredPassword);

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
            if (SelectedGroup != null && 
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
            if (SelectedGroup != null &&
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
        /// auf die Detailansicht der Konversation aus.
        /// </summary>
        /// <param name="selectedConversation">Die gewählte Konversation.</param>
        private void executeConversationSelectedCommand(object selectedConversation)
        {
            Conversation conversation = selectedConversation as Conversation;
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
            if (SelectedGroup != null && 
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

                        await SynchronizeConversations();

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

                        break;
                    case "BallotPivotItem":
                        break;
                    case "GroupDetailsPivotItem":
                        break;
                    case "EventsPivotItem":
                        break;
                    default:
                        break;
                }
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
            if (SelectedGroup != null && 
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
                IsRemovedFromGroup)
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
        #endregion CommandFunctionality

    }
}
