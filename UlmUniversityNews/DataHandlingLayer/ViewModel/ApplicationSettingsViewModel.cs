using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Controller;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    public class ApplicationSettingsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ApplicationSettingsController.
        /// </summary>
        private ApplicationSettingsController applicationSettingsController;
        #endregion Fields

        #region Properties
        private int selectedPivotElementIndex;
        /// <summary>
        /// Gibt den Index des Pivot-Elements an, das gerade aktiv ist.
        /// PivotIndex 0 ist: Nutzereinstellungen
        /// PivotIndex 1 ist: Benachrichtigungseinstellungen
        /// PivotIndex 2 ist: Listeneinstellungen 
        /// </summary>
        public int SelectedPivotItemIndex
        {
            get { return selectedPivotElementIndex; }
            set { selectedPivotElementIndex = value; }
        }

        private bool isGermanLanguageSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Sprache Deutsch gewählt ist.
        /// </summary>
        public bool IsGermanLanguageSelected
        {
            get { return isGermanLanguageSelected; }
            set { this.setProperty(ref this.isGermanLanguageSelected, value); }
        }

        private bool isEnglishLanugageSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Sprache Englisch gewählt ist.
        /// </summary>
        public bool IsEnglishLanguageSelected
        {
            get { return isEnglishLanugageSelected; }
            set { this.setProperty(ref this.isEnglishLanugageSelected, value); }
        }

        private string localUsername;
        /// <summary>
        /// Der Nutzername des lokalen Nutzers.
        /// </summary>
        public string LocalUsername
        {
            get { return localUsername; }
            set { this.setProperty(ref this.localUsername, value); }
        }

        #region NotificationSettings
        private bool isNotificationOptionPrioHighSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Nur Priorität hoch ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioHighSelected
        {
            get { return isNotificationOptionPrioHighSelected; }
            set { this.setProperty(ref this.isNotificationOptionPrioHighSelected, value); }
        }

        private bool isNotificationOptionAllSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Alle Nachrichten ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionAllSelected
        {
            get { return isNotificationOptionAllSelected; }
            set { this.setProperty(ref this.isNotificationOptionAllSelected, value); }
        }

        private bool isNotificationOptionNoneSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Keine Nachricht ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionNoneSelected
        {
            get { return isNotificationOptionNoneSelected; }
            set { this.setProperty(ref this.isNotificationOptionNoneSelected, value); }
        }
        #endregion NotificationSettings

        #region GeneralListSettings
        private bool isAscendingSortingOptionSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option "Aufsteigend" für die Anordnung der Elemente in Auflistungen
        /// definiert ist.
        /// </summary>
        public bool IsAscendingSortingOptionSelected
        {
            get { return isAscendingSortingOptionSelected; }
            set { this.setProperty(ref this.isAscendingSortingOptionSelected, value); }
        }

        private bool isDescendingSortingOptionSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option "Absteigend" für die Anordnung der Elemente in Auflistungen
        /// definiert ist.
        /// </summary>
        public bool IsDescendingSortingOptionSelected
        {
            get { return isDescendingSortingOptionSelected; }
            set { this.setProperty(ref this.isDescendingSortingOptionSelected, value); }
        }
        #endregion GeneralListSettings

        #region AnnouncementOrderSettings
        private bool isAscendingMsgOrderSelected;
        /// <summary>
        /// Gibt an, ob die Nachrichten (Announcements) in Nachrichtenauflistungen von oben nach
        /// unten aufgelistet werden sollen.
        /// </summary>
        public bool IsAscendingMsgOrderSelected
        {
            get { return isAscendingMsgOrderSelected; }
            set { this.setProperty(ref this.isAscendingMsgOrderSelected, value); }
        }

        private bool isDescendingMsgOrderSelected;
        /// <summary>
        /// Gibt an, ob die Nachrichten (Announcements) in Nachrichtenauflistungen von unten nach
        /// oben aufgelistet werden sollen.
        /// </summary>
        public bool IsDescendingMsgOrderSelected
        {
            get { return isDescendingMsgOrderSelected; }
            set { this.setProperty(ref this.isDescendingMsgOrderSelected, value); }
        }      
        #endregion AnnouncementOrderSettings

        #region ChannelSettings
        private bool isChannelOrderOptionAlphabetical;
        /// <summary>
        /// Gibt an, ob Anordnungen von Kanälen in "Meine Kanäle" alphabetisch sortiert werden sollen.
        /// </summary>
        public bool IsChannelOrderOptionAlphabetical
        {
            get { return isChannelOrderOptionAlphabetical; }
            set { this.setProperty(ref this.isChannelOrderOptionAlphabetical, value); }
        }

        private bool isChannelOrderOptionByType;
        /// <summary>
        /// Gibt an, ob Anordnungen von Kanälen in "Meine Kanäle" nach Kanaltyp sortiert werden sollen.
        /// </summary>
        public bool IsChannelOrderOptionByType
        {
            get { return isChannelOrderOptionByType; }
            set { this.setProperty(ref this.isChannelOrderOptionByType, value); }
        }

        private bool isChannelOrderOptionByMsgAmount;
        /// <summary>
        /// Gibt an, ob Anordnungen von Kanälen in "Meine Kanäle" nach Anzahl ungelesener Nachrichten 
        /// sortiert werden sollen.
        /// </summary>
        public bool IsChannelOrderOptionByMsgAmount
        {
            get { return isChannelOrderOptionByMsgAmount; }
            set { this.setProperty(ref this.isChannelOrderOptionByMsgAmount, value); }
        }
        #endregion ChannelSettings

        #region GroupSettings
        private bool isGroupOrderOptionAlphabetical;
        /// <summary>
        /// Gibt an, ob Anordnungen von Gruppen in "Meine Gruppen" alphabetisch sortiert werden sollen.
        /// </summary>
        public bool IsGroupOrderOptionAlphabetical
        {
            get { return isGroupOrderOptionAlphabetical; }
            set { this.setProperty(ref this.isGroupOrderOptionAlphabetical, value); }
        }

        private bool isGroupOrderOptionByType;
        /// <summary>
        /// Gibt an, ob Anordnungen von Gruppen in "Meine Gruppen" nach Gruppentyp sortiert werden sollen.
        /// </summary>
        public bool IsGroupOrderOptionByType
        {
            get { return isGroupOrderOptionByType; }
            set { this.setProperty(ref this.isGroupOrderOptionByType, value); }
        }

        private bool isGroupOrderOptionByMsgAmount;
        /// <summary>
        /// Gibt an, ob Anordnungen von Gruppen in "Meine Gruppen" nach Anzahl ungelesener Nachrichten 
        /// sortiert werden sollen.
        /// </summary>
        public bool IsGroupOrderOptionByMsgAmount
        {
            get { return isGroupOrderOptionByMsgAmount; }
            set { this.setProperty(ref this.isGroupOrderOptionByMsgAmount, value); }
        }
        #endregion GroupSettings

        #region ConversationSettings
        private bool isConversationOrderOptionAlphabetical;
        /// <summary>
        /// Gibt an, ob Anordnungen von Konversationen in "Konversationen" Ansicht
        /// einer Gruppe alphabetisch sortiert werden sollen.
        /// </summary>
        public bool IsConversationOrderOptionAlphabetical
        {
            get { return isConversationOrderOptionAlphabetical; }
            set { this.setProperty(ref this.isConversationOrderOptionAlphabetical, value); }
        }

        private bool isConversationOrderOptionLatest;
        /// <summary>
        /// Gibt an, ob Anordnungen von Konversationen in "Konversationen" Ansicht
        /// einer Gruppe nach Erstellungsdatum sortiert werden sollen.
        /// </summary>
        public bool IsConversationOrderOptionLatest
        {
            get { return isConversationOrderOptionLatest; }
            set { this.setProperty(ref this.isConversationOrderOptionLatest, value); }
        }

        private bool isConversationOrderOptionByMsgAmount;
        /// <summary>
        /// Gibt an, ob Anordnungen von Konversationen in "Konversationen" Ansicht einer Gruppe
        /// nach Anzahl ungelesener Nachrichten sortiert werden sollen.
        /// </summary>
        public bool IsConversationOrderOptionByMsgAmount
        {
            get { return isConversationOrderOptionByMsgAmount; }
            set { this.setProperty(ref this.isConversationOrderOptionByMsgAmount, value); }
        }
        #endregion ConversationSettings

        #region BallotSettings
        private bool isBallotOrderOptionAlphabetical;
        /// <summary>
        /// Gibt an, ob Anordnungen von Abstimmungen in "Abstimmungen" Ansicht
        /// einer Gruppe alphabetisch sortiert werden sollen.
        /// </summary>
        public bool IsBallotOrderOptionAlphabetical
        {
            get { return isBallotOrderOptionAlphabetical; }
            set { this.setProperty(ref this.isBallotOrderOptionAlphabetical, value); }
        }

        private bool isBallotOrderOptionLatestVote;
        /// <summary>
        /// Gibt an, ob Anordnungen von Abstimmungen in "Abstimmungen" Ansicht
        /// einer Gruppe nach Datum der letzten Vote sortiert werden sollen.
        /// </summary>
        public bool IsBallotOrderOptionLatestVote
        {
            get { return isBallotOrderOptionLatestVote; }
            set { this.setProperty(ref this.isBallotOrderOptionLatestVote, value); }
        }

        private bool isBallotOrderOptionByBallotType;
        /// <summary>
        /// Gibt an, ob Anordnungen von Abstimmungen in "Abstimmungen" Ansicht einer Gruppe
        /// nach dem Typ der Umfrage sortiert werden sollen.
        /// </summary>
        public bool IsBallotOrderOptionByBallotType
        {
            get { return isBallotOrderOptionByBallotType; }
            set { this.setProperty(ref this.isBallotOrderOptionByBallotType, value); }
        }
        #endregion BallotSettings

        #endregion Properties

        #region Commands
        private AsyncRelayCommand saveSettingsCommand;
        /// <summary>
        /// Der Befehl zur Speicherung der Einstellungen.
        /// </summary>
        public AsyncRelayCommand SaveSettingsCommand
        {
            get { return saveSettingsCommand; }
            set { saveSettingsCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ApplicationSettingsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            applicationSettingsController = new ApplicationSettingsController(this);

            SaveSettingsCommand = new AsyncRelayCommand(param => executeSaveSettingsCommand());
        }

        /// <summary>
        /// Lädt die aktuell gültigen Einstellungen und passt den Zustand
        /// der ViewModel Instanz entsprechend an. 
        /// </summary>
        /// <returns></returns>
        public void LoadCurrentSettings()
        {
            User localUser = applicationSettingsController.GetCurrentLocalUser();
            LocalUsername = localUser.Name;

            AppSettings appSettings = applicationSettingsController.GetApplicationSettings();
            if(appSettings != null)
            {
                // Listenbezogene Parameter.
                initializeListSettingsViewParameter(appSettings);
                
                // Benachrichtigungseinstellungen.
                NotificationSetting notificationSetting = appSettings.MsgNotificationSetting;
                Debug.WriteLine("Loaded notification setting: " + notificationSetting);
                switch (notificationSetting)
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
                    default:
                        Debug.WriteLine("No valid notification setting found in AppSettings.");
                        break;
                }

                // Bevorzugte Sprache.
                Language favoredLanguage = appSettings.LanguageSetting;
                Debug.WriteLine("Loaded favored language: " + favoredLanguage);
                if (favoredLanguage == Language.ENGLISH)
                {
                    IsEnglishLanguageSelected = true;
                }
                else if (favoredLanguage == Language.GERMAN)
                {
                    IsGermanLanguageSelected = true;
                }
            }
            else
            {
                Debug.WriteLine("Error. Can't load the ApplicationSettings. LoadCurrentSettings has failed.");
            }

            //Language favoredLanguage = await Task.Run(() => applicationSettingsController.GetFavoredLanguage());
        }

        /// <summary>
        /// Initialisiert die Boolean-Variablen, die angeben, welche Einstellungen aktuell
        /// bezüglich der Listenanordnungen gewählt sind.
        /// </summary>
        /// <param name="appSettings">Die aktuellen Anwendungseinstellungen.</param>
        private void initializeListSettingsViewParameter(AppSettings appSettings)
        {
            Debug.WriteLine("Loaded settings for lists: {0}, {1}, {2}, {3}, {4}, {5}.",
               appSettings.GeneralListOrderSetting,
               appSettings.AnnouncementOrderSetting,
               appSettings.ChannelOderSetting,
               appSettings.GroupOrderSetting,
               appSettings.ConversationOrderSetting,
               appSettings.BallotOrderSetting);

            // Allgemeine Listeneinstellungen.
            OrderOption generalListOrder = appSettings.GeneralListOrderSetting;
            IsDescendingSortingOptionSelected = false;
            IsAscendingSortingOptionSelected = false;

            // Initialisierung der Boolean Parameter für allgemeine Listeneinstellungen.
            switch (generalListOrder)
            {
                case OrderOption.DESCENDING:
                    IsDescendingSortingOptionSelected = true;
                    break;
                case OrderOption.ASCENDING:
                    IsAscendingSortingOptionSelected = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the generalListOrder settings. Take ascending as default.");
                    IsAscendingSortingOptionSelected = true;
                    break;
            }

            // Anordnung von Nachrichten (Announcements) in Nachrichtenauflistungen.
            OrderOption announcementOrderSetting = appSettings.AnnouncementOrderSetting;
            IsAscendingMsgOrderSelected = false;
            IsDescendingMsgOrderSelected = false;

            switch (announcementOrderSetting)
            {
                case OrderOption.ASCENDING:
                    IsAscendingMsgOrderSelected = true;
                    break;
                case OrderOption.DESCENDING:
                    IsDescendingMsgOrderSelected = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the announcementOrderSetting. Take ascending as default.");
                    IsAscendingMsgOrderSelected = true;
                    break;
            }

            // Kanaleinstellungen.
            OrderOption channelSettings = appSettings.ChannelOderSetting;

            switch (channelSettings)
            {
                case OrderOption.ALPHABETICAL:
                    IsChannelOrderOptionAlphabetical = true;
                    IsChannelOrderOptionByType = false;
                    IsChannelOrderOptionByMsgAmount = false;
                    break;
                case OrderOption.BY_TYPE:
                    IsChannelOrderOptionAlphabetical = false;
                    IsChannelOrderOptionByType = true;
                    IsChannelOrderOptionByMsgAmount = false;
                    break;
                case OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    
                    IsChannelOrderOptionAlphabetical = false;
                    IsChannelOrderOptionByType = false;
                    IsChannelOrderOptionByMsgAmount = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the channelListOrder settings. Take alphabetical as default.");
                    IsChannelOrderOptionAlphabetical = true;
                    IsChannelOrderOptionByType = false;
                    IsChannelOrderOptionByMsgAmount = false;
                    break;
            }

            // Gruppeneinstellungen.
            OrderOption groupSettings = appSettings.GroupOrderSetting;
            IsGroupOrderOptionAlphabetical = false;
            IsGroupOrderOptionByType = false;
            IsGroupOrderOptionByMsgAmount = false;

            switch (groupSettings)
            {
                case OrderOption.ALPHABETICAL:
                    IsGroupOrderOptionAlphabetical = true;
                    break;
                case OrderOption.BY_TYPE:
                    IsGroupOrderOptionByType = true;
                    break;
                case OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    IsGroupOrderOptionByMsgAmount = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the groupListOrder settings. Take alphabetical as default.");
                    IsGroupOrderOptionAlphabetical = true;
                    break;
            }

            // Konversationeneinstellungen.
            OrderOption conversationSettings = appSettings.ConversationOrderSetting;
            IsConversationOrderOptionAlphabetical = false;
            IsConversationOrderOptionByMsgAmount = false;
            IsConversationOrderOptionLatest = false;

            switch (conversationSettings)
            {
                case OrderOption.ALPHABETICAL:
                    IsConversationOrderOptionAlphabetical = true;
                    break;
                case OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    IsConversationOrderOptionByMsgAmount = true;
                    break;
                case OrderOption.BY_LATEST_DATE:
                    IsConversationOrderOptionLatest = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the conversationListOrder settings. Take alphabetical as default.");
                    IsConversationOrderOptionAlphabetical = true;
                    break;
            }

            // Abstimmungseinstellungen.
            OrderOption ballotSettings = appSettings.BallotOrderSetting;
            IsBallotOrderOptionAlphabetical = false;
            IsBallotOrderOptionByBallotType = false;
            IsBallotOrderOptionLatestVote = false;

            switch (ballotSettings)
            {
                case OrderOption.ALPHABETICAL:
                    IsBallotOrderOptionAlphabetical = true;
                    break;
                case OrderOption.BY_TYPE:
                    IsBallotOrderOptionByBallotType = true;
                    break;
                case OrderOption.BY_LATEST_VOTE:
                    IsBallotOrderOptionLatestVote = true;
                    break;
                default:
                    Debug.WriteLine("No value matched for the ballotListOrder settings. Take alphabetical as default.");
                    IsBallotOrderOptionAlphabetical = true;
                    break;
            }
        }

        /// <summary>
        /// Führt die Speicherung der vorgenommenen Einstellungen durch.
        /// </summary>
        private async Task executeSaveSettingsCommand()
        {
            switch (SelectedPivotItemIndex)
            {
                case 0:
                    // Benutzereinstellungen.
                    try
                    {
                        Debug.WriteLine("Store user information settings.");
                        // Aktualisiere Nutzername, falls nötig, und speichere gewählte Sprache ab.
                        await applicationSettingsController.UpdateLocalUsernameAsync(LocalUsername);
                       
                        AppSettings appSettings = applicationSettingsController.GetApplicationSettings();
                        // Einstellung bezüglich Sprache.
                        if(IsEnglishLanguageSelected && appSettings.LanguageSetting == Language.GERMAN)
                        {
                            // Ändere Sprache auf Englisch.
                            applicationSettingsController.UpdateFavoredLanguageSettings(Language.ENGLISH);
                        }
                        else if(IsGermanLanguageSelected && appSettings.LanguageSetting == Language.ENGLISH)
                        {
                            // Ändere Sprache auf Deutsch.
                            applicationSettingsController.UpdateFavoredLanguageSettings(Language.GERMAN);
                        }

                        // Debug.WriteLine("Current PrimaryLangugae Override is: " + Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride);
                    }
                    catch(ClientException ex)
                    {
                        Debug.WriteLine("Error occurred during the saving process of the user information." 
                         + "Error code is {0}.", ex.ErrorCode);
                        displayError(ex.ErrorCode);
                    }
                    break;
                case 1:
                    // Benachrichtigungseinstellungen.
                    try
                    {
                        Debug.WriteLine("Store notification settings.");
                        if(IsNotificationOptionPrioHighSelected){
                            applicationSettingsController.UpdateNotificationSettings(NotificationSetting.ANNOUNCE_PRIORITY_HIGH);
                        }
                        else if(IsNotificationOptionAllSelected)
                        {
                            applicationSettingsController.UpdateNotificationSettings(NotificationSetting.ANNOUNCE_ALL);
                        }
                        else if(IsNotificationOptionNoneSelected)
                        {
                            applicationSettingsController.UpdateNotificationSettings(NotificationSetting.ANNOUNCE_NONE);
                        }
                    }
                    catch(ClientException ex)
                    {
                        Debug.WriteLine("Error occurred during the saving process of the notification information."
                            + "Error code is {0}.", ex.ErrorCode);
                        displayError(ex.ErrorCode);
                    }
                    break;
                case 2:
                    // Listeneinstellungen.
                    try
                    {
                        Debug.WriteLine("Store list settings.");

                        OrderOption newGeneralListOrderSetting = OrderOption.ASCENDING;
                        if (IsDescendingSortingOptionSelected)
                        {
                            newGeneralListOrderSetting = OrderOption.DESCENDING;
                        }

                        OrderOption newAnnouncementOrderSetting = OrderOption.ASCENDING;
                        if(IsDescendingMsgOrderSelected)
                        {
                            newAnnouncementOrderSetting = OrderOption.DESCENDING;
                        }

                        OrderOption newChannelOrderSetting = OrderOption.ALPHABETICAL;
                        if(IsChannelOrderOptionByType)
                        {
                            newChannelOrderSetting = OrderOption.BY_TYPE;
                        }
                        else if(IsChannelOrderOptionByMsgAmount)
                        {
                            newChannelOrderSetting = OrderOption.BY_NEW_MESSAGES_AMOUNT;
                        }

                        OrderOption newGroupOrderSetting = OrderOption.ALPHABETICAL;
                        if(IsGroupOrderOptionByType)
                        {
                            newGroupOrderSetting = OrderOption.BY_TYPE;
                        }
                        else if(IsGroupOrderOptionByMsgAmount)
                        {
                            newGroupOrderSetting = OrderOption.BY_NEW_MESSAGES_AMOUNT;
                        }

                        OrderOption newConversationOrderSetting = OrderOption.ALPHABETICAL;
                        if(IsConversationOrderOptionByMsgAmount)
                        {
                            newConversationOrderSetting = OrderOption.BY_NEW_MESSAGES_AMOUNT;
                        }
                        else if(IsConversationOrderOptionLatest)
                        {
                            newConversationOrderSetting = OrderOption.BY_LATEST_DATE;
                        }

                        OrderOption newBallotOrderSetting = OrderOption.ALPHABETICAL;
                        if(IsBallotOrderOptionByBallotType)
                        {
                            newBallotOrderSetting = OrderOption.BY_TYPE;
                        }
                        else if(IsBallotOrderOptionLatestVote)
                        {
                            newBallotOrderSetting = OrderOption.BY_LATEST_VOTE;
                        }

                        // Aktualisiere Settings.
                        applicationSettingsController.UpdateListSettings(newGeneralListOrderSetting, newAnnouncementOrderSetting,  newChannelOrderSetting, 
                            newGroupOrderSetting, newConversationOrderSetting, newBallotOrderSetting);
                    }
                    catch(ClientException ex)
                    {
                        Debug.WriteLine("Error occurred during the saving process of the list order information."
                            + "Error code is {0}.", ex.ErrorCode);
                        displayError(ex.ErrorCode);
                    }
                    break;
            }
        }
    }
}
