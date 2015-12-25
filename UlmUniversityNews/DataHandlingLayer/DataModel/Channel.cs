using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Channel Klasse repräsentiert einen Kanal, über den Nachrichten an die Abonnenten verteilt werden können.
    /// </summary>
    public class Channel
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id des Kanals.
        /// </summary>
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// Der Name des Kanals.
        /// </summary>
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string description;
        /// <summary>
        /// Die Beschreibung des Kanals.
        /// </summary>
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private ChannelType type;
        /// <summary>
        /// Gibt an, um welchen Typ es sich bei dem Kanal handelt.
        /// </summary>
        public ChannelType Type
        {
            get { return type; }
            set { type = value; }
        }

        private DateTime creationDate;
        /// <summary>
        /// Das Erstellungsdatum des Kanals.
        /// </summary>
        public DateTime CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTime modificationDate;
        /// <summary>
        /// Das Datum der letzten Änderung des Kanals.
        /// </summary>
        public DateTime ModificationDate
        {
            get { return modificationDate; }
            set { modificationDate = value; }
        }

        private string term;
        /// <summary>
        /// Das Semester, das für den Kanal angegeben wurde.
        /// </summary>
        public string Term
        {
            get { return term; }
            set { term = value; }
        }

        private string locations;
        /// <summary>
        /// Gibt Orte an, die hinsichtlich des Kanals relevant sind.
        /// </summary>
        public string Locations
        {
            get { return locations; }
            set { locations = value; }
        }

        private string dates;
        /// <summary>
        /// Gibt Termine an, die hinsichtlich des Kanals relevant an.
        /// </summary>
        public string Dates
        {
            get { return dates; }
            set { dates = value; }
        }

        private string contacts;
        /// <summary>
        /// Gibt Kontaktdaten von Personen an, die für den Kanal zuständig sind.
        /// </summary>
        public string Contacts
        {
            get { return contacts; }
            set { contacts = value; }
        }

        private string website;
        /// <summary>
        /// Ein Link zu einer Webadresse. 
        /// </summary>
        public string Website
        {
            get { return website; }
            set { website = value; }
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
        public Channel(int id, string name, string description, ChannelType type, DateTime creationDate, 
            DateTime modificationDate, string term, string locations, string dates, string contacts, string website)
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
        }
    }
}
