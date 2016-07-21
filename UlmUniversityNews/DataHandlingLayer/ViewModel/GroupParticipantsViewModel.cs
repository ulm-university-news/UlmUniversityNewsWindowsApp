using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataHandlingLayer.CommandRelays;
using System.Diagnostics;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;

namespace DataHandlingLayer.ViewModel
{
    public class GroupParticipantsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der GroupController Klasse.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private bool hasRemoveParticipantsOption;
        /// <summary>
        /// Gibt an, ob der lokale Nutzer die Möglichkeit hat, Teilnehmer von der Gruppe zu entfernen.
        /// </summary>
        public bool HasRemoveParticipantsOption
        {
            get { return hasRemoveParticipantsOption; }
            set { this.setProperty(ref this.hasRemoveParticipantsOption, value); }
        }

        private bool isDisplayingRemoveWarning;
        /// <summary>
        /// Gibt an, ob aktuell die Warnung bezüglich des Entfernens von Teilnehmern angezeigt wird.
        /// </summary>
        public bool IsDisplayingRemoveWarning
        {
            get { return isDisplayingRemoveWarning; }
            set { this.setProperty(ref this.isDisplayingRemoveWarning, value); }
        }

        private Group selectedGroup;
        /// <summary>
        /// Die gewählte Gruppe. Für diese Gruppe werden die Teilnehmer angezeigt.
        /// </summary>
        public Group SelectedGroup
        {
            get { return selectedGroup; }
            set { this.setProperty(ref this.selectedGroup, value); }
        }

        private ObservableCollection<User> participantsCollection;
        /// <summary>
        /// Die Liste von Teilnehmern der Gruppe.
        /// </summary>
        public ObservableCollection<User> ParticipantsCollection
        {
            get { return participantsCollection; }
            set { this.setProperty(ref this.participantsCollection, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand removeParticipantCommand;
        /// <summary>
        /// Befehl zum Entfernen eines Teilnehmers von der Gruppe.
        /// </summary>
        public AsyncRelayCommand RemoveParticipantCommand
        {
            get { return removeParticipantCommand; }
            set { removeParticipantCommand = value; }
        }

        private AsyncRelayCommand synchronizeGroupParticipantsCommand;
        /// <summary>
        /// Befehl zum Synchronisieren der Teilnehmerinformationen.
        /// </summary>
        public AsyncRelayCommand SynchronizeGroupParticipantsCommand
        {
            get { return synchronizeGroupParticipantsCommand; }
            set { synchronizeGroupParticipantsCommand = value; }
        }

        private RelayCommand updateDisplayingWarningStatusCommand;
        /// <summary>
        /// Befehl zum Aktualisieren des Status bezüglich der Anzeige des Warnhinweises
        /// beim Entfernen von Teilnehmern.
        /// </summary>
        public RelayCommand UpdateDisplayingWarningStatusCommand
        {
            get { return updateDisplayingWarningStatusCommand; }
            set { updateDisplayingWarningStatusCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupParticipantsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public GroupParticipantsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            groupController = new GroupController();

            if (ParticipantsCollection == null)
                ParticipantsCollection = new ObservableCollection<User>();

            // Befehle
            RemoveParticipantCommand = new AsyncRelayCommand(
                param => executeRemoveParticipantCommand(param),
                param => canRemoveParticipants());
            SynchronizeGroupParticipantsCommand = new AsyncRelayCommand(
                param => executeSynchronizeParticipants(),
                param => canSynchronizeParticipants());
            UpdateDisplayingWarningStatusCommand = new RelayCommand(
                param => executeUpdateDisplayingWarningStatus());
        }

        /// <summary>
        /// Lädt die Teilnehmer für die angegebene Gruppe.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        public async Task LoadGroupParticipantsAsync(int groupId)
        {
            Debug.WriteLine("LoadGroupParticipantsAsync: Start loading process for group with id {0}.", groupId);
            try
            {
                SelectedGroup = await Task.Run(() => groupController.GetGroup(groupId));

                if (SelectedGroup != null)
                {
                    if (SelectedGroup.Participants != null)
                    {
                        ParticipantsCollection.Clear();
                        foreach (User participant in SelectedGroup.Participants)
                        {
                            if (participant.Id != SelectedGroup.GroupAdmin)
                            {
                                Debug.WriteLine("LoadGroupParticipantsAsync: Added user with id {0} to participants list.", participant.Id);
                                ParticipantsCollection.Add(participant);
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("LoadGroupParticipantsAsync: No participants set.");
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadGroupParticipantsAsync: Failed to load group participants. " + 
                    "Error code: {0} and msg: '{1}'.", ex.ErrorCode, ex.Message);
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        #region CommandFunctionality
        /// <summary>
        /// Stößt die Überprüfung der Ausführbarkeit der Befehle an.
        /// </summary>
        private void checkCommandExecution()
        {
            SynchronizeGroupParticipantsCommand.OnCanExecuteChanged();
            RemoveParticipantCommand.OnCanExecuteChanged();
            if (canRemoveParticipants())
                HasRemoveParticipantsOption = true;
            else
                HasRemoveParticipantsOption = false;
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Löschen von Teilnehmern aus der Gruppe 
        /// zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canRemoveParticipants()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted && 
                groupController.GetLocalUser().Id == SelectedGroup.GroupAdmin && 
                ParticipantsCollection != null && ParticipantsCollection.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl RemoveParticipantCommand aus.
        /// Entfernt den gewählten Teilnehmer aus der Gruppe.
        /// </summary>
        /// <param name="selectedParticipant">Den zu entfernden Teilnehmer.</param>
        private async Task executeRemoveParticipantCommand(object selectedParticipant)
        {
            if (selectedParticipant != null)
            {
                User selectedUser = selectedParticipant as User;

                if (selectedUser != null)
                {
                    // Entferne Nutzer aus Gruppe.
                    try
                    {
                        displayIndeterminateProgressIndicator("GroupParticipantsRemoveParticipantStatus");

                        await groupController.RemoveParticipantFromGroupAsync(SelectedGroup.Id, selectedUser.Id);

                        ParticipantsCollection.Remove(selectedUser);
                        IsDisplayingRemoveWarning = false;

                        checkCommandExecution();
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("executeRemoveParticipantCommand: Failed to remove participant from group. " + 
                            "Error code: {0} and msg: {1}.", ex.ErrorCode, ex.Message);
                        displayError(ex.ErrorCode);
                    }
                    finally
                    {
                        hideIndeterminateProgressIndicator();
                    }
                }
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zur Synchronisation der Teilnehmer zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeParticipants()
        {
            if (SelectedGroup != null && !SelectedGroup.Deleted)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeParticipantsCommand aus.
        /// Stößt die Synchronisation der Teilnehmerinformationen an.
        /// </summary>
        private async Task executeSynchronizeParticipants()
        {
            try
            {
                displayIndeterminateProgressIndicator("GroupParticipantsSynchronizationStatus");

                await Task.Run(() => groupController.SynchronizeGroupParticipantsAsync(SelectedGroup.Id));

                // Aktualisiere Anzeige durch neu laden.
                await LoadGroupParticipantsAsync(SelectedGroup.Id);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeParticipants: Failed to synchronize participants. " + 
                    "Error code: {0} and msg '{1}'.", ex.ErrorCode, ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Führt den Befehl UpdateDisplayingWarningStatusCommand aus.
        /// Setzt den Status auf "anzeigen".
        /// </summary>
        private void executeUpdateDisplayingWarningStatus()
        {
            Debug.WriteLine("executeUpdateDisplayingWarningStatus: Set flyout status to active.");
            IsDisplayingRemoveWarning = true;
        }
        #endregion CommandFunctionality

    }
}
