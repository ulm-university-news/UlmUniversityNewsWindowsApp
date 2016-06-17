using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse ConversationMessage repräsentiert eine Nachricht, die
    /// in einer Konversation gesendet wird. Die Nachricht wird an alle Teilnehmer
    /// der zugehörigen Gruppe verteilt.
    /// </summary>
    public class ConversationMessage : Message
    {
        #region Properties
        private int authorUser;
        /// <summary>
        /// Die Id des Autors (ein Nutzer) der Konversationsnachricht.
        /// </summary>
        [JsonProperty("authorUser", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AuthorId
        {
            get { return authorUser; }
            set { authorUser = value; }
        }

        private string authorName;
        /// <summary>
        /// Der Name des Autors der Nachricht.
        /// </summary>
        public string AuthorName
        {
            get { return authorName; }
            set { this.setProperty(ref this.authorName, value); }
        }
        
        private int conversationId;
        /// <summary>
        /// Die Id der Konversation, zu der die Konversationsnachricht gehört.
        /// </summary>
        [JsonProperty("conversationId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ConversationId
        {
            get { return conversationId; }
            set { conversationId = value; }
        }
        
        private bool isLatestMessage = false;
        /// <summary>
        /// Gibt an, ob diese Nachricht die aktuellste Nachricht in der Konversation ist.
        /// </summary>
        [JsonIgnore]
        public bool IsLatestMessage
        {
            get { return isLatestMessage; }
            set { this.setProperty(ref this.isLatestMessage, value); }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationMessage.
        /// </summary>
        public ConversationMessage()
            : base()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ConversationMessage.
        /// </summary>
        /// <param name="id">Die eindeutige Id der Konversationsnachricht.</param>
        /// <param name="text">Der Inhalt der Konversationsnachricht.</param>
        /// <param name="messageNumber">Die Nummer der Nachricht in der Konversation.</param>
        /// <param name="priority">Die Priorität, mit der die Konversationsnachricht verschickt wurde.</param>
        /// <param name="creationDate">Das Erstellungsdatum der Nachricht.</param>
        /// <param name="isRead">Gibt an, ob die Konversationsnachricht vom Nutzer bereits gelesen wurde.</param>
        /// <param name="authorId">Die Id des Autors der Nachricht.</param>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachricht gehört.</param>
        public ConversationMessage(int id, string text, int messageNumber, Priority priority, DateTimeOffset creationDate,
            bool isRead, int authorId, int conversationId)
            : base(id, text, messageNumber, creationDate, priority, isRead)
        {
            this.authorUser = authorId;
            this.conversationId = conversationId;
        }

        #region ValidationRules
        /// <summary>
        /// Validiert die Eigenschaft "Text" der ConversationMessage Instanz.
        /// </summary>
        public void ValidateTextProperty()
        {
            if (Text == null)
            {
                SetValidationError("Text", "VE_ConversationMessageTextIsNullValidationError");
            }
            else if (!checkStringRange(
                Constants.Constants.MinConversationMsgLength,
                Constants.Constants.MaxConversationMsgLength,
                Text))
            {
                SetValidationError("Text", "VE_ConversationMessageTextLengthValidationError");
            }
        }

        /// <summary>
        /// Methode, welche die Validierung aller Properties anstößt, 
        /// für die Validierungsregeln definiert sind.
        /// </summary>
        public override void ValidateAll()
        {
            ValidateTextProperty();
        }
        #endregion ValidationRules
    }
}
