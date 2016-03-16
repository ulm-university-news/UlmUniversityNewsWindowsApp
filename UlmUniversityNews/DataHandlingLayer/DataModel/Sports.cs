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
    /// Ein Kanal zu einer Sportveranstaltung bzw. Sportgruppe (Hochschulsport) hat zusätzliche Properties.
    /// </summary>
    public class Sports : Channel
    {
        #region Properties
        private string cost;
        /// <summary>
        /// Die Kosten für die Sportveranstaltung.
        /// </summary>
        [JsonProperty("cost", NullValueHandling = NullValueHandling.Ignore)]
        public string Cost
        {
            get { return cost; }
            set { this.setProperty(ref this.cost, value); }
        }

        private string numberOfParticipants;
        /// <summary>
        /// Die maximale Anzahl an Teilnehmern bei der Sportveranstaltung.
        /// </summary>
        [JsonProperty("numberOfParticipants", NullValueHandling = NullValueHandling.Ignore)]
        public string NumberOfParticipants
        {
            get { return numberOfParticipants; }
            set { this.setProperty(ref this.numberOfParticipants, value); }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Sports.
        /// </summary>
        public Sports()
        {

        }

        public Sports(int id, string name, string description, ChannelType type, DateTimeOffset creationDate,
            DateTimeOffset modificationDate, string term, string locations, string dates, string contacts, string website,
            bool deleted, string cost, string numberOfParticipants)
            : base(id, name, description, type, creationDate, modificationDate, term, locations, dates, contacts, website, deleted)
        {
            this.cost = cost;
            this.numberOfParticipants = numberOfParticipants;
        }

        #region ValidationRules
        /// <summary>
        /// Validiert das Property "NumberOfParticipants".
        /// </summary>
        public void ValidateNumberOfParticipantsProperty()
        {
            if (NumberOfParticipants == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelSportsNrOfParticipantsInfoLength, NumberOfParticipants))
            {
                SetValidationError("NumberOfParticipants", "AddAndEditChannelSportsNumberOfParticipantsInfoTooLongValidationError");
            }
        }
        
        /// <summary>
        /// Validiert das Property "Cost".
        /// </summary>
        public void ValidateCostProperty()
        {
            if (Cost == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelSportsCostInfoLength, Cost))
            {
                SetValidationError("Cost", "AddAndEditChannelSportsCostInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Evaluiert alle Validierungsregeln für die Properties, denen eine solche Regel zugwiesen wurde.
        /// </summary>
        public override void ValidateAll()
        {
            System.Diagnostics.Debug.WriteLine("In ValidateAll of Sports.");

            ValidateCostProperty();
            ValidateNumberOfParticipantsProperty();

            base.ValidateAll();
        }
        #endregion ValidationRules
    }
}
