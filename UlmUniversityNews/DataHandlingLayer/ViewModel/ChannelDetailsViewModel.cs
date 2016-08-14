using DataHandlingLayer.Controller;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.Common;

namespace DataHandlingLayer.ViewModel
{
    public class ChannelDetailsViewModel : ChannelDetailsBaseViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        private bool channelSubscribedStatus;
        /// <summary>
        /// Gibt an, ob der Kanal vom lokalen Nutzer abonniert ist oder nicht.
        /// </summary>
        public bool ChannelSubscribedStatus
        {
            get { return channelSubscribedStatus; }
            set 
            { 
                this.setProperty(ref this.channelSubscribedStatus, value);
                checkCommandExecution();
            }
        }

        private bool canUnsubscribeChannel;
        /// <summary>
        /// Gibt an, ob man den Kanal deabonnieren kann.
        /// </summary>
        public bool CanUnsubscribeChannel
        {
            get { return canUnsubscribeChannel; }
            set { this.setProperty(ref this.canUnsubscribeChannel, value); }
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

        private bool isDeletionNotificationOpen;
        /// <summary>
        /// Gibt an, ob das Flyout zur Anzeige der Benachrichtigung über die Löschung des Kanals
        /// aktuell angezeigt wird.
        /// </summary>
        public bool IsDeletionNotificationOpen
        {
            get { return isDeletionNotificationOpen; }
            set { this.setProperty(ref this.isDeletionNotificationOpen, value); }
        }

        private bool isUpdateAnnouncementsPossible;
        /// <summary>
        /// Gibt an, ob mit dem aktuellen Zustand der View eine Online-Aktualisierung der Announcements
        /// des Kanals möglich ist.
        /// </summary>
        public bool IsUpdateAnnouncementsPossible
        {
            get { return isUpdateAnnouncementsPossible; }
            set { this.setProperty(ref this.isUpdateAnnouncementsPossible, value); }
        }

        private bool isChannelInfoSynchronisationPossible;
        /// <summary>
        /// Gibt an, ob mit dem aktuellen Zustand der View eine Synchronisation der Kanal- und
        /// Moderatoreninformationen des Kanals mit den Serverdaten möglich ist.
        /// </summary>
        public bool IsChannelInfoSynchronisationPossible
        {
            get { return isChannelInfoSynchronisationPossible; }
            set { this.setProperty(ref this.isChannelInfoSynchronisationPossible, value); }
        }
                
        private bool isDeleteChannelWarningFlyoutOpen;
        /// <summary>
        /// Gibt an, ob aktuell das Flyout zur Anzeige des Warnhinweises bezüglich der Löschoperation
        /// des Kanals aus den lokalen Datensätzen aktuell aktiv ist.
        /// </summary>
        public bool IsDeleteChannelWarningFlyoutOpen
        {
            get { return isDeleteChannelWarningFlyoutOpen; }
            set { this.setProperty(ref this.isDeleteChannelWarningFlyoutOpen, value); }
        } 
        #endregion Properties

        #region Commands
        private AsyncRelayCommand subscribeChannelCommand;
        /// <summary>
        /// Der Befehl wird gefeuert, wenn der aktuell angezeigte Kanal abonniert werden soll.
        /// </summary>
        public AsyncRelayCommand SubscribeChannelCommand
        {
            get { return subscribeChannelCommand; }
            set { subscribeChannelCommand = value; }
        }

        private AsyncRelayCommand unsubscribeChannelCommand;
        /// <summary>
        /// Der Befehl wird gefeuert, wenn der aktuell angezeigte Kanal deabonniert werden soll.
        /// </summary>
        public AsyncRelayCommand UnsubscribeChannelCommand
        {
            get { return unsubscribeChannelCommand; }
            set { unsubscribeChannelCommand = value; }
        }

        private AsyncRelayCommand updateAnnouncementsCommand;
        /// <summary>
        /// Der Befehl löst die Aktualisierung der Announcements des Kanals aus.
        /// </summary>
        public AsyncRelayCommand UpdateAnnouncementsCommand
        {
            get { return updateAnnouncementsCommand; }
            set { updateAnnouncementsCommand = value; }
        }

        private AsyncRelayCommand synchronizeChannelInformationCommand;
        /// <summary>
        /// Der Befehl löst die Synchronisation der Kanal- und Moderatoreninformationen des Kanals
        /// mit den Serverdaten aus.
        /// </summary>
        public AsyncRelayCommand SynchronizeChannelInformationCommand
        {
            get { return synchronizeChannelInformationCommand; }
            set { synchronizeChannelInformationCommand = value; }
        }
        
        private RelayCommand switchToChannelSettingsCommand;
        /// <summary>
        /// Befehl zum Wechseln auf die ChannelSettings View.
        /// </summary>
        public RelayCommand SwitchToChannelSettingsCommand
        {
            get { return switchToChannelSettingsCommand; }
            set { switchToChannelSettingsCommand = value; }
        }
        
        private RelayCommand openDeleteChannelLocallyFlyoutCommand;
        /// <summary>
        /// Befehl zum Anzeigen des Warnhinweis bezüglich des lokalen Löschen eines als gelöscht 
        /// markierten Kanals.
        /// </summary>
        public RelayCommand OpenDeleteChannelLocallyFlyoutCommand
        {
            get { return openDeleteChannelLocallyFlyoutCommand; }
            set { openDeleteChannelLocallyFlyoutCommand = value; }
        }

        private RelayCommand deleteChannelLocallyFlyoutCommand;
        /// <summary>
        /// Befehl zum lokalen Löschen eines als gelöscht markierten Kanals.
        /// </summary>
        public RelayCommand DeleteChannelLocallyFlyoutCommand
        {
            get { return deleteChannelLocallyFlyoutCommand; }
            set { deleteChannelLocallyFlyoutCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeuge eine Instanz von der Klasse ChannelDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorReporter">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ChannelDetailsViewModel(INavigationService navService, IErrorMapper errorReporter)
            : base(navService, errorReporter)
        {
            // Initialisiere Befehle.
            SubscribeChannelCommand = new AsyncRelayCommand(
                param => executeSubscribeChannel(),
                param => canSubscribeChannel());
            UnsubscribeChannelCommand = new AsyncRelayCommand(
                param => executeUnsubscribeChannel());
            UpdateAnnouncementsCommand = new AsyncRelayCommand(
                param => executeUpdateAnnouncementsCommand(),
                param => canUpdateAnnouncements());
            SynchronizeChannelInformationCommand = new AsyncRelayCommand(
                param => executeSynchronizeChannelInformationCommand(),
                param => canSynchronizeChannelInformation());
            SwitchToChannelSettingsCommand = new RelayCommand(
                param => executeSwitchToChannelSettingsCommand(),
                param => canSwitchToChannelSettings());
            OpenDeleteChannelLocallyFlyoutCommand = new RelayCommand(
                param => executeOpenDeleteChannelLocallyFlyoutCommand(),
                param => canOpenDeleteChannelLocallyFlyout());
            DeleteChannelLocallyFlyoutCommand = new RelayCommand(
                param => executeDeleteChannelLocallyCommand(),
                param => canDeleteChannelLocally());
        }

        ///// <summary>
        ///// Lädt die Daten des gewählten Kanals in die Properties der ViewModel Instanz.
        ///// </summary>
        ///// <param name="selectedChannel">Der gewählte Kanal als Objekt.</param>
        //public void LoadSelectedChannel(object selectedChannel)
        //{
        //    if (selectedChannel != null)
        //    {
        //        Channel = selectedChannel as Channel;

        //        if(Channel != null)
        //        {
        //            switch (Channel.Type)
        //            {
        //                case ChannelType.LECTURE:
        //                    Lecture = selectedChannel as Lecture;
        //                    break;
        //                case ChannelType.EVENT:
        //                    EventObj = selectedChannel as Event;
        //                    break;
        //                case ChannelType.SPORTS:
        //                    Sports = selectedChannel as Sports;
        //                    break;
        //                default:
        //                    Debug.WriteLine("It is a channel of type Student_Group or Other with no special properties.");
        //                    break;
        //            }

        //            // Prüfe, ob Kanal bereits abonniert wurde.
        //            ChannelSubscribedStatus = channelController.IsChannelSubscribed(Channel.Id);
        //        }
        //    }
        //}

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
            catch(ClientException ex)
            {
                // Zeige Fehlernachricht an.
                displayError(ex.ErrorCode);
            }
                        
            if(Channel != null)
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

                // Prüfe, ob Kanal bereits abonniert wurde.
                ChannelSubscribedStatus = channelController.IsChannelSubscribed(Channel.Id);

                if (ChannelSubscribedStatus && !Channel.Deleted)
                {
                    CanUnsubscribeChannel = true;
                }
                else
                {
                    CanUnsubscribeChannel = false;
                }

                if(ChannelSubscribedStatus == true)
                {
                    // Aktiviere dynamisches Laden der Announcements.
                    // Es sollen mindestens immer alle noch nicht gelesenen Nachrichten geladen werden, immer aber mindestens 20.
                    int numberOfItems = Channel.NumberOfUnreadAnnouncements;
                    if(numberOfItems < 20)
                    {
                        numberOfItems = 20;
                    }

                    Debug.WriteLine("Call constructor of IncrementalLoadingCollection. Selected channel id is {0}.", selectedChannelId);
                    Announcements = new IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement>(selectedChannelId, numberOfItems);
                }

                checkCommandExecution();
            }
        }

        /// <summary>
        /// Markiere die Announcements dieses Kanals als gelesen.
        /// </summary>
        public void MarkAnnouncementsAsRead()
        {
            // Markiere ungelesene Nachrichten nun als gelesen.
            channelController.MarkAnnouncementsAsRead(Channel.Id);
        }

        /// <summary>
        /// Prüft, ob der Kanal lokal als gelöscht markiert ist und zeigt falls notwendig
        /// eine Benachrichtigung für den Nutzer an.
        /// </summary>
        /// <returns></returns>
        public void CheckWhetherChannelIsDeleted()
        {
            if (Channel == null)
                return;

            if (Channel.Deleted)
            {
                showChannelDeletionNotification();
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

            // Zeige Warnhinweis an.
            IsDeletionNotificationOpen = true;
        }

        /// <summary>
        /// Zeigt, soweit für den Kanal aktiviert, eine Benachrichtigung über die Löschung
        /// des Kanals für den Nutzer an.
        /// </summary>
        private void showChannelDeletionNotification()
        {
            if (Channel == null)
                return;

            bool notificationRequired = channelController.IsNotificationAboutDeletionRequired(Channel.Id);
            if (notificationRequired)
            {
                IsDeletionNotificationOpen = true;

                // Deaktiviere zukünftige Benachrichtigungen.
                channelController.DisableNotificationAboutDeletion(Channel.Id);
            }
        }

        #region CommandFunctionality
        /// <summary>
        /// Eine Hilfsmethode, die nach einer Statusänderung des Pivot Elements prüft,
        /// ob noch alle Kommandos ausgeführt werden können.
        /// </summary>
        private void checkCommandExecution()
        {
            SwitchToChannelSettingsCommand.RaiseCanExecuteChanged();
            SubscribeChannelCommand.OnCanExecuteChanged();
            UpdateAnnouncementsCommand.OnCanExecuteChanged();
            OpenDeleteChannelLocallyFlyoutCommand.RaiseCanExecuteChanged();
            SynchronizeChannelInformationCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Kanal aktuell abonniert werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn der Kanal abonniert werden kann, ansonsten false.</returns>
        private bool canSubscribeChannel()
        {
            //  Prüfe nicht auf SelectedPivotItemIndex == 1, da das Nachrichten PivotElement entfernt wird bei ChannelSubscribedStatus == false.
            // In "Kanalinformationen" PivotItem und der Kanal wurde noch nicht abonniert, Kanal nicht als gelöscht markiert.
            if (Channel != null &&
                ChannelSubscribedStatus == false &&
                Channel.Deleted == false)    
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stößt den Abonnementvorgang an. Der lokale Nutzer abonniert den Kanal, der aktuell
        /// in der Detailansicht angezeigt wird.
        /// </summary>
        private async Task executeSubscribeChannel()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                await channelController.SubscribeChannelAsync(Channel.Id);

                //Setze Kanal als abonniert.
                ChannelSubscribedStatus = true;

                if (ChannelSubscribedStatus && !Channel.Deleted)
                {
                    CanUnsubscribeChannel = true;
                }
                else
                {
                    CanUnsubscribeChannel = false;
                }

                // Lade Moderatoren-Info.
                await LoadModeratorsOfChannelAsync();

                // Bleibe auf der Seite, aber lade die Nachrichten nach.
                List<Announcement> announcements = await Task.Run(() => channelController.GetAllAnnouncementsOfChannel(Channel.Id));
                     
                // Setze PageSize auf 0, d.h. lade keine Elemente nach.
                Announcements = new IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement>(Channel.Id, 0);
                await Announcements.LoadExistingCollectionAsync(announcements);
            }
            catch(ClientException ex)
            {
                // Markiere Kanal in lokaler Liste als gelöscht, wenn er nicht auf dem Server gefunden wurde.
                if(ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    // Passe View an.
                    Channel.Deleted = true;
                    checkCommandExecution();
                }

                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Stößt den Deabonnementvorgang an. Der lokale Nutzer deabonniert den Kanal, der aktuell
        /// in der Detailansicht angezeigt wird.
        /// </summary>
        private async Task executeUnsubscribeChannel()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                await channelController.UnsubscribeChannelAsync(Channel.Id);
                ChannelSubscribedStatus = false;
                // Gehe zurück auf den Homescreen.
                _navService.Navigate("Homescreen");

                // Lösche den letzten Back-Stack Eintrag, so dass nicht auf die Detail-Seite
                // per Back-Key gewechselt werden kann.
                _navService.RemoveEntryFromBackStack();
            }
            catch(ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl UpdateAnnouncements ausgeführt werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn das Kommando ausgeführt werden kann, ansonsten false.</returns>
        private bool canUpdateAnnouncements()
        {
            // Wenn Kanal abonniert und nicht als gelöscht markiert ist.
            if(Channel != null &&
               SelectedPivotItemIndex == 0 && 
               channelSubscribedStatus == true &&
               Channel.Deleted == false)
            {
                IsUpdateAnnouncementsPossible = true;
                return true;
            }
            IsUpdateAnnouncementsPossible = false;
            return false;
        }

        /// <summary>
        /// Wird durch das Kommando UpdateAnnouncements ausgelöst und stößt die Aktualisierung
        /// der Announcements des Kanals an.
        /// </summary>
        private async Task executeUpdateAnnouncementsCommand()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                // Kein caching hier. Der Request soll jedes mal auch tatsächlich abgesetzt werden, wenn der Benutzer es will.
                await updateAnnouncementsAsync(false);   
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Something went wrong during updateAnnouncments. The message is: {0}", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>    
        /// Gibt an, ob der Befehl SynchronizeChannelInformation ausgeführt werden kann.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl ausgeführt werden kann, ansonsten false.</returns>
        private bool canSynchronizeChannelInformation()
        {
            if (Channel != null && 
                SelectedPivotItemIndex == 1 &&
                Channel.Deleted == false)
            {
                IsChannelInfoSynchronisationPossible = true;
                return true;
            }
            IsChannelInfoSynchronisationPossible = false;
            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeChannelInformationCommand aus. Stößt die Synchronisation
        /// der Kanal- und Moderatoreninformationen des gewählten Kanals an.
        /// </summary>
        private async Task executeSynchronizeChannelInformationCommand()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                await synchroniseChannelInformationAsync();
            }
            catch (ClientException ex)
            {
                if (ex.ErrorCode == ErrorCodes.ChannelNotFound)
                {
                    // Markiere Kanal als gelöscht.
                    Channel.Deleted = true;
                    checkCommandExecution();
                }

                Debug.WriteLine("executeSynchronizeChannelInformationCommand: Something went wrong during execution. " + 
                    "The message is: {0}", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob aktuell auf die ChannelSettings View gewechselt werden kann.
        /// </summary>
        /// <returns>Liefert true, falls die Navigation erlaubt ist, ansonsten false.</returns>
        private bool canSwitchToChannelSettings()
        {
            // Wenn der Kanal abonniert wurde und nicht als gelöscht markiert ist.
            if(Channel != null && 
                ChannelSubscribedStatus && 
                Channel.Deleted == false)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Wechselt auf die View ChannelSettings.
        /// </summary>
        private void executeSwitchToChannelSettingsCommand()
        {
            if(Channel != null)
            {
                _navService.Navigate("ChannelSettings", Channel.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Anzeigen des Warnhinweises
        /// bezüglich der Löschoperation des Kanals aus den lokalen
        /// Datensätzen zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canOpenDeleteChannelLocallyFlyout()
        {
            if (Channel != null &&
                Channel.Deleted)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl OpenDeleteChannelLocallyFlyoutCommand aus. Zeigt einen
        /// Warnhinweis bezüglich der Löschung der lokalen Kanalressource an. Macht dadurch auch
        /// den Befehl DeleteChannelLocallyCommand verfügbar.
        /// </summary>
        private void executeOpenDeleteChannelLocallyFlyoutCommand()
        {
            IsDeleteChannelWarningFlyoutOpen = true;
        }
        

        /// <summary>
        /// Gibt an, ob der Befehl zum Löschen eines Kanals aus
        /// den lokalen Datensätzen zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canDeleteChannelLocally()
        {
            if (Channel != null && 
                Channel.Deleted)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl DeleteChannelLocallyCommand aus. Löscht den
        /// aktuell markierten Kanal aus den lokalen Datensätzen.
        /// </summary>
        private void executeDeleteChannelLocallyCommand()
        {
            if (Channel == null)
                return;

            try
            {
                IsDeleteChannelWarningFlyoutOpen = false;
                channelController.DeleteLocalChannel(Channel.Id);

                // Navigiere zum Homescreen.
                _navService.Navigate("Homescreen");

                // Lösche letzten Back-Stack Eintrag, so dass nicht auf diese Seite zurück navigiert werden kann
                // mittels des Back-Key.
                _navService.RemoveEntryFromBackStack();
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeDeleteChannelLocallyCommand: Command execution failed.");
                displayError(ex.ErrorCode);
            }
        }
        #endregion CommandFunctionality
    }
}
