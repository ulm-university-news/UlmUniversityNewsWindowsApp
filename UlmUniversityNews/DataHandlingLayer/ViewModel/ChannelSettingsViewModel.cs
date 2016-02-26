using DataHandlingLayer.Controller;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class ChannelSettingsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der ChannelController Klasse.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        #region Properties
        private bool isNotificationOptionPrioAppDefaultSelected;
        /// <summary>
        /// Gibt an, ob die Option 'AppDefault' angewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioAppDefaultSelected
        {
            get { return isNotificationOptionPrioAppDefaultSelected; }
            set { this.setProperty(ref this.isNotificationOptionPrioAppDefaultSelected, value); }
        }

        private bool isNotificationOptionPrioHighSelected;
        /// <summary>
        /// Gibt an, ob die Option 'nur Priorität Hoch' angewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioHighSelected
        {
            get { return isNotificationOptionPrioHighSelected; }
            set { this.setProperty(ref this.isNotificationOptionPrioHighSelected, value); }
        }

        private bool isNotificationOptionAllSelected;
        /// <summary>
        /// Gibt an, ob die Option 'Alle Nachrichten' angewählt ist.
        /// </summary>
        public bool IsNotificationOptionAllSelected
        {
            get { return isNotificationOptionAllSelected; }
            set { this.setProperty(ref this.isNotificationOptionAllSelected, value); }
        }

        private bool isNotificationOptionNoneSelected;
        /// <summary>
        /// Gibt an, ob die Option 'Keine Nachrichten' angewählt ist.
        /// </summary>
        public bool IsNotificationOptionNoneSelected
        {
            get { return isNotificationOptionNoneSelected; }
            set { this.setProperty(ref this.isNotificationOptionNoneSelected, value); }
        }

        private Channel selectedChannel;
        /// <summary>
        /// Das Channel Objekt des gewählten Kanals.
        /// </summary>
        public Channel SelectedChannel
        {
            get { return selectedChannel; }
            set { this.setProperty(ref this.selectedChannel, value); }
        }
        #endregion Properties

        #region Commands
        private RelayCommand saveNotificationSettingsCommand;
        /// <summary>
        /// Befehl zum Speichern der Benachrichtigungseinstellungen für den gewählten Kanal. 
        /// </summary>
        public RelayCommand SaveNotificationSettingsCommand
        {
            get { return saveNotificationSettingsCommand; }
            set { saveNotificationSettingsCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelSettingsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ChannelSettingsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController();

            SaveNotificationSettingsCommand = new RelayCommand(param => executeSaveNotificationSettingsCommand());
        }

        /// <summary>
        /// Lädt die NotificationSettings für den Kanal mit der angegebnen Id. Initialisiert
        /// die View Parameter entsprechend den aktuellen Einstellungen des Kanals.
        /// </summary>
        /// <param name="selectedChannelId">Die Id des Kanals, der gewählt wurde.</param>
        public void LoadSettingsOfSelectedChannel(int selectedChannelId)
        {
            try
            {
                SelectedChannel = channelController.GetChannel(selectedChannelId);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("An error occurred during the loading process of the selected channel object.");
                // Zeige Fehlernachricht an.
                displayError(ex.ErrorCode);
            }

            if(SelectedChannel != null)
            {
                IsNotificationOptionAllSelected = false;
                IsNotificationOptionPrioAppDefaultSelected = false;
                IsNotificationOptionPrioHighSelected = false;
                IsNotificationOptionNoneSelected = false;

                switch (SelectedChannel.AnnouncementNotificationSetting)
                {
                    case NotificationSetting.ANNOUNCE_PRIORITY_HIGH:
                        IsNotificationOptionPrioHighSelected = true;
                        break;
                    case NotificationSetting.ANNOUNCE_ALL:
                        IsNotificationOptionAllSelected = true;
                        break;
                    case NotificationSetting.ANNOUNCE_NONE:
                        IsNotificationOptionNoneSelected = true;
                        break;
                    case NotificationSetting.APPLICATION_DEFAULT:
                        IsNotificationOptionPrioAppDefaultSelected = true;
                        break;
                    default:
                        Debug.WriteLine("No value matched for the current notification settings of this channel." + 
                            "Take AppDefault as default value.");
                        IsNotificationOptionPrioAppDefaultSelected = true;
                        break;
                }
            }
        }

        /// <summary>
        /// Führt das Speichern der Benachrichtigungseinstellungen für den Kanal aus.
        /// </summary>
        private void executeSaveNotificationSettingsCommand()
        {
            displayIndeterminateProgressIndicator("Saving");

            NotificationSetting selectedOption = NotificationSetting.APPLICATION_DEFAULT;
            // Prüfe, welche Option gewählt wurde.
            if (IsNotificationOptionPrioAppDefaultSelected)
            {
                selectedOption = NotificationSetting.APPLICATION_DEFAULT;
            }
            else if (IsNotificationOptionPrioHighSelected)
            {
                selectedOption = NotificationSetting.ANNOUNCE_PRIORITY_HIGH;
            }
            else if (IsNotificationOptionAllSelected)
            {
                selectedOption = NotificationSetting.ANNOUNCE_ALL;
            }
            else if (IsNotificationOptionNoneSelected)
            {
                selectedOption = NotificationSetting.ANNOUNCE_NONE;
            }

            try
            {
                channelController.UpdateNotificationSettingsForChannel(SelectedChannel.Id, selectedOption);
                hideIndeterminateProgressIndicator();
                displayStatusBarText("Data saved", 2.5f);
            }
            catch(ClientException ex)
            {
                Debug.WriteLine("Update of notification settings has failed. Error code is: {0}", ex.ErrorCode);
                displayError(ex.ErrorCode);
                hideIndeterminateProgressIndicator();
            }
        }
    }
}
