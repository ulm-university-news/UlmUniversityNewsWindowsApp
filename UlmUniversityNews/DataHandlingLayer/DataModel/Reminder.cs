using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using DataHandlingLayer.DataModel.Validator;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Ein Reminder ist eine Ressource, die in einem bestimmten Intervall
    /// eine definierte Announcement Nachricht erzeugt und in den Kanal schickt, für
    /// den der Reminder definiert ist.
    /// </summary>
    public class Reminder : PropertyChangedNotifier
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id der Reminder-Ressource.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private DateTime creationDate;
        /// <summary>
        /// Das Erstellungsdatum des Reminder.
        /// </summary>
        [JsonProperty("creationDate", DefaultValueHandling=DefaultValueHandling.Ignore)]
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTime modificationDate;
        /// <summary>
        /// Das Änderungsdatum des Reminder.
        /// </summary>
        [JsonProperty("modificationDate", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime ModificationDate
        {
            get { return modificationDate; }
            set { modificationDate = value; }
        }

        private DateTime startDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder zum ersten Mal feuern soll.
        /// </summary>
        [JsonProperty("startDate", DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        private DateTime nextDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder das nächste Mal feuert.
        /// </summary>
        [JsonIgnore]
        public DateTime NextDate
        {
            get { return nextDate; }
            set { this.setProperty(ref this.nextDate, value); }
        }
        
        private DateTime endDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an dem der Reminder abläuft und deaktiviert werden soll.
        /// </summary>
        [JsonProperty("endDate", DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        private int interval;
        /// <summary>
        /// Das Intervall, in dem der Reminder die Announcements feuert.
        /// Ist das Intervall = 0, dann handelt es sich um ein One-Time Reminder, der genau 
        /// einmal feuert.
        /// </summary>
        [JsonProperty("interval", NullValueHandling = NullValueHandling.Ignore)]
        public int Interval
        {
            get { return interval; }
            set { interval = value; }
        }

        private bool ignore;
        /// <summary>
        /// Gibt an, ob der Reminder beim nächsten Termin aussetzen soll.
        /// </summary>
        [JsonProperty("ignore")]
        public bool Ignore
        {
            get { return ignore; }
            set { ignore = value; }
        }

        private int channelId;
        /// <summary>
        /// Die Id des Kanals zu dem der Reminder gehört.
        /// </summary>
        [JsonProperty("channelId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int ChannelId
        {
            get { return channelId; }
            set { channelId = value; }
        }

        private int authorModerator;
        /// <summary>
        /// Die Id des Autors (ein Moderator), der für die erzeugten Announcement als Autor eingetragen wird.
        /// </summary>
        [JsonProperty("authorModerator", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AuthorId
        {
            get { return authorModerator; }
            set { authorModerator = value; }
        }

        private string title;
        /// <summary>
        /// Der Titel, der bei den vom Reminder erzeugten Announcements gesetzt wird.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string text;
        /// <summary>
        /// Der Inhalt der Announcements, die durch den Reminder erzeugt werden.
        /// </summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        private Priority messagePriority;
        /// <summary>
        /// Die Priorität, mit der vom Reminder erzeugte Announcements verschickt werden.
        /// </summary>
        [JsonProperty("priority"), JsonConverter(typeof(StringEnumConverter))]
        public Priority MessagePriority
        {
            get { return messagePriority; }
            set { messagePriority = value; }
        }

        private bool isExpired;
        /// <summary>
        /// Gibt an, ob der Reminder abgelaufen ist.
        /// Ist der Reminder abgelaufen, so wird er keine Announcements mehr feuern.
        /// </summary>
        [JsonIgnore]
        public bool IsExpired
        {
            get { return isExpired; }
            set { this.setProperty(ref this.isExpired, value); }
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

        #region ReminderRelatedFunctionality
        /// <summary>
        /// Prüft, ob der Reminder bereits abgelaufen ist. Ein abgelaufener Reminder wird nicht mehr feuern.
        /// Setzt den IsExpired Property Wert.
        /// </summary>
        /// <returns>Liefert true, wenn der Reminder abgelaufen ist, ansonsten false.</returns>
        public void EvaluateIsExpired()
        {
            // Wenn es ein One-Time Reminder ist, dann muss dessen Start-Datum in der Zukunft liegen.
            if (Interval == 0)
            {
                int comparisonResult = DateTime.Compare(StartDate, DateTime.Now);
                if (comparisonResult < 0)
                {
                    // StartDate ist früher als aktuelles Datum. Das heißt, der Reminder ist abgelaufen.
                    IsExpired = true;
                    return;
                }
                else
                {
                    // StartDate ist nicht früher als aktuelles Datum. Reminder noch aktiv.
                    IsExpired = false;
                    return;
                }
            }

            // Prüfe, ob Ende-Datum des Reminders bereits vorbei ist.
            int endDateComparisonResult = DateTime.Compare(EndDate, DateTime.Now);
            if (endDateComparisonResult < 0)
            {
                // EndDate ist früher als das aktuelle Datum. Reminder ist abgelaufen.
                IsExpired = true;
                return;
            }

            // Prüfe, ob der nächste Termin für den Reminder nach dem Ende-Datum liegt.
            int nextReminderComparisonResult = DateTime.Compare(EndDate, NextDate);
            if (nextReminderComparisonResult < 0)
            {
                // EndDate ist früher als nächster Termin des Reminders. Reminder ist abgelaufen.
                IsExpired = true;
                return;
            }

            // Reminder noch nicht abgelaufen.
            IsExpired = false;
        }

        /// <summary>
        /// Berechnet und setzt den nächsten Termin, an dem der Reminder feuern wird.
        /// </summary>
        public void ComputeNextDate()
        {
            // Wenn das Intervall gleich 0 ist, dann ist es ein One-Time Reminder. 
            // Man kann diesen somit als abgelaufen markieren.
            if (Interval == 0)
            {
                NextDate = EndDate.AddSeconds(1.0f);
            }
            else
            {
                // Addiere Intervall auf, um nächstes Datum zu bekommen.
                NextDate = NextDate.AddSeconds(Interval);
            }
        }

        /// <summary>
        /// Berechnet und setzt den ersten Termin, an dem Reminder feuern wird.
        /// </summary>
        public void ComputeFirstNextDate()
        {
            // Setze den ersten Termin für den Reminder. Setze zunächst auf den Start-Termin.
            NextDate = StartDate;

            // Bei Intervall gleich 0, d.h. One-Time Reminder, ist der nächste Termin gleich dem Start-Termin.
            if (Interval == 0)
            {
                return;
            }

            // Der nächste Termin muss in der Zukunft liegen.
            while (NextDate.CompareTo(DateTime.Now) < 0)
            {
                NextDate = NextDate.AddSeconds(Interval);
            }

            // Wenn Reminder für nächsten Termin ausgesetzt ist.
            if (Ignore)
            {
                ComputeNextDate();
            }
        }
        #endregion ReminderRelatedFunctionality

        #region ValidationRules
        /// <summary>
        /// Validiert das Property Title.
        /// </summary>
        public void ValidateTitleProperty()
        {
            if (Title == null)
            {
                SetValidationError("Title", "AddAnnouncementValidationErrorTitleIsNull");   // Verwende gleiche Fehlernachricht wie bei Announcement.
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
                SetValidationError("Text", "AddAnnouncementValidationErrorTextIsNull"); // Verwende gleiche Fehlernachricht wie bei Announcement.
                return;
            }
            if (!checkStringRange(0, Constants.Constants.MaxAnnouncementContentLength, Text))
            {
                SetValidationError("Text", "AddAnnouncementValidationErrorTextTooLong");
            }
        }

        /// <summary>
        /// Validiert das Property "Interval".
        /// </summary>
        public void ValidateInterval()
        {
            // Interval 0 ist ein valides Interval (One-Time Reminder).
            if (Interval == 0)
                return;

            // Wenn der Start-Termin gleich dem Ende-Termin ist, dann muss es sich um einen One-Time Reminder handeln.
            if (StartDate.CompareTo(EndDate) == 0 && Interval != 0)
            {
                SetValidationError("Interval", "AddAndEditReminderStartAndEndDateEqualIntervalInvalidValidationError");
            }
            else
            {
                // Prüfe, ob das Interval ein mehrfaches eines Tages ist. (86400s = 24h * 60m * 60s).
                if (Interval % 86400 != 0)
                {
                    SetValidationError("Interval", "AddAndEditReminderInvalidIntervalValidationError");
                }
                else
                {
                    // Prüfe, ob das Interval wenigstens einen Tag umfasst, und nicht mehr als 28 Tage (4 Wochen).
                    if (Interval < 86400 || Interval > 2419200)
                    {
                        SetValidationError("Interval", "AddAndEditReminderInvalidIntervalValidationError");
                    }
                }
            }
        }

        /// <summary>
        /// Validiert die Properties StartDate und EndDate.
        /// </summary>
        public void ValidateStartAndEndDate()
        {
            // Prüfe zunächst, ob Start- und Ende-Datum gesetzt wurden.
            if (StartDate == null || EndDate == null)
            {
                SetValidationError("StartAndEndDate", "AddAndEditReminderStartOrEndDateNotSetValidatonError");
                return;
            }

            if (StartDate.CompareTo(DateTime.MinValue) == 0 || EndDate.CompareTo(DateTime.MinValue) == 0)
            {
                SetValidationError("StartAndEndDate", "AddAndEditReminderStartOrEndDateNotSetValidatonError");
                return;
            }

            // Prüfe, ob das Start-Datum nach dem Ende-Datum ist.
            if (StartDate.CompareTo(EndDate) > 0)
            {
                SetValidationError("StartAndEndDate", "AddAndEditReminderStartDateAfterEndDateValidationError");
            }
            else if (EndDate.CompareTo(DateTime.Now) < 0)  // Prüfe, ob das Ende-Datum in der Zukunft liegt.
            {
                SetValidationError("StartAndEndDate", "AddAndEditReminderEndDateInPastValidationError");
            }
        }

        /// <summary>
        /// Validiert alle Properties der Reminder Klasse, für die Validierungsregeln definiert sind.
        /// </summary>
        public override void ValidateAll()
        {
            ValidateTitleProperty();
            ValidateTextProperty();
            ValidateStartAndEndDate();
            ValidateInterval();
        }
        #endregion ValidatonRules
    }
}
