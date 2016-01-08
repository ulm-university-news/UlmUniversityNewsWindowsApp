using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.DataModel.FakeDataForDesign
{
    public class FakeChannel
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

        //private DataHandlingLayer.DataModel.Enums.ChannelType type;

        //public DataHandlingLayer.DataModel.Enums.ChannelType Type
        //{
        //    get { return type; }
        //    set { type = value; }
        //}

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

        private bool deleted;
        /// <summary>
        /// Gibt an, ob ein Kanal als gelöscht markiert wurde.
        /// </summary>
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
        public int NumberOfUnreadAnnouncements
        {
            get { return numberOfUnreadAnnouncements; }
            set { numberOfUnreadAnnouncements = value; }
        }
        #endregion Properties
    }
}
