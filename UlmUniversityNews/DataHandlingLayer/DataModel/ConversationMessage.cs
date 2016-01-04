﻿using DataHandlingLayer.DataModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    public class ConversationMessage : Message
    {
        #region Properties
        private int authorUser;
        /// <summary>
        /// Die Id des Autors (ein Nutzer) der Konversationsnachricht.
        /// </summary>
        public int AuthorId
        {
            get { return authorUser; }
            set { authorUser = value; }
        }

        private int conversationId;
        /// <summary>
        /// Die Id der Konversation, zu der die Konversationsnachricht gehört.
        /// </summary>
        public int ConversationId
        {
            get { return conversationId; }
            set { conversationId = value; }
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
        public ConversationMessage(int id, string text, int messageNumber, Priority priority, DateTime creationDate,
            bool isRead, int authorId, int conversationId)
            : base(id, text, messageNumber, creationDate, priority, isRead)
        {
            this.authorUser = authorId;
            this.conversationId = conversationId;
        }
    }
}