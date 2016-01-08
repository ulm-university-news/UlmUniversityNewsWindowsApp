﻿using DataHandlingLayer.Controller;
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
    public class ChannelDetailsViewModel : ViewModel
    {
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

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
                Debug.WriteLine("Channel subscribed status changed.");
                checkCommandExecution();
            }
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
                Debug.WriteLine("In setter method of selected pivot item index. The new index is: " + value);
                selectedPivotItemIndex = value;
                checkCommandExecution();
            }
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
        #endregion Properties

        #region Commands
        private AsyncRelayCommand subscribeChannelCommand;
        /// <summary>
        /// Das Kommando wird gefeuert, wenn der aktuell angezeigte Kanal abonniert werden soll.
        /// </summary>
        public AsyncRelayCommand SubscribeChannelCommand
        {
            get { return subscribeChannelCommand; }
            set { subscribeChannelCommand = value; }
        }

        private AsyncRelayCommand unsubscribeChannelCommand;
        /// <summary>
        /// Das Kommando wird gefeuert, wenn der aktuell angezeigte Kanal deabonniert werden soll.
        /// </summary>
        public AsyncRelayCommand UnsubscribeChannelCommand
        {
            get { return unsubscribeChannelCommand; }
            set { unsubscribeChannelCommand = value; }
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
            channelController = new ChannelController();

            // Initialisiere Kommandos.
            SubscribeChannelCommand = new AsyncRelayCommand(param => executeSubscribeChannel(), param => canSubscribeChannel());
            UnsubscribeChannelCommand = new AsyncRelayCommand(param => executeUnsubscribeChannel(), param => canUnsubscribeChannel());
        }

        /// <summary>
        /// Lädt die Daten des gewählten Kanals in die Properties der ViewModel Instanz.
        /// </summary>
        /// <param name="selectedChannel">Der gewählte Kanal als Objekt.</param>
        public void LoadSelectedChannel(object selectedChannel)
        {
            if (selectedChannel != null)
            {
                Channel = selectedChannel as Channel;

                if(Channel != null)
                {
                    switch (Channel.Type)
                    {
                        case ChannelType.LECTURE:
                            Lecture = selectedChannel as Lecture;
                            break;
                        case ChannelType.EVENT:
                            EventObj = selectedChannel as Event;
                            break;
                        case ChannelType.SPORTS:
                            Sports = selectedChannel as Sports;
                            break;
                        default:
                            Debug.WriteLine("It is a channel of type Student_Group or Other with no special properties.");
                            break;
                    }

                    // Prüfe, ob Kanal bereits abonniert wurde.
                    ChannelSubscribedStatus = channelController.IsChannelSubscribed(Channel.Id);
                }
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
            }
        }

        /// <summary>
        /// Markiere die Announcements dieses Kanals als gelesen.
        /// </summary>
        public async Task MarkAnnouncementsAsReadAsync()
        {
            await Task.Run(() => {
                    // Markiere ungelesene Nachrichten nun als gelesen.
                    channelController.MarkAnnouncementsAsRead(Channel.Id);
            });
        }

        /// <summary>
        /// Eine Hilfsmethode, die nach einer Statusänderung des Pivot Elements prüft,
        /// ob noch alle Kommandos ausgeführt werden können.
        /// </summary>
        private void checkCommandExecution()
        {
            SubscribeChannelCommand.OnCanExecuteChanged();
            UnsubscribeChannelCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Kanal aktuell abonniert werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn der Kanal abonniert werden kann, ansonsten false.</returns>
        private bool canSubscribeChannel()
        {
            //  Prüfe nicht auf SelectedPivotItemIndex == 1, da das Nachrichten PivotElement entfernt wird bei ChannelSubscribedStatus == false. 
            if (Channel != null &&
                ChannelSubscribedStatus == false)    // In "Kanalinformationen" PivotItem und der Kanal wurde noch nicht abonniert.
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
                displayProgressBar();
                await channelController.SubscribeChannelAsync(Channel.Id);

                //Setze Kanal als abonniert.
                ChannelSubscribedStatus = true;

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
                    Channel.Deleted = true;
                }

                displayError(ex.ErrorCode);
            }
            finally
            {
                hideProgressBar();
            }
        }

        /// <summary>
        /// Gibt an, ob der Kanal aktuell deabonniert werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn der Kanal deabonniert werden kann, ansonsten false.</returns>
        private bool canUnsubscribeChannel()
        {
            if (Channel != null &&
                SelectedPivotItemIndex == 1 &&
                ChannelSubscribedStatus == true)    // In "Kanalinformationen" PivotItem und der Kanal wurde bereits abonniert.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Stößt den Deabonnementvorgang an. Der lokale Nutzer deabonniert den Kanal, der aktuell
        /// in der Detailansicht angezeigt wird.
        /// </summary>
        private async Task executeUnsubscribeChannel()
        {
            try
            {
                displayProgressBar();
                await channelController.UnsubscribeChannelAsync(Channel.Id);
                ChannelSubscribedStatus = false;
                // Gehe zurück auf den Homescreen.
                _navService.Navigate("Homescreen");
            }
            catch(ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideProgressBar();
            }
        }
    }
}
