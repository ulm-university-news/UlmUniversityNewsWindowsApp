﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse Option repräsentiert eine Abstimmungsoption, die einer Abstimmung zugeordnet ist.
    /// Eine Abstimmungsoption stellt eine Wahlmöglichkeit einer Abstimmung dar. Abstimmungsoptionen
    /// werden mit den entsprechenden Nutzern verknüpft, die für diese Option abgestimmt haben.
    /// </summary>
    public class Option
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id dieser Abstimmungsoption.
        /// </summary>
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string text;
        /// <summary>
        /// Der Text der Abstimmungsoption
        /// </summary>
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text
        {
            get { return text; }
            set { text = value; }
        }

        private List<int> voterIds;
        /// <summary>
        /// Liste von Identifier, die eindeutig auf Nutzer verweisen,
        /// die für diese Abstimmungsoption gestimmt haben.
        /// </summary>
        [JsonIgnore]
        public List<int> VoterIds
        {
            get { return voterIds; }
            set { voterIds = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeuge Instanz der Klasse Option.
        /// </summary>
        public Option()
        {

        }
    }
}
