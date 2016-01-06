using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Channel Klasse repräsentiert einen Kanal, über den Nachrichten an die Abonnenten verteilt werden können.
    /// </summary>
    public class Channel : PropertyChangedNotifier
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id des Kanals.
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// Der Name des Kanals.
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string description;
        /// <summary>
        /// Die Beschreibung des Kanals.
        /// </summary>
        [JsonProperty("description")]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private ChannelType type;
        /// <summary>
        /// Gibt an, um welchen Typ es sich bei dem Kanal handelt.
        /// </summary>
        [JsonProperty("type"), JsonConverter(typeof(StringEnumConverter))]
        public ChannelType Type
        {
            get { return type; }
            set { type = value; }
        }

        private DateTime creationDate;
        /// <summary>
        /// Das Erstellungsdatum des Kanals.
        /// </summary>
        [JsonProperty("creationDate", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTime modificationDate;
        /// <summary>
        /// Das Datum der letzten Änderung des Kanals.
        /// </summary>
        [JsonProperty("modificationDate", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTime ModificationDate
        {
            get { return modificationDate; }
            set { modificationDate = value; }
        }

        private string term;
        /// <summary>
        /// Das Semester, das für den Kanal angegeben wurde.
        /// </summary>
        [JsonProperty("term", NullValueHandling = NullValueHandling.Ignore)]
        public string Term
        {
            get { return term; }
            set { term = value; }
        }

        private string locations;
        /// <summary>
        /// Gibt Orte an, die hinsichtlich des Kanals relevant sind.
        /// </summary>
        [JsonProperty("locations", NullValueHandling = NullValueHandling.Ignore)]
        public string Locations
        {
            get { return locations; }
            set { locations = value; }
        }

        private string dates;
        /// <summary>
        /// Gibt Termine an, die hinsichtlich des Kanals relevant an.
        /// </summary>
        [JsonProperty("dates", NullValueHandling = NullValueHandling.Ignore)]
        public string Dates
        {
            get { return dates; }
            set { dates = value; }
        }

        private string contacts;
        /// <summary>
        /// Gibt Kontaktdaten von Personen an, die für den Kanal zuständig sind.
        /// </summary>
        [JsonProperty("contacts", NullValueHandling = NullValueHandling.Ignore)]
        public string Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }

        private string website;
        /// <summary>
        /// Ein Link zu einer Webadresse. 
        /// </summary>
        [JsonProperty("website", NullValueHandling = NullValueHandling.Ignore)]
        public string Website
        {
            get { return website; }
            set { website = value; }
        }

        private bool deleted;
        /// <summary>
        /// Gibt an, ob ein Kanal als gelöscht markiert wurde.
        /// </summary>
        [JsonIgnore]
        public bool Deleted
        {
            get { return deleted; }
            set { deleted = value; }
        }

        private int numberOfUnreadAnnouncements;
        /// <summary>
        /// Gibt an, wie viele ungelesenen Nachrichten der Kanal aktuell hat.
        /// Ist der Kanal noch nicht abonniert ist die Anzahl immer 0.
        /// </summary>
        [JsonIgnore]
        public int NumberOfUnreadAnnouncements
        {
            get { return numberOfUnreadAnnouncements; }
            set { base.setProperty(ref this.numberOfUnreadAnnouncements, value); }
        }   
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Channel.
        /// </summary>
        public Channel()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Channel.
        /// </summary>
        /// <param name="id">Die eindeutige Id des Kanals.</param>
        /// <param name="name">Der Name des Kanals.</param>
        /// <param name="description">Eine Beschreibung des Kanals.</param>
        /// <param name="type">Der Typ des Kanals.</param>
        /// <param name="creationDate">Das Erstellungsdatum des Kanals.</param>
        /// <param name="modificationDate">Das Datum der letzten Änderung des Kanals.</param>
        /// <param name="term">Das Semester, das für den Kanal angegeben wurde.</param>
        /// <param name="locations">Orte, die bezüglich des Kanals relevant sind.</param>
        /// <param name="dates">Termine, die bezüglich des Kanals relevant sind.</param>
        /// <param name="contacts">Kontaktdaten von verantwortlichen Personen.</param>
        /// <param name="website">Ein oderer mehrere Links zu Webseiten.</param>
        /// <param name="deleted">Gibt an, ob der Kanal als gelöscht markiert wurde.</param>
        public Channel(int id, string name, string description, ChannelType type, DateTime creationDate, 
            DateTime modificationDate, string term, string locations, string dates, string contacts, string website, bool deleted)
        {
            this.id = id;
            this.name = name;
            this.description = description;
            this.type = type;
            this.creationDate = creationDate;
            this.modificationDate = modificationDate;
            this.term = term;
            this.locations = locations;
            this.dates = dates;
            this.contacts = contacts;
            this.website = website;
            this.deleted = deleted;
        }
    }
}
