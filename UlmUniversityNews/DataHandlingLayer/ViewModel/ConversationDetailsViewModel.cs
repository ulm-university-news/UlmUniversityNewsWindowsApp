using DataHandlingLayer.Common;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.Controller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;

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
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ConversationDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            groupController = new GroupController();
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
    }
}
