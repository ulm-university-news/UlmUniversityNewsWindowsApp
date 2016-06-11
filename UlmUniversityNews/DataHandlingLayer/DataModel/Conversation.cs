﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse Conversation repräsentiert eine Konversation. Eine Konversation gehört zu einer Gruppe 
    /// und enthält Nachrichten. Nachrichten können in die Konversation gesendet werden von Teilnehmern der Gruppe.
    /// Die Nachrichten werden an alle Teilnehmer verteilt. Konversationen bündeln Nachrichten unter einem bestimmten 
    /// Thema (Title).
    /// </summary>
    public class Conversation
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id der Konversation.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string title;
        /// <summary>
        /// Der Titel der Konversation.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private bool? isClosed;
        /// <summary>
        /// Gibt an, ob die Konversation geschlossen ist.
        /// </summary>
        [JsonProperty("closed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsClosed
        {
            get { return isClosed; }
            set { isClosed = value; }
        }

        private int adminId;
        /// <summary>
        /// Die Id des Teilnehmers der Gruppe, der Administrator dieser Konversation ist.
        /// </summary>
        [JsonProperty("admin", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AdminId
        {
            get { return adminId; }
            set { adminId = value; }
        }

        private List<ConversationMessage> conversationMessages;
        /// <summary>
        /// Die zu dieser Konversation gehörenden Nachrichten.
        /// </summary>
        [JsonIgnore]
        public List<ConversationMessage> ConversationMessages
        {
            get { return conversationMessages; }
            set { conversationMessages = value; }
        }

        private int amountOfUnreadMessages;
        /// <summary>
        /// Gibt an, wie viele ungelesene Nachrichten aktuell in der Konversation vorliegen.
        /// </summary>
        [JsonIgnore]
        public int AmountOfUnreadMessages
        {
            get { return amountOfUnreadMessages; }
            set { amountOfUnreadMessages = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Conversation.
        /// </summary>
        public Conversation()
        {

        }
    }
}
