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
    public class AddAndEditGroupViewModel : DialogBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private bool isCreateDialog;
        /// <summary>
        /// Gibt an, ob es sich bei der aktuellen Dialog-Instanz um einen
        /// Erstellungsdialog für eine Gruppe handelt.
        /// </summary>
        public bool IsCreateDialog
        {
            get { return isCreateDialog; }
            set { this.setProperty(ref this.isCreateDialog, value); }
        }

        private bool isEditDialog;
        /// <summary>
        /// Gibt an, ob es sich bei der aktuellen Dialog-Instanz um einen
        /// Änderungsdialog für eine Gruppe handelt.
        /// </summary>
        public bool IsEditDialog
        {
            get { return isEditDialog; }
            set { this.setProperty(ref this.isEditDialog, value); }
        }
        
        private string groupName;
        /// <summary>
        /// Der Name der Gruppe.
        /// </summary>
        public string GroupName
        {
            get { return groupName; }
            set { this.setProperty(ref this.groupName, value); }
        }

        private bool isPasswordFieldActive;
        /// <summary>
        /// Gibt an, ob das Eingabefeld für die Passworteingabe aktuell 
        /// aktiviert ist oder nicht.
        /// </summary>
        public bool IsPasswordFieldActive
        {
            get { return isPasswordFieldActive; }
            set { this.setProperty(ref this.isPasswordFieldActive, value); }
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
        private int selectedComboBoxIndex;
        /// <summary>
        /// Der Index des ausgewählten ComboBox-Eintrags.
        /// 0 = "keine Angabe"
        /// 1 = "Sommersemster"
        /// 2 = "Wintersemster"
        /// </summary>
        public int SelectedComboBoxIndex
        {
            get { return selectedComboBoxIndex; }
            set { this.setProperty(ref this.selectedComboBoxIndex, value); }
        }
        
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

        private Group editableGroup;
        /// <summary>
        /// Die Gruppen-Instanz, deren Daten geändert werden sollen.
        /// </summary>
        public Group EditableGroup
        {
            get { return editableGroup; }
            set { this.setProperty(ref this.editableGroup, value); }
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

        private AsyncRelayCommand editGroupCommand;
        /// <summary>
        /// Befehl zum Ändern einer Gruppe.
        /// </summary>
        public AsyncRelayCommand EditGroupCommand
        {
            get { return editGroupCommand; }
            set { editGroupCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddGroupViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditGroupViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            // Erzeuge Referenz auf GroupController mit Validierungsfehler-Meldung.
            groupController = new GroupController(this);

            // Erzeuge Befehle.
            AddGroupCommand = new AsyncRelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
            EditGroupCommand = new AsyncRelayCommand(param => executeEditGroupAsync(), param => canEditGroup());
        }

        /// <summary>
        /// Initialisiert die ViewModel Instanz, so dass die Erstellung
        /// einer Gruppe ermöglicht wird.
        /// </summary>
        public void LoadCreateDialog()
        {
            IsCreateDialog = true;
            IsEditDialog = false;
            IsPasswordFieldActive = true;

            IsNoTermSelected = true;
            IsSummerTermSelected = false;
            IsWinterTermSelected = false;

            checkCommandFunctionality();
        }

        /// <summary>
        /// Initialisiert die ViewModel Instanz, so dass die Änderung
        /// der Gruppe mit der angegebenen Id ermöglicht wird.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die für den Änderungsdialog geladen werden soll.</param>
        public void LoadEditDialog(int groupId)
        {
            IsCreateDialog = false;
            IsEditDialog = true;
            IsPasswordFieldActive = false;

            // Lade Gruppe.
            EditableGroup = groupController.GetGroup(groupId);

            if (EditableGroup != null)
            {
                // Initialisiere Parameter.
                GroupName = EditableGroup.Name;
                GroupDescription = EditableGroup.Description;
                SelectedGroupType = EditableGroup.Type;

                if (EditableGroup.Term == null)
                {
                    SelectedComboBoxIndex = 0;
                    IsNoTermSelected = true;
                    IsSummerTermSelected = false;
                    IsWinterTermSelected = false;
                }
                else if (EditableGroup.Term.StartsWith("S"))
                {
                    SelectedComboBoxIndex = 1;
                    IsNoTermSelected = false;
                    IsSummerTermSelected = true;
                    IsWinterTermSelected = false;

                    string year = EditableGroup.Term.Substring(1, EditableGroup.Term.Length - 1);
                    TermYear = year;
                }
                else if (EditableGroup.Term.StartsWith("W"))
                {
                    SelectedComboBoxIndex = 2;
                    IsNoTermSelected = false;
                    IsSummerTermSelected = false;
                    IsWinterTermSelected = true;

                    string year = EditableGroup.Term.Substring(1, EditableGroup.Term.Length - 1);
                    TermYear = year;
                }
            }

            checkCommandFunctionality();
        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Group aus den Daten
        /// der Eingabefelder.
        /// </summary>
        /// <returns>Eine Instanz von Group.</returns>
        private Group createGroupFromEnteredData()
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

            // Setze Passwort null, falls es nicht eingegeben wurde.
            if (newGroup.Password != null && newGroup.Password.Trim().Length == 0)
                newGroup.Password = null;

            return newGroup;
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsmethode, welche die Prüfung der Verfügbarkeit von Befehlen anstößt.
        /// </summary>
        private void checkCommandFunctionality()
        {
            AddGroupCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob anhand des aktuellen Zustands der Befehl zum
        /// Anlegen einer neuen Gruppe zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canAddGroup()
        {
            if (IsCreateDialog)
                return true;
            return false;
        }

        /// <summary>
        /// Führt den Befehl AddGroupCommand aus. Stößt das Anlegen einer neuen Gruppe an.
        /// </summary>
        private async Task executeAddGroupCommand()
        {
            Group newGroup = createGroupFromEnteredData();

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

        /// <summary>
        /// Gibt an, ob anhand des aktuellen Zustands der Befehl zum
        /// Ändern einer neuen Gruppe zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canEditGroup()
        {
            if (IsEditDialog)
                return true;
            return false;
        }

        /// <summary>
        /// Führt den Befehl EditGroupCommand aus. Stößt die Aktualisierung der gewählten Gruppe an.
        /// </summary>
        private async Task executeEditGroupAsync()
        {
            if (EditableGroup == null)
                return;

            // Erstelle neue Gruppe aus eingegebenen Daten.
            Group newGroup = createGroupFromEnteredData();

            try
            {
                displayIndeterminateProgressIndicator();
                bool successful = false;

                // Prüfe, ob ein Passwort erwartet wird.
                if (IsPasswordFieldActive)
                {
                    successful = await groupController.UpdateGroupAsync(EditableGroup, newGroup, false);
                }
                else
                {
                    successful = await groupController.UpdateGroupAsync(EditableGroup, newGroup, true);
                }

                if (successful)
                {
                    if (_navService.CanGoBack())
                    {
                        _navService.GoBack();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeEditGroupAsync: Update of group failed.");
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
