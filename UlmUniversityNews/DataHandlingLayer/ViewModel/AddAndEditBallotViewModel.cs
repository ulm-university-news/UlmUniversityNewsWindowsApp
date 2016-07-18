using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;

namespace DataHandlingLayer.ViewModel
{
    public class AddAndEditBallotViewModel : DialogBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private bool isAddDialog;
        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zum Hinzufügen einer neuen Abstimmung handelt.
        /// </summary>
        public bool IsAddDialog
        {
            get { return isAddDialog; }
            set { this.setProperty(ref this.isAddDialog, value); }
        }

        private bool isEditDialog;
        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zum Bearbeiten einer bestehenden Abstimmung handelt.
        /// </summary>
        public bool IsEditDialog
        {
            get { return isEditDialog; }
            set { this.setProperty(ref this.isEditDialog, value); }
        }

        #region InputRelatedProperties
        private string enteredTitle;
        /// <summary>
        /// Gibt den Titel der Abstimmung an, die vom Nutzer eingegeben wurde.
        /// </summary>
        public string EnteredTitle
        {
            get { return enteredTitle; }
            set { this.setProperty(ref this.enteredTitle, value); }
        }

        private string enteredDescription;
        /// <summary>
        /// Gibt die Beschreibung der Abstimmung an, die vom Nutzer eingegeben wurde.
        /// </summary>
        public string EnteredDescription
        {
            get { return enteredDescription; }
            set { this.setProperty(ref this.enteredDescription, value); }
        }

        private bool isMultipleChoiceSelected;
        /// <summary>
        /// Gibt an, ob die Abstimmung die Auswahl mehrere Abstimmungsoptionen pro Nutzer zulässt,
        /// oder nicht.
        /// </summary>
        public bool IsMultipleChoiceSelected
        {
            get { return isMultipleChoiceSelected; }
            set { this.setProperty(ref this.isMultipleChoiceSelected, value); }
        }

        private bool isPublicVotesSelected;
        /// <summary>
        /// Gibt an, ob die Namen der Nutzer angezeigt werden bei der Repräsentation der Ergebnisse 
        /// der Abstimmung.
        /// </summary>
        public bool IsPublicVotesSelected
        {
            get { return isPublicVotesSelected; }
            set { this.setProperty(ref this.isPublicVotesSelected, value); }
        }

        private string enteredOptionText;
        /// <summary>
        /// Der vom Nutzer eingegebene Text einer Abstimmungsoption.
        /// </summary>
        public string EnteredOptionText
        {
            get { return enteredOptionText; }
            set { this.setProperty(ref this.enteredOptionText, value); }
        }
        #endregion InputRelatedProperties

        private Group affectedGroup;
        /// <summary>
        /// Die Gruppe, für die eine neue Abstimmung angelegt werden soll,
        /// bzw. für die eine bestehende Abstimmung geändert werden soll.
        /// </summary>
        public Group AffectedGroup
        {
            get { return affectedGroup; }
            set { this.setProperty(ref this.affectedGroup, value); }
        }

        private Ballot editableBallot;
        /// <summary>
        /// Die Abstimmung, die im Änderungsdialog bearbeitet werden soll.
        /// </summary>
        public Ballot EditableBallot
        {
            get { return editableBallot; }
            set { this.setProperty(ref this.editableBallot, value); }
        }

        private ObservableCollection<Option> ballotOptionsCollection;
        /// <summary>
        /// Liste von Abstimmungsoptionen, die für diese Abstimmung angelegt wurden.
        /// </summary>
        public ObservableCollection<Option> BallotOptionsCollection
        {
            get { return ballotOptionsCollection; }
            set { this.setProperty(ref this.ballotOptionsCollection, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand createBallotCommand;
        /// <summary>
        /// Befehl zum Erstellen einer neuen Abstimmung.
        /// </summary>
        public AsyncRelayCommand CreateBallotCommand
        {
            get { return createBallotCommand; }
            set { createBallotCommand = value; }
        }

        private AsyncRelayCommand editBallotCommand;
        /// <summary>
        /// Befehl zum Speichern der Änderungen an einer bestehenden Abstimmung.
        /// </summary>
        public AsyncRelayCommand EditBallotCommand
        {
            get { return editBallotCommand; }
            set { editBallotCommand = value; }
        }

        private RelayCommand addBallotOption;
        /// <summary>
        /// Befehl zum Hinzufügen einer Abstimmungsoption zur Abstimmung.
        /// </summary>
        public RelayCommand AddBallotOptionCommand
        {
            get { return addBallotOption; }
            set { addBallotOption = value; }
        }

        private RelayCommand removeBallotOption;
        /// <summary>
        /// Befehl zum Entfernen einer Abstimmungsoption von der Abstimmung.
        /// </summary>
        public RelayCommand RemoveBallotOptionCommand
        {
            get { return removeBallotOption; }
            set { removeBallotOption = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAndEditBallotViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditBallotViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            groupController = new GroupController(this);

            // Befehle
            CreateBallotCommand = new AsyncRelayCommand(
                param => executeCreateBallotCommandAsync(),
                param => canCreateBallot());
            EditBallotCommand = new AsyncRelayCommand(
                param => executeEditBallotAsync(),
                param => canEditBallot());
            AddBallotOptionCommand = new RelayCommand(
                param => executeAddOptionTextCommand(),
                param => canAddOptionText());
            RemoveBallotOptionCommand = new RelayCommand(
                param => executeRemoveOptionTextCommand(param),
                param => canRemoveOptionText());
        }

        /// <summary>
        /// Lädt Dialog zum Erstellen einer neuen Abstimmung.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die eine neue Abstimmung erstellt werden soll.</param>
        public async Task LoadCreateBallotDialogAsync(int groupId)
        {
            IsAddDialog = true;
            IsEditDialog = false;

            try
            {
                AffectedGroup = await Task.Run(() => groupController.GetGroup(groupId));
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadCreateBallotDialogAsync: Failed to load create ballot dialog. " + 
                    "Error code is: {0} and msg is: {1}.", ex.ErrorCode, ex.Message);
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        /// <summary>
        /// Lädt Dialog, um gewählte Abstimmung bearbeiten zu können.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die geändert werden soll.</param>
        public async Task LoadEditBallotDialogAsync(int groupId, int ballotId)
        {
            IsAddDialog = false;
            IsEditDialog = true;

            try
            {
                AffectedGroup = await Task.Run(() => groupController.GetGroup(groupId));
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadEditBallotDialogAsync: Failed to load edit ballot dialog. " +
                    "Error code is: {0} and msg is: {1}.", ex.ErrorCode, ex.Message);
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        #region CommandFunctionality
        /// <summary>
        /// Stößt die Überprüfung der Ausführbarkeit der Befehle an.
        /// </summary>
        private void checkCommandExecution()
        {
            CreateBallotCommand.OnCanExecuteChanged();
            EditBallotCommand.OnCanExecuteChanged();
            AddBallotOptionCommand.RaiseCanExecuteChanged();
            RemoveBallotOptionCommand.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Prüft, ob der Befehl zur Erstellung einer Abstimmung aktuell
        /// zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canCreateBallot()
        {
            if (AffectedGroup != null &&
                !AffectedGroup.Deleted &&
                IsAddDialog && 
                BallotOptionsCollection != null && BallotOptionsCollection.Count >= 2)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Anlegen einer neuen Abstimmung aus.
        /// </summary>
        private async Task executeCreateBallotCommandAsync()
        {
            // TODO
        }

        /// <summary>
        /// Prüft, ob der Befehl zum Speichern der Änderungen an einer bestehenden Abstimmung
        /// aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canEditBallot()
        {
            if (AffectedGroup != null && !AffectedGroup.Deleted &&
                IsEditDialog && 
                EditableBallot != null && 
                EditableBallot.IsClosed.HasValue && EditableBallot.IsClosed.Value == false &&
                BallotOptionsCollection != null && BallotOptionsCollection.Count >= 2)
            {
                return true;
            }

            return false;
        }

        private async Task executeEditBallotAsync()
        {
            // TODO
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Hinzufügen von Abstimmungsoptionen
        /// zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canAddOptionText()
        {
            if (AffectedGroup != null && !AffectedGroup.Deleted)
            {
                // Special case: Edit closed ballot.
                if (IsEditDialog && (EditableBallot == null || EditableBallot.IsClosed.Value == true))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Hinzufügen einer Abstimmungsoption zur Abstimmung aus.
        /// </summary>
        private void executeAddOptionTextCommand()
        {
            // TODO
        }

        /// <summary>
        /// Prüft, ob der Befehl zum Entfernen von Abstimmungsoptionen von der Abstimmung
        /// zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canRemoveOptionText()
        {
            if (AffectedGroup != null && !AffectedGroup.Deleted)
            {
                // Special case: Edit closed ballot.
                if (IsEditDialog && (EditableBallot == null || EditableBallot.IsClosed.Value == true))
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Entfernen einer Abstimmungsoption von der Abstimmung aus.
        /// </summary>
        /// <param name="selectedOption"></param>
        private void executeRemoveOptionTextCommand(object selectedOption)
        {
            // TODO
        }
        #endregion CommandFunctionality

    }
}
