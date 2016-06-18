using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    public class GroupSettingsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private Group selectedGroup;
        /// <summary>
        /// Die Instanz der gewählten Gruppe, zu der die Einstellungen angezeigt werden.
        /// </summary>
        public Group SelectedGroup
        {
            get { return selectedGroup; }
            set { this.setProperty(ref this.selectedGroup, value); }
        }

        private bool isNotificationOptionPrioAppDefaultSelected;
        /// <summary>
        /// Gibt an, ob die Option "Application Default" angewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioAppDefaultSelected
        {
            get { return isNotificationOptionPrioAppDefaultSelected; }
            set { this.setProperty(ref this.isNotificationOptionPrioAppDefaultSelected, value); }
        }

        private bool isNotificationOptionPrioHighSelected;
        /// <summary>
        /// Gibt an, ob die Option "Nur Priorität hoch" angewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioHighSelected
        {
            get { return isNotificationOptionPrioHighSelected; }
            set { this.setProperty(ref this.isNotificationOptionPrioHighSelected, value); }
        }

        private bool isNotificationOptionAllSelected;
        /// <summary>
        /// Gibt an, ob die Option "Alle Nachrichten" angewählt ist.
        /// </summary>
        public bool IsNotificationOptionAllSelected
        {
            get { return isNotificationOptionAllSelected; }
            set { this.setProperty(ref this.isNotificationOptionAllSelected, value); }
        }

        private bool isNotificationOptionNoneSelected;
        /// <summary>
        /// Gibt an, ob die Option "Keine Nachrichten" angewählt ist.
        /// </summary>
        public bool IsNotificationOptionNoneSelected
        {
            get { return isNotificationOptionNoneSelected; }
            set { this.setProperty(ref this.isNotificationOptionNoneSelected, value); }
        }
        #endregion Properties

        #region Commands
        private RelayCommand saveNotificationSettingsCommand;
        /// <summary>
        /// Befehl zum Abspeichern der Gruppeneinstellungen.
        /// </summary>
        public RelayCommand SaveNotificationSettingsCommand
        {
            get { return saveNotificationSettingsCommand; }
            set { saveNotificationSettingsCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse GroupSettingsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public GroupSettingsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            groupController = new GroupController();

            // Erzeuge Befehle.
            SaveNotificationSettingsCommand = new RelayCommand(
                param => executeSaveGroupNotificationSettingsCommand(),
                param => canSaveGroupSettings());
        }

        /// <summary>
        /// Lädt die Gruppeneinstellungen für die Gruppe mit der angegebenen Id.
        /// </summary>
        /// <param name="groupId">Die Id der gewählten Gruppe.</param>
        public void LoadGroupSettings(int groupId)
        {
            try
            {
                SelectedGroup = groupController.GetGroup(groupId);

                if (SelectedGroup != null)
                {
                    IsNotificationOptionAllSelected = false;
                    IsNotificationOptionNoneSelected = false;
                    IsNotificationOptionPrioAppDefaultSelected = false;
                    IsNotificationOptionPrioHighSelected = false;
                    switch (SelectedGroup.GroupNotificationSetting)
                    {
                        case DataModel.Enums.NotificationSetting.ANNOUNCE_PRIORITY_HIGH:
                            IsNotificationOptionPrioHighSelected = true;
                            break;
                        case DataModel.Enums.NotificationSetting.ANNOUNCE_ALL:
                            IsNotificationOptionAllSelected = true;
                            break;
                        case DataModel.Enums.NotificationSetting.ANNOUNCE_NONE:
                            IsNotificationOptionNoneSelected = true;
                            break;
                        case DataModel.Enums.NotificationSetting.APPLICATION_DEFAULT:
                            IsNotificationOptionPrioAppDefaultSelected = true;
                            break;
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadGroupSettings: Failed to load group settings.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsfunktion, die die Prüfung der Ausführbarkeit von Befehlen anstößt.
        /// </summary>
        private void checkCommandExecution()
        {
            SaveNotificationSettingsCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Speichern der Gruppeneinstellungen zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSaveGroupSettings()
        {
            if (SelectedGroup != null && 
                !SelectedGroup.Deleted)
                return true;

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Speichern der Gruppeneinstellungen aus.
        /// </summary>
        private void executeSaveGroupNotificationSettingsCommand()
        {
            displayIndeterminateProgressIndicator("StatusBarInformationSaving");

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
                Debug.WriteLine("executeSaveGroupNotificationSettingsCommand: the selected option is {0}.", selectedOption);
                groupController.ChangeNotificationSettingsForGroup(SelectedGroup.Id, selectedOption);
                hideIndeterminateProgressIndicator();
                displayStatusBarText("StatusBarInformationDataSaved", 3.0f);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Update of notification settings has failed. Error code is: {0}", ex.ErrorCode);
                displayError(ex.ErrorCode);
                hideIndeterminateProgressIndicator();
            }
        }
        #endregion CommandFunctionality

    }
}
