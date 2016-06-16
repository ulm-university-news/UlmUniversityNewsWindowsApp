using DataHandlingLayer.Common;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.Controller;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    public class ConversationDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Die Referenz auf eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private string enteredMessage;
        /// <summary>
        /// Die vom Nutzer eingegebene Nachricht.
        /// </summary>
        public string EnteredMessage
        {
            get { return enteredMessage; }
            set { this.setProperty(ref this.enteredMessage, value); }
        }
        
        private Conversation selectedConversation;
        /// <summary>
        /// Die gewählte Konversation, zu der die Details angezeigt werden sollen.
        /// </summary>
        public Conversation SelectedConversation
        {
            get { return selectedConversation; }
            set { this.setProperty(ref this.selectedConversation, value); }
        }
        
        private IncrementalLoadingCollection<IncrementalConversationMessagesLoader, ConversationMessage> conversationMessages;
        /// <summary>
        /// Collection mit den Nachrichten der Konversation. Die Nachrichten können abhängig vom Zustand
        /// des zugehörigen Listenelements dynamisch nachgeladen werden.
        /// </summary>
        public IncrementalLoadingCollection<IncrementalConversationMessagesLoader, ConversationMessage> ConversationMessages
        {
            get { return conversationMessages; }
            set { this.setProperty(ref this.conversationMessages, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand sendMessageCommand;
        /// <summary>
        /// Befehl zum Senden einer Konversationsnachricht.
        /// </summary>
        public AsyncRelayCommand SendMessageCommand
        {
            get { return sendMessageCommand; }
            set { sendMessageCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ConversationDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            groupController = new GroupController(this);

            // Erzeuge Befehle.
            SendMessageCommand = new AsyncRelayCommand(
                param => executeSendMessageCommandAsync(),
                param => canSendMessage()
                );
        }

        /// <summary>
        /// Lade die Konversation, die durch die angegebene Id repräsentiert ist.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation.</param>
        public async Task LoadSelectedConversationAsync(int conversationId)
        {
            try
            {
                SelectedConversation = await Task.Run(() => groupController.GetConversation(conversationId, false));

                if (SelectedConversation != null)
                {
                    // Aktiviere dynamisches Laden der Konversationsnachrichten.
                    // Es sollen mindestens immer alle noch nicht gelesenen Nachrichten geladen werden, immer aber mindestens 20.
                    int numberOfItems = SelectedConversation.AmountOfUnreadMessages;
                    if (numberOfItems < 20)
                    {
                        numberOfItems = 20;
                    }

                    ConversationMessages = new IncrementalLoadingCollection<IncrementalConversationMessagesLoader, ConversationMessage>(
                        SelectedConversation.Id,
                        numberOfItems);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadSelectedConversationAsync: Failed to load selected conversation.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        /// <summary>
        /// Markiere die Nachrichten dieser Konversation als gelesen.
        /// </summary>
        public void MarkConversationMessagesAsRead()
        {
            if (SelectedConversation == null)
                return;

            try
            {
                groupController.MarkConversationMessagesAsRead(SelectedConversation.Id);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("MarkConversationMessagesAsRead: Failed to mark conversation messages as read.");
                displayError(ex.ErrorCode);
            }
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsfunktion, welche die Prüfung der Ausführbarkeit der Befehle anstößt.
        /// </summary>
        private void checkCommandExecution()
        {
            SendMessageCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Prüft, ob der Befehl zum Senden einer Konversationsnachricht aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSendMessage()
        {
            if (SelectedConversation != null)
                return true;

            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Senden einer Konversationsnachricht aus.
        /// </summary>
        /// <returns></returns>
        private async Task executeSendMessageCommandAsync()
        {
            if (SelectedConversation == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();

                string messageContent = EnteredMessage;
                Priority priority = Priority.NORMAL;

                User localUser = groupController.GetLocalUser();

                // Wenn es eine Tutoriumsgruppe ist und der Nutzer Tutor ist, dann sende mit Priorität hoch.
                Group group = groupController.GetGroup(SelectedConversation.GroupId);
                if (group.Type == GroupType.TUTORIAL && group.GroupAdmin == localUser.Id)
                {
                    Debug.WriteLine("executeSendMessageCommandAsync: Set message priority to high.");
                    priority = Priority.HIGH;
                }

                await groupController.SendConversationMessageAsync(
                    group.Id, 
                    SelectedConversation.Id, 
                    messageContent, 
                    priority);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSendMessageCommandAsync: Failed to send message.");
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
