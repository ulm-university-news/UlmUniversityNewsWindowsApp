using DataHandlingLayer.DataModel.Enums;
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
        public string Cost
        {
            get { return cost; }
            set { cost = value; }
        }

        private string numberOfParticipants;
        /// <summary>
        /// Die maximale Anzahl an Teilnehmern bei der Sportveranstaltung.
        /// </summary>
        public string NumberOfParticipants
        {
            get { return numberOfParticipants; }
            set { numberOfParticipants = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz von der Klasse Sports.
        /// </summary>
        public Sports()
        {

        }

        public Sports(int id, string name, string description, ChannelType type, DateTime creationDate,
            DateTime modificationDate, string term, string locations, string dates, string contacts, string website,
            string cost, string numberOfParticipants)
            : base(id, name, description, type, creationDate, modificationDate, term, locations, dates, contacts, website)
        {
            this.cost = cost;
            this.numberOfParticipants = numberOfParticipants;
        }
    }
}
