using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse Message repräsentiert allgemein eine gesendete Nachricht. Eine Nachricht
    /// hat eine eindeutige Id sowie eine Nummer bezüglich der Ressource zu der sie gehört. 
    /// Der Inhalt einer Nachricht ist eine normale Zeichenfolge.
    /// </summary>
    public class Message
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id einer Nachricht.
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string text;
        /// <summary>
        /// Der Nachrichteninhalt als String.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        private int messageNumber;
        /// <summary>
        /// Die Nachrichtennummer der Nachricht bezüglich der Ressource zu der die Nachricht gehört. Nachrichten gehören 
        /// immer zu einer Ressource, z.B. zu einem Kanal oder einer Gruppen-Konversation. Innerhalb der zugehörigen Ressource
        /// sind die Nachrichten abhängig vom Datum ihrer Erstellung aufsteigend durchnummeriert. 
        /// </summary>
        public int MessageNumber
        {
            get { return messageNumber; }
            set { messageNumber = value; }
        }

        private DateTime creationDate;
        /// <summary>
        /// Das Erstellungsdatum der Nachricht.
        /// </summary>
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private Priority messagePriority;
        /// <summary>
        /// Die Priorität, mit der die Nachricht verschickt wurde.
        /// </summary>
        public Priority MessagePriority
        {
            get { return messagePriority; }
            set { messagePriority = value; }
        }

        private bool isRead;
        /// <summary>
        /// Gibt an, ob die Nachricht bereits gelesen wurde.
        /// </summary>
        public bool Read
        {
            get { return isRead; }
            set { isRead = value; }
        }  
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Message.
        /// </summary>
        public Message()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Message.
        /// </summary>
        /// <param name="id">Die eindeutige Id einer Nachricht.</param>
        /// <param name="text">Der Nachrichteninhalt als String.</param>
        /// <param name="messageNumber">Die Nachrichtenummer bezüglich der Ressource, zu der die Nachricht gehört.</param>
        /// <param name="creationDate">Das Erstellungsdatum der Nachricht.</param>
        /// <param name="messagePriority">Die Priorität mit der die Nachricht verschickt wurde.</param>
        /// <param name="isRead">Gibt an, ob die Nachricht bereits gelesen wurde.</param>
        public Message(int id, string text, int messageNumber, DateTime creationDate, Priority messagePriority, bool isRead)
        {
            this.id = id;
            this.text = text;
            this.messageNumber = messageNumber;
            this.creationDate = creationDate;
            this.messagePriority = messagePriority;
            this.isRead = isRead;
        }
    }
}
