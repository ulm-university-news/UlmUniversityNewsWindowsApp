using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Ein Reminder ist eine Ressource, die in einem bestimmten Intervall
    /// eine definierte Announcement Nachricht erzeugt und in den Kanal schickt, für
    /// den der Reminder definiert ist.
    /// </summary>
    public class Reminder
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id der Reminder-Ressource.
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private DateTime creationDate;
        /// <summary>
        /// Das Erstellungsdatum des Reminder.
        /// </summary>
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTime modificationDate;
        /// <summary>
        /// Das Änderungsdatum des Reminder.
        /// </summary>
        public DateTime ModificationDate
        {
            get { return modificationDate; }
            set { modificationDate = value; }
        }

        private DateTime startDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder zum ersten Mal feuern soll.
        /// </summary>
        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        private DateTime nextDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder das nächste Mal feuert.
        /// </summary>
        public DateTime NextDate
        {
            get { return nextDate; }
            set { nextDate = value; }
        }
        
        private DateTime endDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder abläuft und deaktiviert werden soll.
        /// </summary>
        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        private int interval;
        /// <summary>
        /// Das Intervall, in dem der Reminder die Announcements feuert.
        /// </summary>
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }

        private bool ignore;
        /// <summary>
        /// Gibt an, ob der Reminder beim nächsten Termin aussetzen soll.
        /// </summary>
        public bool Ignore
        {
            get { return ignore; }
            set { ignore = value; }
        }

        private int channelId;
        /// <summary>
        /// Die Id des Kanals zu dem der Reminder gehört.
        /// </summary>
        public int ChannelId
        {
            get { return channelId; }
            set { channelId = value; }
        }

        private int authorModerator;
        /// <summary>
        /// Die Id des Autors (ein Moderator), der für die erzeugten Announcement als Autor eingetragen wird.
        /// </summary>
        public int AuthorId
        {
            get { return authorModerator; }
            set { authorModerator = value; }
        }

        private string title;
        /// <summary>
        /// Der Titel, der bei den vom Reminder erzeugten Announcements gesetzt wird.
        /// </summary>
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string text;
        /// <summary>
        /// Der Inhalt der Announcements, die durch den Reminder erzeugt werden.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        private Priority messagePriority;
        /// <summary>
        /// Die Priorität, mit der vom Reminder erzeugte Announcements verschickt werden.
        /// </summary>
        public Priority MessagePriority
        {
            get { return messagePriority; }
            set { messagePriority = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Reminder.
        /// </summary>
        public Reminder()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Reminder.
        /// </summary>
        /// <param name="id">Die eindeutige Id der Reminder-Ressource.</param>
        /// <param name="creationDate">Das Erstellungsdatum des Reminder.</param>
        /// <param name="modificationDate">Das Änderungsdatum des Reminder.</param>
        /// <param name="startDate">Das Datum und die Uhrzeit, an dem der Reminder zum ersten Mal feuern soll.</param>
        /// <param name="endDate">Das Datum und die Uhrzeit, an dem der Reminder deaktiviert werden soll.</param>
        /// <param name="interval">Das Intervall, in dem der Reminder Announcement Nachrichten erstellen und verschicken soll.</param>
        /// <param name="ignore">Gibt an, ob der Reminder beim nächsten Termin aussetzen soll.</param>
        /// <param name="channelId">Die Id des Kanals, zu dem der Reminder gehört.</param>
        /// <param name="authorId">Die Id des Moderators, der in den erzeugten Announcements als Autor genannt wird.</param>
        /// <param name="title">Der Titel, der in den vom Reminder erzeugten Announcements gesetzt wird.</param>
        /// <param name="text">Der Inhalt der Announcements, die vom Reminder verschickt werden.</param>
        /// <param name="priority">Die Priorität, mit der die vom Reminder erzeugten Announcements verschickt werden sollen.</param>
        public Reminder(int id, DateTime creationDate, DateTime modificationDate, DateTime startDate, DateTime endDate,
            int interval, bool ignore, int channelId, int authorId, string title, string text, Priority priority)
        {
            this.id = id;
            this.creationDate = creationDate;
            this.modificationDate = modificationDate;
            this.startDate = startDate;
            this.endDate = endDate;
            this.interval = interval;
            this.ignore = ignore;
            this.channelId = channelId;
            this.authorModerator = authorId;
            this.title = title;
            this.text = text;
            this.messagePriority = priority;
        }
    }
}
