using Newtonsoft.Json;
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
    public class Option : PropertyChangedNotifier
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
            set { this.setProperty(ref this.text, value); }
        }

        private bool isChosen;
        /// <summary>
        /// Gibt an, ob der Nutzer die Abstimmungsoption gewählt hat.
        /// </summary>
        [JsonIgnore]
        public bool IsChosen
        {
            get { return isChosen; }
            set { this.setProperty(ref this.isChosen, value); }
        }

        private List<int> voterIds;
        /// <summary>
        /// Liste von Identifier, die eindeutig auf Nutzer verweisen,
        /// die für diese Abstimmungsoption gestimmt haben.
        /// </summary>
        [JsonProperty("voters", NullValueHandling = NullValueHandling.Ignore)]
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

        #region ValidationRules
        /// <summary>
        /// Validiert die Eigenschaft "Text" der Option Instanz.
        /// </summary>
        public void ValidateTextProperty()
        {
            if (Text == null || Text.Trim().Length == 0)
            {
                SetValidationError("Text", "VE_BallotOptionIsNullValidationError");
            }
            else if (!checkStringRange(
                Constants.Constants.MinBallotOptionLength, 
                Constants.Constants.MaxBallotOptionLength,
                Text))
            {
                SetValidationError("Text", "VE_BallotOptionInvalidLengthValidationError");
            }
        }

        /// <summary>
        /// Stößt die Validierung der Eigenschaften an, für die
        /// eine Validierungsregel definiert wurde.
        /// </summary>
        public override void ValidateAll()
        {
            ValidateTextProperty();
        }
        #endregion ValidationRules
    }
}
