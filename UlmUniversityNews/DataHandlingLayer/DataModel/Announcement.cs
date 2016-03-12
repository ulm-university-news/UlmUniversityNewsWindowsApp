using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.DataModel.Validator;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Eine Announcement repräsentiert eine Nachricht, die von einem Moderator eines
    /// Kanals in eben diesen Kanal geschickt wird. Abonnenten eines Kanals erhalten 
    /// Announcement Nachrichten, die von den verantwortlichen Moderatoren verschickt werden.
    /// </summary>
    public class Announcement : Message
    {
        #region Properties
        private int channelId;
        /// <summary>
        /// Die Id des Kanals zu dem die Announcement Nachricht gehört.
        /// </summary>
        [JsonProperty("channelId", NullValueHandling = NullValueHandling.Ignore)]
        public int ChannelId
        {
            get { return channelId; }
            set { channelId = value; }
        }

        private int authorModerator;
        /// <summary>
        /// Die Id des Autors (ein Moderator), der diese Nachricht verfasst hat.
        /// </summary>
        [JsonProperty("authorModerator", NullValueHandling = NullValueHandling.Ignore)]
        public int AuthorId
        {
            get { return authorModerator; }
            set { authorModerator = value; }
        }

        private string title;
        /// <summary>
        /// Der Title der Announcement Nachricht.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Announcement.
        /// </summary>
        public Announcement()
            : base()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Announcement.
        /// </summary>
        /// <param name="id">Die eindeutige Id der Announcement.</param>
        /// <param name="text">Der Inhalt der Announcement.</param>
        /// <param name="messageNumber">Die Nachrichtennummer der Announcement.</param>
        /// <param name="creationDate">Das Erstellungsdatum der Announcement.</param>
        /// <param name="priority">Die Priorität, mit der diese Announcement geschickt wurde.</param>
        /// <param name="isRead">Gib an, ob die Annoucement bereits vom Nutzer gelesen wurde.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem diese Announcement gehört.</param>
        /// <param name="authorId">Die Id des Autors der Announcement.</param>
        /// <param name="title">Der Titel der Announcement.</param>
        public Announcement(int id, string text, int messageNumber, DateTimeOffset creationDate, Priority priority, bool isRead,
            int channelId, int authorId, string title)
            : base(id, text, messageNumber, creationDate, priority, isRead)
        {
            this.channelId = channelId;
            this.authorModerator = authorId;
            this.title = title;
        }

        /// <summary>
        /// Validiert das Property Title.
        /// </summary>
        public void ValidateTitleProperty()
        {
            if (Title == null)
            {
                SetValidationError("Title", "AddAnnouncementValidationErrorTitleIsNull");
                return;
            }
            if (!checkStringRange(0, Constants.Constants.MaxAnnouncementTitleLength, Title))
            {
                SetValidationError("Title", "AddAnnouncementValidationErrorTitleTooLong");
            }
        }

        /// <summary>
        /// Validiert das Property Text.
        /// </summary>
        public void ValidateTextProperty()
        {
            if (Text == null)
            {
                SetValidationError("Text", "AddAnnouncementValidationErrorTextIsNull");
                return;
            }
            if (!checkStringRange(0, Constants.Constants.MaxAnnouncementContentLength, Text))
            {
                SetValidationError("Text", "AddAnnouncementValidationErrorTextTooLong");
            }
        }

        /// <summary>
        /// Validiere alle Properties, für die Validierungsregeln definiert sind.
        /// </summary>
        public override void ValidateAll()
        {
            ValidateTextProperty();
            ValidateTitleProperty();
        }
    }
}
