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

        private Group correspondingGroup;
        /// <summary>
        /// Die Gruppeninstanz, zu der die Konversation gehört.
        /// </summary>
        public Group CorrespondingGroup
        {
            get { return correspondingGroup; }
            set { this.setProperty(ref this.correspondingGroup, value); }
        }

        private bool activeParticipant;
        /// <summary>
        /// Gibt an, ob der lokale Nutzer noch ein aktiver Teilnehmer der Gruppe ist.
        /// </summary>
        public bool IsActiveParticipant
        {
            get { return activeParticipant; }
            set { this.setProperty(ref this.activeParticipant, value); }
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

        private AsyncRelayCommand synchronizeMessagesCommand;
        /// <summary>
        /// Befehl zum Synchronisieren der Nachrichten mit dem Server.
        /// </summary>
        public AsyncRelayCommand SynchronizeMessagesCommand
        {
            get { return synchronizeMessagesCommand; }
            set { synchronizeMessagesCommand = value; }
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
            SynchronizeMessagesCommand = new AsyncRelayCommand(
                param => executeSynchronizeMessagesCommand(),
                param => canSynchronizeMessages());
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

                    // Lade noch die zugehörige Gruppeninstanz und prüfe den Active-Zustand des lokalen Nutzers.
                    CorrespondingGroup = await Task.Run(() => groupController.GetGroup(SelectedConversation.GroupId));
                    User localUser = groupController.GetLocalUser();
                    if (groupController.IsActiveParticipant(SelectedConversation.GroupId, localUser.Id))
                    {
                        IsActiveParticipant = true;
                    }
                    else
                    {
                        IsActiveParticipant = false;
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadSelectedConversationAsync: Failed to load selected conversation.");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();

            // Führe noch einen Aktualisierungsrequest im Hintergrund aus.
            await synchronizeConversationMessages(true, false);
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

        /// <summary>
        /// Hilfsmethode zur Aktualisierung der ConversationMessage Collection.
        /// Lädt fehlende Nachrichten aus den lokalen Datensätzen nach. Reine offline Funktionalität, d.h.
        /// kein Request an den Server.
        /// </summary>
        private void updateConversationMessagesCollection()
        {
            // Die bisher neuste Nachricht ist jetzt nicht mehr die aktuellste.
            ConversationMessage currentFrontMessage = ConversationMessages.FirstOrDefault<ConversationMessage>();
            if (currentFrontMessage != null)
                currentFrontMessage.IsLatestMessage = false;

            // Lade die Konversationen in die Collection.
            // Bestimme zunächst die höchste Nachrichtennummer in der Collection.
            int highestMessageNr = 0;
            if (ConversationMessages != null && ConversationMessages.Count > 0)
                highestMessageNr = ConversationMessages.Max(item => item.MessageNumber);

            Debug.WriteLine("updateConversationMessagesCollection: The current max message nr is: {0}.", highestMessageNr);

            // Rufe die Nachrichten ab.
            List<ConversationMessage> messages = groupController.GetConversationMessages(SelectedConversation.Id, highestMessageNr);
            foreach (ConversationMessage message in messages)
            {
                ConversationMessages.Insert(0, message);
            }

            // Setze neue aktuellste Nachricht.
            currentFrontMessage = ConversationMessages.FirstOrDefault<ConversationMessage>();
            if (currentFrontMessage != null)
                currentFrontMessage.IsLatestMessage = true;
        }

        /// <summary>
        /// Führt eine Synchronisation der Konversationsnachrichten mit dem Server aus.
        /// Es wird ein Request an den Server geschickt und die neusten Nachrichten abgerufen.
        /// Die Anzeige wird aktualisiert falls notwendig.
        /// </summary>
        /// <param name="withCaching">Gibt an, ob Caching bei dem Request erlaubt sein soll.</param>
        /// <param name="reportErrors">Gibt an, ob möglicherweise auftretende Fehler dem Nutzer angezeigt werden sollen.</param>
        private async Task synchronizeConversationMessages(bool withCaching, bool reportErrors)
        {
            try
            {
                displayIndeterminateProgressIndicator();

                if (SelectedConversation != null && 
                    SelectedConversation.IsClosed.HasValue && 
                    SelectedConversation.IsClosed.Value == false)
                {
                    await groupController.SynchronizeConversationMessagesWithServer(
                        SelectedConversation.GroupId,
                        SelectedConversation.Id,
                        withCaching);

                    // Aktualisiere noch die Anzeige.
                    updateConversationMessagesCollection();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("synchronizeConversationMessages: Failed to synchronize conversation messages.");
                if (reportErrors)
                    displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        #region CommandFunctionality
        /// <summary>
        /// Hilfsfunktion, welche die Prüfung der Ausführbarkeit der Befehle anstößt.
        /// </summary>
        private void checkCommandExecution()
        {
            SendMessageCommand.OnCanExecuteChanged();
            SynchronizeMessagesCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Prüft, ob der Befehl zum Senden einer Konversationsnachricht aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSendMessage()
        {
            if (SelectedConversation != null &&
                IsActiveParticipant)
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
                // Setze Wert auf null, wenn keine Nachricht eingegeben wurde.
                if (messageContent != null && 
                    messageContent.Trim() == string.Empty)
                {
                    messageContent = null;
                }
                   
                Priority priority = Priority.NORMAL;

                User localUser = groupController.GetLocalUser();

                if (CorrespondingGroup != null)
                {
                    // Wenn es eine Tutoriumsgruppe ist und der Nutzer Tutor ist, dann sende mit Priorität hoch.
                    if (CorrespondingGroup.Type == GroupType.TUTORIAL && CorrespondingGroup.GroupAdmin == localUser.Id)
                    {
                        Debug.WriteLine("executeSendMessageCommandAsync: Set message priority to high.");
                        priority = Priority.HIGH;
                    }
                }

                bool successful = await groupController.SendConversationMessageAsync(
                    SelectedConversation.GroupId, 
                    SelectedConversation.Id, 
                    messageContent, 
                    priority);

                if (successful)
                {
                    // Setze Textfeld zurück.
                    EnteredMessage = "";

                    // Aktualisere die Anzeige.
                    updateConversationMessagesCollection();
                }
                
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

        /// <summary>
        /// Prüft, ob der Befehl zur Synchronisation von Nachrichten aktuell zur Verfügung steht. 
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeMessages()
        {
            if (SelectedConversation != null &&
                IsActiveParticipant)
                return true;

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeMessagesCommand aus.
        /// </summary>
        private async Task executeSynchronizeMessagesCommand()
        {
            // Führe Synchronisation der Nachrichten durch.
            await synchronizeConversationMessages(false, true);
        }
        #endregion CommandFunctionality
    }
}
