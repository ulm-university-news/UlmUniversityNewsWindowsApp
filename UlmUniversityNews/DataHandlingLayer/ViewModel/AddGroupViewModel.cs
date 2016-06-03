using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.Controller;

namespace DataHandlingLayer.ViewModel
{
    public class AddGroupViewModel : DialogBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController; 
        #endregion Fields

        #region Properties
        private string groupName;
        /// <summary>
        /// Der Name der Gruppe.
        /// </summary>
        public string GroupName
        {
            get { return groupName; }
            set { this.setProperty(ref this.groupName, value); }
        }

        private GroupType selectedGroupType;
        /// <summary>
        /// Der gewählte Typ der Gruppe.
        /// </summary>
        public GroupType SelectedGroupType
        {
            get { return selectedGroupType; }
            set { this.setProperty(ref this.selectedGroupType, value); }
        }

        #region Term
        private bool isNoTermSelected;
        /// <summary>
        /// Gibt an, ob der Eintrag "Keine Angabe" gewählt ist.
        /// </summary>
        public bool IsNoTermSelected
        {
            get { return isNoTermSelected; }
            set { this.setProperty(ref this.isNoTermSelected, value); }
        }
        
        private bool isSummerTermSelected;
        /// <summary>
        /// Gibt an, ob der Eintrag "Sommersemester" aktuell gewählt ist.
        /// </summary>
        public bool IsSummerTermSelected
        {
            get { return isSummerTermSelected; }
            set { this.setProperty(ref this.isSummerTermSelected, value); }
        }

        private bool isWinterTermSelected;
        /// <summary>
        ///  Gibt an, ob der Eintrag "Wintersemester" aktuell gewählt ist.
        /// </summary>
        public bool IsWinterTermSelected
        {
            get { return isWinterTermSelected; }
            set { this.setProperty(ref this.isWinterTermSelected, value); }
        }

        private string termYear;
        /// <summary>
        /// Gibt das gewählte Jahr für das Semester an.
        /// </summary>
        public string TermYear
        {
            get { return termYear; }
            set { this.setProperty(ref this.termYear, value); }
        }
        #endregion Term

        private string groupDescription;
        /// <summary>
        /// Die Beschreibung bezüglich der Gruppe.
        /// </summary>
        public string GroupDescription
        {
            get { return groupDescription; }
            set { this.setProperty(ref this.groupDescription, value); }
        }

        private string groupPassword;
        /// <summary>
        /// Das für die Gruppe gesetzte Passwort.
        /// </summary>
        public string GroupPassword
        {
            get { return groupPassword; }
            set { this.setProperty(ref this.groupPassword, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand addGroupCommand;
        /// <summary>
        /// Befehl zum Anlegen einer neuen Gruppe.
        /// </summary>
        public AsyncRelayCommand AddGroupCommand
        {
            get { return addGroupCommand; }
            set { addGroupCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddGroupViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddGroupViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            // Erzeuge Referenz auf GroupController mit Validierungsfehler-Meldung.
            groupController = new GroupController(this);

            // Erzeuge Befehle.
            AddGroupCommand = new AsyncRelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
        }

        #region CommandFunctionality
        /// <summary>
        /// Gibt an, ob anhand des aktuellen Zustands der Befehl zum
        /// Anlegen einer neuen Gruppe zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canAddGroup()
        {
            return true;
        }

        /// <summary>
        /// Führt den Befehl AddGroupCommand aus. Stößt das Anlegen einer neuen Gruppe an.
        /// </summary>
        private async Task executeAddGroupCommand()
        {
            string term = string.Empty;
            if (IsNoTermSelected)
            {
                // Semester ignorieren.
                term = null;
            }
            else if (IsWinterTermSelected)
            {
                term = "W" + TermYear;
            }
            else if (IsSummerTermSelected)
            {
                term = "S" + TermYear;
            }

            // Erzeuge neue Gruppeninstanz aus eingegebenen Daten.
            Group newGroup = new Group()
            {
                Name = GroupName,
                Description = GroupDescription,
                Term = term,
                Type = SelectedGroupType,
                Password = GroupPassword
            };

            try
            {
                displayIndeterminateProgressIndicator();
                bool successful = await groupController.CreateGroupAsync(newGroup);

                if (successful)
                {
                    // Navigiere zurück auf den Homescreen der Moderatorenansicht.
                    if (_navService.CanGoBack())
                    {
                        _navService.GoBack();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during creation process of group. Message is: {0}.", ex.Message);
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
