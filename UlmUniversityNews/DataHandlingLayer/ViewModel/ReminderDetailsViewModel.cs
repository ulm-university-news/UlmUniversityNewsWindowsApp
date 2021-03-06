﻿using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.Controller;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class ReminderDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Eine Referenz auf eine Instanz des ModeratorController.
        /// </summary>
        private ModeratorController moderatorController;
        #endregion Fields

        #region Properties
        private Reminder selectedReminder;
        /// <summary>
        /// Das Reminder Objekt, von dem die Details angezeigt werden sollen.
        /// </summary>
        public Reminder SelectedReminder
        {
            get { return selectedReminder; }
            set { this.setProperty(ref this.selectedReminder, value); }
        }

        private Channel selectedChannel;
        /// <summary>
        /// Das Kanal Objekt des Kanals, zu dem der Reminder gehört.
        /// </summary>
        public Channel SelectedChannel
        {
            get { return selectedChannel; }
            set { this.setProperty(ref this.selectedChannel, value); }
        }

        private bool validReminderSelected;
        /// <summary>
        /// Gibt an, ob ein valider Reminder gewählt wurde.
        /// </summary>
        public bool ValidReminderSelected
        {
            get { return validReminderSelected; }
            set { this.setProperty(ref this.validReminderSelected, value); }
        }
        
        private string lastModifiedBy;
        /// <summary>
        /// Der Name des Moderators, der den Reminder als letztes geändert hat.
        /// </summary>
        public string LastModifiedBy
        {
            get { return lastModifiedBy; }
            set { this.setProperty(ref this.lastModifiedBy, value); }
        }

        private bool isActivateReminderFlyoutOpen;
        /// <summary>
        /// Gibt an, ob das Flyout bezüglich der Aktivierung eines Reminders aktuell offen ist oder nicht.
        /// </summary>
        public bool IsActivateReminderFlyoutOpen
        {
            get { return isActivateReminderFlyoutOpen; }
            set { this.setProperty(ref this.isActivateReminderFlyoutOpen, value); }
        }

        private bool isDeactivateReminderFlyoutOpen;
        /// <summary>
        /// Gibt an, ob das Flyout bezüglich der Deaktivierung eines Reminders aktuell offen ist oder nicht.
        /// </summary>
        public bool IsDeactivateReminderFlyoutOpen 
        {
            get { return isDeactivateReminderFlyoutOpen; }
            set { this.setProperty(ref this.isDeactivateReminderFlyoutOpen, value); }
        }   
        #endregion Properties

        #region Commands
        private RelayCommand switchToEditReminderDialogCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Dialog zum bearbeiten des Reminder.
        /// </summary>
        public RelayCommand SwitchToEditReminderDialogCommand
        {
            get { return switchToEditReminderDialogCommand; }
            set { switchToEditReminderDialogCommand = value; }
        }

        private AsyncRelayCommand deleteReminderCommand;
        /// <summary>
        /// Befehl zum Löschen eines Reminder.
        /// </summary>
        public AsyncRelayCommand DeleteReminderCommand
        {
            get { return deleteReminderCommand; }
            set { deleteReminderCommand = value; }
        }

        private AsyncRelayCommand activateReminderCommand;
        /// <summary>
        /// Befehl zum Aktivieren eines aktuell deaktivierten Reminders.
        /// </summary>
        public AsyncRelayCommand ActivateReminderCommand
        {
            get { return activateReminderCommand; }
            set { activateReminderCommand = value; }
        }

        private AsyncRelayCommand deactivateReminderCommand;
        /// <summary>
        /// Befehl zum Deaktivieren eines aktuell aktivierten Reminders.
        /// </summary>
        public AsyncRelayCommand DeactivateReminderCommand
        {
            get { return deactivateReminderCommand; }
            set { deactivateReminderCommand = value; }
        }

        private AsyncRelayCommand synchronizeReminderCommand;
        /// <summary>
        /// Befehl zur Synchronisation der Daten des aktuell betrachteten Reminders
        /// mit den Serverdaten.
        /// </summary>
        public AsyncRelayCommand SynchronizeReminderCommand
        {
            get { return synchronizeReminderCommand; }
            set { synchronizeReminderCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ReminderDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ReminderDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController();
            moderatorController = new ModeratorController();

            // Befehle anlegen.
            SwitchToEditReminderDialogCommand = new RelayCommand(
                param => executeSwitchToEditReminderCommand(),
                param => canSwitchToEditReminderCommand());
            DeleteReminderCommand = new AsyncRelayCommand(
                param => executeDeleteReminderCommand(),
                param => canDeleteReminder());
            ActivateReminderCommand = new AsyncRelayCommand(
                param => executeActivateReminderCommand(),
                param => canActivateReminder());
            DeactivateReminderCommand = new AsyncRelayCommand(
                param => executeDeactivateReminderCommand(),
                param => canDeactivateReminder());
            SynchronizeReminderCommand = new AsyncRelayCommand(
                param => executeSynchronizeReminderCommand(),
                param => canSynchronizeReminder());
        }

        /// <summary>
        /// Lädt den Reminder mit der angegebenen Id und weitere für die View
        /// relavante Parameter.
        /// </summary>
        /// <param name="reminderId">Die Id des Reminders, der geladen werden soll.</param>
        public async Task LoadSelectedReminderAsync(int reminderId)
        {
            try
            {
                SelectedReminder = await Task.Run(() => channelController.GetReminder(reminderId));
                //Debug.WriteLine("SelectedReminder properties: " + SelectedReminder.ToString());

                if (SelectedReminder != null)
                {
                    ValidReminderSelected = true;

                    // Berechne nächsten Reminder Termin.
                    SelectedReminder.ComputeFirstNextDate();
                    // Prüfe ob Reminder abgelaufen ist.
                    SelectedReminder.EvaluateIsExpired();

                    SelectedChannel = await Task.Run(() => channelController.GetChannel(SelectedReminder.ChannelId));

                    // Lade Moderator, der Reminder zuletzt geändert hat.
                    // Dieser ist vom Server als Autor eingetragen worden.
                    Moderator author = await Task.Run(() => moderatorController.GetModerator(SelectedReminder.AuthorId));
                    Debug.WriteLine("Author is: {0}.", author);
                    if (author != null)
                        LastModifiedBy = author.FirstName + " " + author.LastName;
                }
                else
                {
                    ValidReminderSelected = false;
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error during loading process. Cannot continue." + 
                    " Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        /// <summary>
        /// Stößt eine Synchronisation der lokal gehaltenen Reminder-Instanz des Reminders
        /// mit der angegebenen Id an. Die neuesten Daten des Reminder werden vom Server
        /// abgerufen und der lokale Datensatz falls notwendig aktualisiert.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem der Reminder gehört.</param>
        /// <param name="reminderId">Die Id des Reminders, der synchronisiert werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlschlägt.</exception>
        private async Task synchronizeReminderAsync(int channelId, int reminderId)
        {
            // Frage aktuellste Version des Reminders vom Server ab.
            Reminder referenceReminder = await channelController.GetReminderAsync(channelId, reminderId);

            // Vergleiche mit lokal gehaltener Version, ob Aktualisierung erforderlich.
            Reminder currentReminder = SelectedReminder;

            if (DateTimeOffset.Compare(currentReminder.ModificationDate, referenceReminder.ModificationDate) < 0)
            {
                Debug.WriteLine("synchronizeReminderAsync: Need to update.");
                channelController.ReplaceLocalReminder(referenceReminder);
                updateViewRelatedPropertiesOfReminder(currentReminder, referenceReminder);
            }
            else if (currentReminder.Ignore != referenceReminder.Ignore)
            {
                Debug.WriteLine("synchronizeReminderAsync: Need to update due to ignore field.");
                currentReminder.Ignore = referenceReminder.Ignore;
                channelController.ReplaceLocalReminder(currentReminder);
                updateViewRelatedPropertiesOfReminder(currentReminder, referenceReminder);
            }
            else if (currentReminder.IsActive != referenceReminder.IsActive)
            {
                Debug.WriteLine("synchronizeReminderAsync: Need to update to due isActive field.");
                currentReminder.IsActive = referenceReminder.IsActive;  // Aktualisere nur IsActive.
                channelController.ChangeReminderActiveStatus(
                    currentReminder.Id, 
                    currentReminder.IsActive);

                // Aktualisiere Befehlsausführbarkeiten.
                checkCommandExecution();
            }
        }

        /// <summary>
        /// Aktualisiert die für die View relevanten Parameter eines Reminder Objekts, welches aktuell
        /// vom ViewModel gehalten wird.
        /// </summary>
        /// <param name="currentReminder">Die aktuell gehaltene Reminder-Instanz.</param>
        /// <param name="newReminder">Die Reminder-Instanz mit den neuen Daten.</param>
        private void updateViewRelatedPropertiesOfReminder(Reminder currentReminder, Reminder newReminder)
        {
            currentReminder.StartDate = newReminder.StartDate;
            currentReminder.EndDate = newReminder.EndDate;
            currentReminder.Interval = newReminder.Interval;
            currentReminder.Ignore = newReminder.Ignore;
            currentReminder.IsActive = newReminder.IsActive;
            currentReminder.Text = newReminder.Text;
            currentReminder.Title = newReminder.Title;
            currentReminder.MessagePriority = newReminder.MessagePriority;
            currentReminder.ModificationDate = newReminder.ModificationDate;
            currentReminder.AuthorId = newReminder.AuthorId;

            currentReminder.ComputeFirstNextDate();
            currentReminder.EvaluateIsExpired();
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsmethode, welche die Ausführbarkeit von Befehlen abhängig vom aktuellen
        /// View Zustand evaluiert.
        /// </summary>
        private void checkCommandExecution()
        {
            SwitchToEditReminderDialogCommand.RaiseCanExecuteChanged();
            DeleteReminderCommand.OnCanExecuteChanged();
            ActivateReminderCommand.OnCanExecuteChanged();
            DeactivateReminderCommand.OnCanExecuteChanged();
            SynchronizeReminderCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Befehl SwitchToEditReminderCommand abhängig vom aktuellen
        /// Zustand der View zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSwitchToEditReminderCommand()
        {
            if (SelectedChannel != null && SelectedReminder != null
                && !SelectedChannel.Deleted)
            {
                if (SelectedReminder.IsActive != null 
                    && SelectedReminder.IsActive == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToEditReminderCommand aus. Wechselt auf den Dialog
        /// zum Bearbeiten des Reminder.
        /// </summary>
        private void executeSwitchToEditReminderCommand()
        {
            if (SelectedReminder != null && SelectedChannel != null)
            {
                string navigationParameter = 
                    "navParam?channelId=" + SelectedChannel.Id + "?reminderId=" + SelectedReminder.Id;
                _navService.Navigate("AddAndEditReminder", navigationParameter);
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Löschen des Reminders abhängig vom aktuellen
        /// View Zustand ausgeführt werden kann.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canDeleteReminder()
        {
            if (SelectedChannel != null && SelectedReminder != null
                && !SelectedChannel.Deleted)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl DeleteReminderCommand aus.
        /// </summary>
        private async Task executeDeleteReminderCommand()
        {
            if (SelectedChannel == null || SelectedReminder == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();

                await channelController.DeleteReminderAsync(SelectedChannel.Id, SelectedReminder.Id);

                // Bei erfolgreicher Löschung, navigiere zurück zum ModeratorChannelDetails View.
                if (_navService.CanGoBack())
                {
                    _navService.GoBack();
                }
                
            }
            catch (ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl ActivateReminderCommand ausgeführt werden kann 
        /// basierend auf dem aktuellen Zustand der View.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canActivateReminder()
        {
            if (SelectedChannel != null && SelectedReminder != null
                && !SelectedChannel.Deleted)
            {
                if (SelectedReminder.IsActive != null
                    && SelectedReminder.IsActive == false)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl ActivateReminderCommand aus. Aktiviert den aktuell
        /// gewählten Reminder.
        /// </summary>
        private async Task executeActivateReminderCommand()
        {
            if (SelectedChannel == null || SelectedReminder == null)
                return;

            displayIndeterminateProgressIndicator("StatusBarInformationActivating");
            try
            {
                IsActivateReminderFlyoutOpen = true;        // Änderung von true auf false, sonst wird Änderung nicht bekanntgegeben.
                IsActivateReminderFlyoutOpen = false;

                bool successful = await channelController.ChangeReminderActiveStatusAsync(
                    SelectedChannel.Id,
                    SelectedReminder.Id,
                    true);

                if (successful)
                {
                    SelectedReminder.IsActive = true;
                    checkCommandExecution();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Failed to execute ActivateReminderCommand." + 
                    " Error code is: {0}.", ex.ErrorCode);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl DeactivateReminderCommand ausgeführt werden kann 
        /// basierend auf dem aktuellen Zustand der View.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canDeactivateReminder()
        {
            if (SelectedChannel != null && SelectedReminder != null
                && !SelectedChannel.Deleted)
            {
                if (SelectedReminder.IsActive != null
                    && SelectedReminder.IsActive == true)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl DeactivateReminderCommand aus. Deaktiviert den aktuell
        /// gewählten Reminder.
        /// </summary>
        private async Task executeDeactivateReminderCommand()
        {
            if (SelectedChannel == null || SelectedReminder == null)
                return;

            displayIndeterminateProgressIndicator("StatusBarInformationDeactivating");
            try
            {
                IsDeactivateReminderFlyoutOpen = true;  // Änderung von true auf false, sonst wird Änderung nicht bekanntgegeben.
                IsDeactivateReminderFlyoutOpen = false;

                bool successful = await channelController.ChangeReminderActiveStatusAsync(
                    SelectedChannel.Id,
                    SelectedReminder.Id,
                    false);

                if (successful)
                {
                    SelectedReminder.IsActive = false;
                    checkCommandExecution();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Failed to execute DeactivateReminderCommand." +
                    " Error code is: {0}.", ex.ErrorCode);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zur Synchronisation der Reminderdaten abhängig
        /// vom aktuellen Zustand der View zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeReminder()
        {
            if (SelectedChannel != null && SelectedReminder != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeReminderCommand aus. Stößt eine Synchronisation
        /// der Reminderdaten mit den Serverdaten an.
        /// </summary>
        private async Task executeSynchronizeReminderCommand()
        {
            if (SelectedChannel == null || SelectedReminder == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();

                await synchronizeReminderAsync(SelectedChannel.Id, SelectedReminder.Id);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeReminderCommand: Error occurred. Error code is: {0}.",
                    ex.ErrorCode);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
        #endregion CommandFunctionality
    }
}
