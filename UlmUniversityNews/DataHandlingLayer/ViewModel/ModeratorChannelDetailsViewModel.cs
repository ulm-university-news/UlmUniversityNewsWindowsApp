using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Common;
using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class ModeratorChannelDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Eine Datenstruktur, die eine schnelle Überprüfung der aktuell im ViewModel 
        /// verwalteten Reminder ermöglicht.
        /// </summary>
        private Dictionary<int, Reminder> reminderLookup;
        #endregion Fields

        #region Properties
        private Channel channel;
        /// <summary>
        /// Der Kanal, dessen Details angezeigt werden sollen.
        /// </summary>
        public Channel Channel
        {
            get { return channel; }
            set
            {
                this.setProperty(ref this.channel, value);
                checkCommandExecution();
            }
        }

        private Lecture lecture;
        /// <summary>
        /// Eine Instanz der Klasse Lecture, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Lecture hat. 
        /// </summary>
        public Lecture Lecture
        {
            get { return lecture; }
            set { this.setProperty(ref this.lecture, value); }
        }

        private Sports sports;
        /// <summary>
        /// Eine Instanz der Klasse Sports, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Sports hat.
        /// </summary>
        public Sports Sports
        {
            get { return sports; }
            set { this.setProperty(ref this.sports, value); }
        }

        private Event eventObj;
        /// <summary>
        /// Eine Instanz der Klasse Sports, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Event hat.
        /// </summary>
        public Event EventObj
        {
            get { return eventObj; }
            set { this.setProperty(ref this.eventObj, value); }
        }

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

        private int listRotationAngle;
        /// <summary>
        /// Der Winkel, um den die Liste mit den Announcements gedreht wird.
        /// Der Winkel wird verwendet, um die Anordnung der Announcements (von oben nach untern, von unten nach oben)
        /// zu realisieren.
        /// </summary>
        public int ListRotationAngle
        {
            get { return listRotationAngle; }
            set { this.setProperty(ref this.listRotationAngle, value); }
        }

        private bool showScrollBar;
        /// <summary>
        /// Gibt an, ob die ScrollBar Leiste eingeblendet werden soll, oder ob sie ausgeblendet werden soll.
        /// </summary>
        public bool ShowScrollBar
        {
            get { return showScrollBar; }
            set { showScrollBar = value; }
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
        
        private IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement> announcements = null;
        /// <summary>
        /// Die zum Kanal gehörenden Announcements in einer Collection. Hierbei handelt es sich um eine Collection,
        /// die dynamisches Laden von Announcements ermöglicht.
        /// </summary>
        public IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement> Announcements
        {
            get { return announcements; }
            set { this.setProperty(ref this.announcements, value); }
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
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorChannelDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper"></param>
        public ModeratorChannelDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController();

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

            // Lade Anwendungseinstellungen und passe View Parameter entsprechend an.
            AppSettings appSettings = channelController.GetApplicationSettings();
            if (appSettings.AnnouncementOrderSetting == OrderOption.ASCENDING)
            {
                ListRotationAngle = 0;
                ShowScrollBar = true;
            }
            else if (appSettings.AnnouncementOrderSetting == OrderOption.DESCENDING)
            {
                ListRotationAngle = 180;
                ShowScrollBar = false;
            }
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
        }

        /// <summary>
        /// Führe eine Aktualisierung der Announcements durch. Es wird beim
        /// Server nach neuen Announcements für den Kanal angefragt.
        /// </summary>
        public async Task PerformAnnouncementUpdate()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                await this.updateAnnouncements(false);
            }
            catch (ClientException ex)
            {
                // bei Fehler keine Nachricht an Nutzer, da Operation im Hintergrund ausgeführt wird.
                Debug.WriteLine("ClientException occurred during updateAnnouncements. Error code is: {0}.", ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Lädt die Reminder für den gewählten Kanal aus der lokalen Datenbank.
        /// </summary>
        public async Task LoadRemindersOfChannel()
        {
            if (Channel == null)
                return;

            try
            {
                List<Reminder> reminderList = await Task.Run(() => channelController.GetRemindersOfChannel(Channel.Id));

                reminderList = new List<Reminder>(
                    from reminder in reminderList
                    orderby reminder.Title
                    select reminder
                );

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
        /// Führe eine Synchronisation der lokalen Datensätze mit den Datensätzen auf dem
        /// Server aus. Aktualisiere lokale Datensätze falls notwendig.
        /// </summary>
        public async Task SynchroniseRemindersWithServer()
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
                Task updateTask = Task.Run(() => channelController.UpdateLocalReminders(reminderListServer, Channel.Id));

                List<Reminder> reminderList = reminderListServer;
                // Sortiere reminderList.
                reminderList = new List<Reminder>(
                    from reminder in reminderList
                     orderby reminder.Title
                     select reminder
                );

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
                        // TODO - prüfe, ob der Reminder aktualisiert wurde.
                    }
                }

                // Warte noch auf das Beenden der Aktualisierungen der lokalen Datensätze.
                await updateTask;
            }
            catch (ClientException ex)
            {
                // Zeige Fehler hier zunächst nicht an.
                Debug.WriteLine("Synchronization of reminders failed. Error code is: {0}.", ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Eine Hilfsmethode, die die Aktualisierung der Announcements des aktuellen Kanals ausführt.
        /// </summary>
        /// <param name="withCaching">Gibt an, ob der Request bei mehrfachen gleichen Requests innerhalb eines Zeitraums erneut ausgeführt werden soll,
        ///     oder ob der Eintrag aus dem Cache verwendet werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn die Aktualisierung der Announcements fehlschlägt.</exception>
        private async Task updateAnnouncements(bool withCaching)
        {
            if (Channel == null)
                return;

            // Extrahiere als erstes die aktuell höchste MessageNr einer Announcement in diesem Kanal.
            int maxMsgNr = 0;
            maxMsgNr = channelController.GetHighestMsgNrForChannel(Channel.Id);
            Debug.WriteLine("Perform update announcement operation with max messageNumber of {0}.", maxMsgNr);

            // Frage die Announcements ab.
            List<Announcement> receivedAnnouncements = await channelController.GetAnnouncementsOfChannelAsync(Channel.Id, maxMsgNr, withCaching);

            if (receivedAnnouncements != null && receivedAnnouncements.Count > 0)
            {
                await Task.Run(() => channelController.StoreReceivedAnnouncementsAsync(receivedAnnouncements));

                foreach (Announcement announcement in receivedAnnouncements)
                {
                    Announcements.Insert(0, announcement);
                }
            }
        }

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
            
            if (SelectedPivotItemIndex == 2)    // Channel-Details Pivotitem gewählt.
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
            if (SelectedPivotItemIndex == 0)
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
            if (SelectedPivotItemIndex == 2)
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
            if (SelectedPivotItemIndex == 1)
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
    }
}
