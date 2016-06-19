using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;

namespace DataHandlingLayer.ViewModel
{
    public class AddAndEditConversationViewModel : DialogBaseViewModel
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
        /// Gibt an, ob es sich um einen Dialog zum Anlegen einer neuen Konversation handelt.
        /// </summary>
        public bool IsAddDialog
        {
            get { return isAddDialog; }
            set { this.setProperty(ref this.isAddDialog, value); }
        }

        private bool isEditDialog;
        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zum Ändern einer bestehenden Konversation handelt.
        /// </summary>
        public bool IsEditDialog
        {
            get { return isEditDialog; }
            set { this.setProperty(ref this.isEditDialog, value); }
        }

        private Group correspondingGroup;
        /// <summary>
        /// Die Gruppe, zu der die Konversation gehört/gehören soll.
        /// </summary>
        public Group CorrespondingGroup
        {
            get { return correspondingGroup; }
            set { this.setProperty(ref this.correspondingGroup, value); }
        }

        private Conversation editableConversation;
        /// <summary>
        /// Die Konversation, die in einem Änderungsdialog geändert werden kann.
        /// </summary>
        public Conversation EditableConversation
        {
            get { return editableConversation; }
            set { this.setProperty(ref this.editableConversation, value); }
        }

        private string enteredTitle;
        /// <summary>
        /// Der vom Nutzer eingegebene Titel für die Konversation.
        /// </summary>
        public string EnteredTitle
        {
            get { return enteredTitle; }
            set { this.setProperty(ref this.enteredTitle, value); }
        }

        private bool isClosedChecked;
        /// <summary>
        /// Gibt an, ob die Konversation als geschlossen markiert ist.
        /// </summary>
        public bool IsClosedChecked
        {
            get { return isClosedChecked; }
            set { this.setProperty(ref this.isClosedChecked, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand addConversationCommand;
        /// <summary>
        /// Befehl zum Anlegen einer neuen Konversation.
        /// </summary>
        public AsyncRelayCommand AddConversationCommand
        {
            get { return addConversationCommand; }
            set { addConversationCommand = value; }
        }

        private AsyncRelayCommand editConversationCommand;
        /// <summary>
        /// Befehl zum Ändern einer Konversation.
        /// </summary>
        public AsyncRelayCommand EditConversationCommand
        {
            get { return editConversationCommand; }
            set { editConversationCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAndEditConversationViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditConversationViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            groupController = new GroupController(this);

            // Erzeuge Befehle.
            AddConversationCommand = new AsyncRelayCommand(
                param => executeAddConversationCommandAsync(),
                param => canAddConversation());
            EditConversationCommand = new AsyncRelayCommand(
                param => executeEditConversationCommandAsync(),
                param => canEditConversation());
        }

        /// <summary>
        /// Lädt den Dialog zum Erstellen einer neuen Konversation.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehören soll.</param>
        public void LoadAddConversationDialog(int groupId)
        {
            IsAddDialog = true;
            IsEditDialog = false;

            try
            {
                CorrespondingGroup = groupController.GetGroup(groupId);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadAddConversationDialog: Failed to load the add conversation dialog.");
                displayError(ex.ErrorCode);
            }
            
            checkCommandExecution();
        }

        /// <summary>
        /// Lädt den Dialog zum Ändern der angegebenen Konversation.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversation gehört.</param>
        /// <param name="conversationId">Die Id der Konversation, die geändert werden soll.</param>
        public async Task LoadEditConversationDialogAsync(int groupId, int conversationId)
        {
            IsAddDialog = false;
            IsEditDialog = true;

            try
            {
                CorrespondingGroup = await Task.Run(() => groupController.GetGroup(groupId));
                EditableConversation = await Task.Run(() => groupController.GetConversation(conversationId, false));

                if (CorrespondingGroup != null && EditableConversation != null)
                {
                    EnteredTitle = EditableConversation.Title;

                    if (EditableConversation.IsClosed.HasValue && 
                        EditableConversation.IsClosed == true)
                    {
                        IsClosedChecked = true;
                    }
                    else
                    {
                        IsClosedChecked = false;
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadEditConversationDialogAsync: Failed to load edit dialog.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsmethode, welche die Prüfung der Ausführbarkeit von Befehlen anstößt.
        /// </summary>
        private void checkCommandExecution()
        {
            AddConversationCommand.OnCanExecuteChanged();
            EditConversationCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Anlegen einer Konversation aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canAddConversation()
        {
            if (IsAddDialog && 
                CorrespondingGroup != null && 
                !CorrespondingGroup.Deleted)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ausführung des Befehls zum Anlegen einer Konversation.
        /// </summary>
        private async Task executeAddConversationCommandAsync()
        {
            try
            {
                displayIndeterminateProgressIndicator("AddAndEditConversationAddConversationStatus");

                Conversation conversation = new Conversation()
                {
                    Title = EnteredTitle
                };

                bool successful = await groupController.CreateConversationAsync(CorrespondingGroup.Id, conversation);
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
                Debug.WriteLine("executeAddConversationCommandAsync: Failed to add conversation.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Ändern einer Konversation zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canEditConversation()
        {
            if (IsEditDialog && 
                CorrespondingGroup != null && 
                !CorrespondingGroup.Deleted && 
                EditableConversation != null)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Ausführen des Befehls zum Ändern einer bestehenden Konversation.
        /// </summary>
        private async Task executeEditConversationCommandAsync()
        {
            try
            {
                displayIndeterminateProgressIndicator("AddAndEditConversationEditConversationStatus");

                Conversation newConversation = new Conversation()
                {
                    Title = EnteredTitle,
                    IsClosed = IsClosedChecked
                };

                bool successful = await groupController.UpdateConversationAsync(
                    CorrespondingGroup.Id,
                    EditableConversation,
                    newConversation);

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
                Debug.WriteLine("executeEditConversationCommandAsync: Failed to edit the conversation.");
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
