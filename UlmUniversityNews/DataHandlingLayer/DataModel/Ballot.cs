using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse Ballot repräsentiert eine Abstimmung, die einer Gruppe zugeordnet ist.
    /// Eine Abstimmung kann mehrere Abstimmungsoptionen haben, für die die Teilnehmer der Gruppe
    /// abstimmen können.
    /// </summary>
    public class Ballot : PropertyChangedNotifier
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id dieser Abstimmung.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string title;
        /// <summary>
        /// Der Titel der Abstimmung.
        /// </summary>
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title
        {
            get { return title; }
            set { title = value; }
        }

        private string description;
        /// <summary>
        /// Die Beschreibung der Abstimmung.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private int adminId;
        /// <summary>
        /// Die Id des Teilnehmers, der Administrator dieser Abstimmung ist.
        /// </summary>
        [JsonProperty("admin", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int AdminId
        {
            get { return adminId; }
            set { adminId = value; }
        }

        private string adminName;
        /// <summary>
        /// Der Name des Administrators der Abstimmung.
        /// </summary>
        public string AdminName
        {
            get { return adminName; }
            set { adminName = value; }
        }

        private bool? isClosed;
        /// <summary>
        /// Gibt an, ob die Abstimmung geschlossen ist.
        /// </summary>
        [JsonProperty("closed", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsClosed
        {
            get { return isClosed; }
            set { isClosed = value; }
        }

        private bool? isMultipleChoice;
        /// <summary>
        /// Gibt an, ob es sich bei der Abstimmung um eine Multiple
        /// Choice Abstimmung handelt.
        /// </summary>
        [JsonProperty("multipleChoice", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsMultipleChoice
        {
            get { return isMultipleChoice; }
            set { isMultipleChoice = value; }
        }

        private bool? hasPublicVotes;
        /// <summary>
        /// Gibt an, ob die Abstimmungsergebnisse anonmy oder öffentlich
        /// veröffentlicht werden.
        /// </summary>
        [JsonProperty("publicVotes", NullValueHandling = NullValueHandling.Ignore)]
        public bool? HasPublicVotes
        {
            get { return hasPublicVotes; }
            set { hasPublicVotes = value; }
        }

        private List<Option> options;
        /// <summary>
        /// Die der Abstimmung zugeordneten Abstimmungsoptionen.
        /// </summary>
        [JsonIgnore]
        public List<Option> Options
        {
            get { return options; }
            set { options = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Ballot.
        /// </summary>
        public Ballot()
        {

        }
    }
}
