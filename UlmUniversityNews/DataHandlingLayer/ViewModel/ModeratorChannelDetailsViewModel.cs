using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Common;

namespace DataHandlingLayer.ViewModel
{
    public class ModeratorChannelDetailsViewModel : ChannelDetailsBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Datenstruktur, die eine schnelle Überprüfung der aktuell im ViewModel 
        /// verwalteten Reminder ermöglicht.
        /// </summary>
        private Dictionary<int, Reminder> reminderLookup;
        #endregion Fields

        #region Properties        
        private int selectedPivotItemIndex;
        /// <summary>
        /// Gibt den Index des aktuell ausgewählten PivotItems an.
        /// Index 0 ist "Meine Kanäle", Index 1 ist "Meine Gruppen".
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

        private bool canDeleteChannel;
        /// <summary>
        /// Gibt an, ob aktuell der Befehl zum Löschen des Kanals zur Vefügung steht.
        /// </summary>
        public bool CanDeleteChannel
        {
            get { return canDeleteChannel; }
            set { this.setProperty(ref this.canDeleteChannel, value); }
        }
        
        private ObservableCollection<Reminder> reminders;
        /// <summary>
        /// Die zu dem Kanal gehörenden Reminder.
        /// </summary>
        public ObservableCollection<Reminder> Reminders
        {
            get { return reminders; }
            set { reminders = value; }
        }      
        #endregion Properties

        #region Commands
        private RelayCommand switchToAddAnnouncementDialogCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Dialog zur Erstellung einer Announcement Nachricht.
        /// </summary>
        public RelayCommand SwitchToAddAnnouncementDialogCommand
        {
            get { return switchToAddAnnouncementDialogCommand; }
            set { switchToAddAnnouncementDialogCommand = value; }
        }

        private RelayCommand switchToEditChannelDialogCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Dialog zur Bearbeitung eines Kanals.
        /// </summary>
        public RelayCommand SwitchToEditChannelDialogCommand
        {
            get { return switchToEditChannelDialogCommand; }
            set { switchToEditChannelDialogCommand = value; }
        }

        private RelayCommand switchToAddReminderDialogCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Dialog zur Erstellung eines Reminders.
        /// </summary>
        public RelayCommand SwitchToAddReminderDialogCommand
        {
            get { return switchToAddReminderDialogCommand; }
            set { switchToAddReminderDialogCommand = value; }
        }

        private RelayCommand reminderSelectedCommand;
        /// <summary>
        /// Befehl, der ausgeführt wird, wenn ein Reminder vom Nutzer gewählt wurde.
        /// </summary>
        public RelayCommand ReminderSelectedCommand
        {
            get { return reminderSelectedCommand; }
            set { reminderSelectedCommand = value; }
        }

        private AsyncRelayCommand deleteChannelCommand;
        /// <summary>
        /// Befehl zum Löschen eines Kanals.
        /// </summary>
        public AsyncRelayCommand DeleteChannelCommand
        {
            get { return deleteChannelCommand; }
            set { deleteChannelCommand = value; }
        }

        private AsyncRelayCommand synchronizeWithServerCommand;
        /// <summary>
        /// Befehl zum Aktualisieren der Informationen durch Synchronisation mit dem Server.
        /// </summary>
        public AsyncRelayCommand SynchronizeWithServerCommand
        {
            get { return synchronizeWithServerCommand; }
            set { synchronizeWithServerCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorChannelDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper"></param>
        public ModeratorChannelDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            Reminders = new ObservableCollection<Reminder>();
            reminderLookup = new Dictionary<int, Reminder>();

            // Erzeuge Befehle.
            SwitchToAddAnnouncementDialogCommand = new RelayCommand(
                param => executeSwitchToAddAnnouncementDialogCommand(),
                param => canSwitchToAddAnnouncementDialog());
            SwitchToEditChannelDialogCommand = new RelayCommand(
                param => executeSwitchToEditChannelDialogCommand(),
                param => canSwitchToEditChannelDialog());
            SwitchToAddReminderDialogCommand = new RelayCommand(
                param => executeSwitchToAddReminderDialogCommand(),
                param => canSwitchToAddReminderDialog());
            ReminderSelectedCommand = new RelayCommand(
                param => executeReminderSelectedCommand(param));
            DeleteChannelCommand = new AsyncRelayCommand(
                param => executeDeleteChannelCommand());
            SynchronizeWithServerCommand = new AsyncRelayCommand(
                param => executeSynchronizeWithServerCommand(),
                param => canPerformSynchronizationWithServer());
        }

        /// <summary>
        /// Lädt die Daten des Kanals mit der übergebenen Id in das ViewModel
        /// und macht die Daten über die Properties verfügbar.
        /// </summary>
        /// <param name="selectedChannelId">Die Id des Kanals, der geladen werden soll.</param>
        public void LoadSelectedChannel(int selectedChannelId)
        {
            try
            {
                Channel = channelController.GetChannel(selectedChannelId);
            }
            catch (ClientException ex)
            {
                // Zeige Fehlernachricht an.
                displayError(ex.ErrorCode);
            }

            if (Channel != null)
            {
                switch (Channel.Type)
                {
                    case ChannelType.LECTURE:
                        Debug.WriteLine("Selected channel is of type Lecture.");
                        Lecture = Channel as Lecture;
                        break;
                    case ChannelType.EVENT:
                        Debug.WriteLine("Selected channel is of type Event.");
                        EventObj = Channel as Event;
                        break;
                    case ChannelType.SPORTS:
                        Debug.WriteLine("Selected channel is of type Sports.");
                        Sports = Channel as Sports;
                        break;
                    default:
                        Debug.WriteLine("Selected channel is of type Student_Group or Other with no special properties.");
                        break;
                }
            }

            // Initialisiere asynchrones Announcement Laden.
            Announcements = new IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement>(selectedChannelId, 20);

            checkCommandExecution();
        }

        /// <summary>
        /// Lädt die Reminder für den gewählten Kanal aus der lokalen Datenbank.
        /// </summary>
        public async Task LoadRemindersOfChannelAsync()
        {
            if (Channel == null)
                return;

            try
            {
                List<Reminder> reminderList = await Task.Run(() => channelController.GetRemindersOfChannel(Channel.Id));

                reminderList = sortReminderListEntries(reminderList);

                foreach (Reminder reminder in reminderList)
                {
                    // Berechne nächsten Reminder Termin.
                    reminder.ComputeFirstNextDate();
                    // Prüfe, ob Reminder noch aktiv.
                    reminder.EvaluateIsExpired();

                    Reminders.Add(reminder);
                    reminderLookup.Add(reminder.Id, reminder);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Failed to load reminders for channel.");
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Ruft die neueste Liste von Reminder für den gegebenen Kanal vom Server ab
        /// und prüft, ob lokal entsprechend Reminder hinzugefügt oder aktualisiert werden müssen.
        /// </summary>
        public async Task CheckForMissingReminders()
        {
            if (Channel == null)
                return;

            displayIndeterminateProgressIndicator();
            try
            {
                List<Reminder> reminderListServer = await Task.Run(() => channelController.GetRemindersOfChannelAsync(Channel.Id, false));
                if (reminderListServer == null)
                    return;

                // Starte Aktualisierung der lokalen Datensätze asynchron.
                await Task.Run(() => channelController.AddOrUpdateLocalReminders(reminderListServer, Channel.Id));

                // Aktualisiere die im ViewModel gehaltene Liste von Reminders.
                await updateRemindersListAsync();
            }
            catch (ClientException ex)
            {
                // Zeige Fehler hier zunächst nicht an.
                Debug.WriteLine("CheckForMissingReminders: Check of missing reminders failed. Error code is: {0}.", ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Aktualisiere die Anzeige, wenn ein ChannelDeleted Event empfangen wurde,
        /// welches den gerade angezeigten Kanal betrifft.
        /// </summary>
        public void PerformViewUpdateOnChannelDeletedEvent()
        {
            if (Channel == null)
                return;

            // Setze Kanal auf gelöscht und prüfe Befehlsausführungen.
            Channel.Deleted = true;
            checkCommandExecution();
        }
        
        /// <summary>
        /// Sortiert die Liste von Remindern Objekten. Aktuell werden die Reminder alphabetisch
        /// nach ihrem Titel sortiert.
        /// </summary>
        /// <param name="reminderList">Die zu sortierende Liste von Remindern.</param>
        /// <returns>Eine sortierte Liste von Remindern.</returns>
        private List<Reminder> sortReminderListEntries(List<Reminder> reminderList)
        {               
            // Sortiere reminderList.
            reminderList = new List<Reminder>(
                from reminder in reminderList
                orderby reminder.Title
                select reminder
            );
            return reminderList;
        }

        /// <summary>
        /// Aktualisiert die im ViewModel gehaltene Liste von Reminder-Objekten. Synchronisiert die
        /// Liste mit den lokal in der Anwendung für den gegebenen Kanal verwalteten Reminder Objekten.
        /// </summary>
        private async Task updateRemindersListAsync()
        {
            try
            {
                List<Reminder> reminderList = await Task.Run(() => channelController.GetRemindersOfChannel(Channel.Id));

                reminderList = sortReminderListEntries(reminderList);

                // Prüfe, ob es einen Reminder gibt, der noch nicht in der View verwaltet wird.
                foreach (Reminder reminder in reminderList)
                {
                    if (!reminderLookup.ContainsKey(reminder.Id))
                    {
                        // Berechne nächsten Reminder Termin.
                        reminder.ComputeFirstNextDate();
                        // Prüfe, ob Reminder noch aktiv.
                        reminder.EvaluateIsExpired();

                        // Füge Reminder hinzu.
                        Reminders.Insert(reminderList.IndexOf(reminder), reminder);
                        reminderLookup.Add(reminder.Id, reminder);
                    }
                    else
                    {
                        // Prüfe, ob der Reminder aktualisiert wurde.
                        Reminder currentReminder = reminderLookup[reminder.Id];
                        if (DateTimeOffset.Compare(currentReminder.ModificationDate, reminder.ModificationDate) < 0)
                        {
                            // Aktualisiere relevante Properties für die View.
                            updateViewRelatedPropertiesOfReminder(currentReminder, reminder);
                        }
                        else if (currentReminder.Ignore != reminder.Ignore) // Hat sich das Ignore Flag geändert.
                        {
                            // Aktualisiere auch hier relevante Properties für die View.
                            updateViewRelatedPropertiesOfReminder(currentReminder, reminder);
                        }
                    }
                }

                // Prüfe, ob in Reminders ein Reminder-Objekt ist, welches lokal nicht mehr vorhanden ist.
                for (int i = 0; i < Reminders.Count; i++)
                {
                    bool isContained = false;

                    foreach (Reminder reminder in reminderList)
                    {
                        if (reminder.Id == Reminders[i].Id)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        Debug.WriteLine("Need to take reminder with id {0} from the list.", Reminders[i].Id);
                        reminderLookup.Remove(Reminders[i].Id);                     
                        Reminders.RemoveAt(i);
                    }
                }

            }
            catch (ClientException ex)
            {
                Debug.WriteLine("updateRemindersList: Error occurred. Error code is: {0}.", ex.ErrorCode);
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

        /// <summary>
        /// Führe eine Synchronisation der lokalen Datensätze mit den Datensätzen auf dem
        /// Server aus. Aktualisiere lokale Datensätze falls notwendig.
        /// </summary>
        /// <exception cref="ClientException">Wirft ClientException, wenn Synchronisation fehlschlägt.</exception>
        private async Task synchroniseRemindersWithServerAsync()
        {
            if (Channel == null)
                return;

            List<Reminder> reminderListServer = await Task.Run(() => channelController.GetRemindersOfChannelAsync(Channel.Id, false));
            if (reminderListServer == null)
                return;

            // Starte Aktualisierung der lokalen Datensätze asynchron.
            await Task.Run(() => channelController.SynchronizeLocalReminders(reminderListServer, Channel.Id));

            // Aktualisiere die im ViewModel gehaltene Liste von Reminders.
            await updateRemindersListAsync();
        }

        #region CommandFunctions
        /// <summary>
        /// Hilfsmethode, welche die Überprüfung der Ausführbarkeit der Befehle 
        /// anstößt. Kann verwendet werden, um die Ausführbarkeit nach einer Änderung des
        /// View Zustand zu überprüfen.
        /// </summary>
        private void checkCommandExecution()
        {
            SwitchToAddAnnouncementDialogCommand.RaiseCanExecuteChanged();
            SwitchToEditChannelDialogCommand.RaiseCanExecuteChanged();
            SwitchToAddReminderDialogCommand.RaiseCanExecuteChanged();
            
            if (SelectedPivotItemIndex == 2 && Channel != null)    // Channel-Details Pivotitem gewählt.
            {
                CanDeleteChannel = true;
            }
            else
            {
                CanDeleteChannel = false;
            }
        }

        /// <summary>
        /// Gibt an, ob nach dem aktuellen Zustand der View ein Wechsel auf den Dialog
        /// zur Erstellung einer Announcement Nachricht möglich ist.
        /// </summary>
        /// <returns>Liefert true, wenn der Wechsel möglich ist, ansonsten false.</returns>
        private bool canSwitchToAddAnnouncementDialog()
        {
            // Pivot Index 0 ist der Announcement-Tab 
            if (SelectedPivotItemIndex == 0 && Channel != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToAddAnnouncementDialogCommand aus. Wechselt auf den
        /// Dialog zur Erstellung einer Announcement-Nachricht.
        /// </summary>
        private void executeSwitchToAddAnnouncementDialogCommand()
        {
            if (Channel != null)
            {
                _navService.Navigate("AddAnnouncement", Channel.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob nach dem aktuellen Zustand der View ein Wechsel auf den Dialog
        /// zur Bearbeitung des gewählten Kanals möglich ist.
        /// </summary>
        /// <returns>Liefert true, wenn der Wechsel möglich ist, ansonsten false.</returns>
        private bool canSwitchToEditChannelDialog()
        {
            // Pivot Index 2 ist der Kanalinformationen-Tab.
            if (SelectedPivotItemIndex == 2 && Channel != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToEditChannelDialogCommand aus. Wechselt auf den
        /// Dialog zur Bearbeitung des gewählten Kanals.
        /// </summary>
        private void executeSwitchToEditChannelDialogCommand()
        {
            if (Channel != null)
            {
                _navService.Navigate("AddAndEditChannel", Channel.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob nach dem aktuellen Zustand der View ein Wechsel auf den Dialog
        /// zur Erstellung eines Reminder möglich ist.
        /// </summary>
        /// <returns>Liefert true, wenn Wechsel auf Dialog möglich ist, ansonsten false.</returns>
        private bool canSwitchToAddReminderDialog()
        {
            // Pivot Index 1 ist der Reminder-Tab
            if (SelectedPivotItemIndex == 1 && Channel != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SwitchToAddReminderDialogCommand aus. Wechselt auf den Dialog
        /// zur Erstellung eines Reminder. 
        /// </summary>
        private void executeSwitchToAddReminderDialogCommand()
        {
            if (Channel != null)
            {
                string navigationParameter = "navParam?channelId=" + Channel.Id;
                _navService.Navigate("AddAndEditReminder", navigationParameter);
            }
        }

        /// <summary>
        /// Ausführung des Befehls ReminderSelectedCommand. Leitet auf die
        /// Detailseite des gewählten Reminders weiter.
        /// </summary>
        /// <param name="selectedItem">Der gewählte Eintrag.</param>
        private void executeReminderSelectedCommand(object selectedItem)
        {
            Reminder selectedReminder = selectedItem as Reminder;
            if (selectedReminder != null)
            {
                _navService.Navigate("ReminderDetails", selectedReminder.Id);
            }
        }

        ///// <summary>
        ///// Gibt an, ob mit dem aktuellen Zustand der View der Befehl
        ///// "Kanal löschen" zur Verfügung steht. 
        ///// </summary>
        ///// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        //private bool canDeleteChannel()
        //{
        //    // Pivot Index 2 ist der Kanalinformationen-Tab.
        //    if (SelectedPivotItemIndex == 2)
        //    {
        //        return true;
        //    }
        //    return false;
        //}

        /// <summary>
        /// Führt den Befehl DeleteChannelCommand aus. Löscht den Kanal,
        /// der aktuell gewählt ist.
        /// </summary>
        private async Task executeDeleteChannelCommand()
        {
            if (Channel == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();

                await channelController.DeleteChannelAsync(Channel.Id);

                // Bei erfolgreicher Löschung, gehe zurück auf den Homescreen.
                _navService.Navigate("HomescreenModerator");

                // Lösche den letzten Eintrag aus dem BackStack, so dass nicht auf die Detailseite zurück navigiert
                // werden kann mittels des Back-Keys.
                _navService.RemoveEntryFromBackStack();
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error during DeleteChannelCommand execution. Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl SynchronizeWithServerCommand unter Berücksichtigung des
        /// aktuellen Zustands im ViewModel zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canPerformSynchronizationWithServer()
        {
            if (Channel != null)
                return true;

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeWithServerCommand aus. Aktualisiert entsprechend dem
        /// aktuellen View Zustand die Ressourcen.
        /// </summary>
        private async Task executeSynchronizeWithServerCommand()
        {
            if (Channel == null)
                return;

            try
            {
                switch (SelectedPivotItemIndex)
                {
                    case 0:
                        displayIndeterminateProgressIndicator("Synchronize messages");
                        // Abfrage, ob neue Announcement Nachrichten vorliegen.
                        Debug.WriteLine("executeSynchronizeWithServerCommand: Start to update announcements.");
                        await updateAnnouncementsAsync(false);
                        break;
                    case 1:
                        displayIndeterminateProgressIndicator("Synchronize reminders");
                        // Synchronisiere Reminder-Informationen mit dem Server.
                        Debug.WriteLine("executeSynchronizeWithServerCommand: Start to update reminders.");
                        await synchroniseRemindersWithServerAsync();
                        break;
                    case 2:
                        displayIndeterminateProgressIndicator("Synchronize channel information");
                        // Frage Informationen zu diesem Kanal ab.
                        Debug.WriteLine("executeSynchronizeWithServerCommand: Start to update channel and moderator info.");
                        await synchroniseChannelInformationAsync();
                        break;
                    default:
                        // Mache nichts.
                        break;
                }
            }
            catch (ClientException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    // Markiere Kanal als gelöscht.
                    Channel.Deleted = true;
                    checkCommandExecution();
                }

                Debug.WriteLine("Error during SynchronizeWithServerCommand execution. Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
        #endregion CommandFunctions
    }
}
