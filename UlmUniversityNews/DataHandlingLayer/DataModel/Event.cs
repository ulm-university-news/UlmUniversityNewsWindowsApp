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
    /// Ein Event ist ein Kanal mit zusätzlichen Properties, die hinsichtlich eines Events relevant sind.
    /// </summary>
    public class Event : Channel
    {
        #region Properties
        private string cost;
        /// <summary>
        /// Die Kosten für das Event, z.B. Eintrittskosten.
        /// </summary>
        [JsonProperty("cost", NullValueHandling = NullValueHandling.Ignore)]
        public string Cost
        {
            get { return cost; }
            set { cost = value; }
        }

        private string organizer;
        /// <summary>
        /// Der Organisator des Events.
        /// </summary>
        [JsonProperty("organizer", NullValueHandling = NullValueHandling.Ignore)]
        public string Organizer
        {
            get { return organizer; }
            set { organizer = value; }
        }    
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Event.
        /// </summary>
        public Event()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Event.
        /// </summary>
        /// <param name="id">Die eindeutige Id des Events.</param>
        /// <param name="name">Der Name des Events.</param>
        /// <param name="description">Die Beschreibung des Events.</param>
        /// <param name="type">Der Typ des Kanals, hier Event.</param>
        /// <param name="creationDate">Das Erstellungsdatum des Events.</param>
        /// <param name="modiciationDate">Das Datum der letzten Änderung.</param>
        /// <param name="term">Das zugeordnete Semester.</param>
        /// <param name="locations">Ort, an dem das Event stattfindet.</param>
        /// <param name="dates">Termin, an dem das Event stattfindet.</param>
        /// <param name="contacts">Kontaktdaten von verantwortlichen Personen.</param>
        /// <param name="website">Ein oder mehrere Links auf Webseiten.</param>
        /// <param name="deleted">Gibt an, ob das Event als gelöscht markiert wurde.</param>
        /// <param name="cost">Die dem Event zugeordneten Kosten.</param>
        /// <param name="organizer">Der Veranstalter des Events.</param>
        public Event(int id, string name, string description, ChannelType type, DateTimeOffset creationDate, 
            DateTimeOffset modiciationDate, string term, string locations, string dates, string contacts, 
            string website, bool deleted, string cost, string organizer)
            : base(id, name, description, type, creationDate, modiciationDate, term, locations, dates, contacts, website, deleted)
        {
            this.cost = cost;
            this.organizer = organizer;
        }

        #region ValidationRules
        /// <summary>
        /// Validiert das Property "Organizer".
        /// </summary>
        public void ValidateOrganizerProperty()
        {
            if (Organizer == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelEventOrganizerInfoLength, Organizer))
            {
                SetValidationError("Organizer", "AddAndEditChannelEventOrganizerInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Validiert das Property "Cost".
        /// </summary>
        public void ValidateCostProperty()
        {
            if (Cost == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelEventCostInfoLength, Cost))
            {
                SetValidationError("Cost", "AddAndEditChannelEventCostInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Evaluiert alle Validierungsregeln für die Properties, denen eine solche Regel zugwiesen wurde.
        /// </summary>
        public override void ValidateAll()
        {
            System.Diagnostics.Debug.WriteLine("In ValidateAll of Event class.");

            ValidateCostProperty();
            ValidateOrganizerProperty();

            base.ValidateAll();
        }
        #endregion ValidationRules
    }
}
