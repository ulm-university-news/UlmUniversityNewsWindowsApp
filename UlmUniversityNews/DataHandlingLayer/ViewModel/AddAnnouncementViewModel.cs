using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    public class AddAnnouncementViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ChannelController.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        #region Properties
        private Channel selectedChannel;
        /// <summary>
        /// Der aktuell ausgewählte Kanal, für den die Announcement erstellt werden soll.
        /// </summary>
        public Channel SelectedChannel
        {
            get { return selectedChannel; }
            set { this.setProperty(ref this.selectedChannel, value); }
        }

        private string announcementTitle;
        /// <summary>
        /// Der Titel der neu hinzuzufügenden Announcement Nachricht.
        /// </summary>
        public string AnnouncementTitle
        {
            get { return announcementTitle; }
            set { this.setProperty(ref this.announcementTitle, value); }
        }

        private string announcementContent;
        /// <summary>
        /// Der Inhalt der neu hinzuzufügenden Announcement Nachricht.
        /// </summary>
        public string AnnouncementContent
        {
            get { return announcementContent; }
            set { this.setProperty(ref this.announcementContent, value); }
        }

        private bool isMessagePriorityHighSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Nachrichtenpriorität 'hoch' gewählt ist.
        /// </summary>
        public bool IsMessagePriorityHighSelected
        {
            get { return isMessagePriorityHighSelected; }
            set { this.setProperty(ref this.isMessagePriorityHighSelected, value); }
        }
        
        private bool isMessagePriorityNormalSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Nachrichtenpriorität 'normal' gewählt ist.
        /// </summary>
        public bool IsMessagePriorityNormalSelected
        {
            get { return isMessagePriorityNormalSelected; }
            set { this.setProperty(ref this.isMessagePriorityNormalSelected, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand createNewAnnouncementCommand;
        /// <summary>
        /// Befehl zum Erzeugen und Senden einer neuen Announcement Nachricht für den gewählten Kanal.
        /// </summary>
        public AsyncRelayCommand CreateNewAnnouncementCommand
        {
            get { return createNewAnnouncementCommand; }
            set { createNewAnnouncementCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAnnouncementViewModel
        /// </summary>
        /// <param name="navService"></param>
        /// <param name="errorMapper"></param>
        public AddAnnouncementViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController(this);

            // Initialisiere Parameter.
            IsMessagePriorityNormalSelected = true;
            IsMessagePriorityHighSelected = false;

            // Befehle anlegen.
            CreateNewAnnouncementCommand = new AsyncRelayCommand(param => executeCreateNewAnnouncementCommand());
        }

        /// <summary>
        /// Lädt den Kanal mit der angegebenen Id. Für diesen Kanal kann eine neue Announcement
        /// angelegt werden.
        /// </summary>
        /// <param name="channelId">Die Id des gewählten Kanals.</param>
        public async Task LoadSelectedChannel(int channelId)
        {
            try
            {
                SelectedChannel = await Task.Run(() => channelController.GetChannel(channelId));
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("The loading of the selected channel has failed. Msg is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Führt den Befehl CreateNewAnnouncementCommand aus. Legt eine neue Announcement Nachricht an.
        /// Die Nachricht wird auf dem Server erzeugt und von diesem an alle Abonnenten verteilt.
        /// </summary>
        private async Task executeCreateNewAnnouncementCommand()
        {
            if (SelectedChannel != null)
            {
                displayIndeterminateProgressIndicator();

                // Baue Announcement Objekt mit eingegebenen Daten.
                Priority messagePriority = Priority.NORMAL;
                if (IsMessagePriorityHighSelected)
                {
                    messagePriority = Priority.HIGH;
                }

                Announcement newAnnouncement = new Announcement()
                {
                    Text = AnnouncementContent,
                    Title = AnnouncementTitle,
                    MessagePriority = messagePriority
                };

                try
                {
                    // Starte Anlegen einer neuen Announcement.
                    bool successful = await channelController.CreateAnnouncementAsync(SelectedChannel.Id, newAnnouncement);

                    if (successful && _navService.CanGoBack())
                    {
                        // Gehe zurück auf Detailseite.
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
        }
    }
}
