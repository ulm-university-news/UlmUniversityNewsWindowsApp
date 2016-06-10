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

        private bool hasLeaveOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglichkeit hat die Gruppe zu verlassen.
        /// </summary>
        public bool HasLeaveOption
        {
            get { return hasLeaveOption; }
            set { this.setProperty(ref this.hasLeaveOption, value); }
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
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadGroupFromLocalStorageAsync: Execution failed.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
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
                IsGroupParticipant)
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
            User localUser = groupController.GetLocalUser();
            // Nur möglich für Administrator von Gruppe. Außerdem nur auf dem "Details" Pivot Item.
            if (SelectedGroup != null &&
                localUser.Id == SelectedGroup.GroupAdmin && 
                SelectedPivotItemIndex == 2)
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
        #endregion CommandFunctionality

    }
}
