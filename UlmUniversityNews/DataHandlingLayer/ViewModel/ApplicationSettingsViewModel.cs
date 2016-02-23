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

        private bool isNotificationOptionPrioHighSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Nur Priorität hoch ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionPrioHighSelected
        {
            get { return isNotificationOptionPrioHighSelected; }
            set { isNotificationOptionPrioHighSelected = value; }
        }

        private bool isNotificationOptionAllSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Alle Nachrichten ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionAllSelected
        {
            get { return isNotificationOptionAllSelected; }
            set { isNotificationOptionAllSelected = value; }
        }

        private bool isNotificationOptionNoneSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Option 'Keine Nachricht ankündigen' gewählt ist.
        /// </summary>
        public bool IsNotificationOptionNoneSelected
        {
            get { return isNotificationOptionNoneSelected; }
            set { isNotificationOptionNoneSelected = value; }
        }
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
        public async Task LoadCurrentSettings()
        {
            User localUser = applicationSettingsController.GetCurrentLocalUser();
            LocalUsername = localUser.Name;

            Language favoredLanguage = await Task.Run(() => applicationSettingsController.GetFavoredLanguage());
            if(favoredLanguage == Language.ENGLISH)
            {
                IsEnglishLanguageSelected = true;
            }
            else if(favoredLanguage == Language.GERMAN)
            {
                IsGermanLanguageSelected = true;
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
                    // Benutzereinstellungen:
                    try
                    {
                        // Aktualisiere Nutzername, falls nötig, und speichere gewählte Sprache ab.
                        await applicationSettingsController.UpdateLocalUsernameAsync(LocalUsername);
                        // TODO Language

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
                    break;
                case 2:
                    break;
            }
        }
    }
}
