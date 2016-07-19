using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    public class VoteResult : PropertyChangedNotifier
    {
        #region Properties
        private int optionId;
        /// <summary>
        /// Die Id der Abstimmungsoption, für die das Ergebnis abgebildet wird.
        /// </summary>
        public int OptionId
        {
            get { return optionId; }
            set { this.setProperty(ref this.optionId, value); }
        }

        private string optionText;
        /// <summary>
        /// Der Text der eigentlichen Abstimmungsoption, für die das Ergebnis gilt.
        /// </summary>
        public string OptionText
        {
            get { return optionText; }
            set { this.setProperty(ref this.optionText, value); }
        }

        private int voteCount;
        /// <summary>
        /// Die Anzahl an Stimmen, welche die repräsentierte Abstimmungsoption erhalten hat.
        /// </summary>
        public int VoteCount
        {
            get { return voteCount; }
            set { this.setProperty(ref this.voteCount, value); }
        }

        private float voteResultPercentage;
        /// <summary>
        /// Gibt das Abstimmungsergebnis für diese Abstimmungsoption in Prozent an.
        /// 100 % entspricht dabei der Anzahl aller Stimmen, die in der Abstimmung abgegeben wurden.
        /// </summary>
        public float VoteResultPercentage
        {
            get { return voteResultPercentage; }
            set { this.setProperty(ref this.voteResultPercentage, value); }
        }

        private bool isPublic;
        /// <summary>
        /// Gibt an, ob das Ergebnis öffentlich ist. Bei einem öffentlichen Ergebnis
        /// werden die Namen der Nutzer angezeigt, die für die Option gestimmt haben.
        /// </summary>
        public bool IsPublic
        {
            get { return isPublic; }
            set { this.setProperty(ref this.isPublic, value); }
        }

        private string voterNames;
        /// <summary>
        /// Die Namen der Nutzer, die für die Abstimmungsoption gestimmt haben, als String.
        /// </summary>
        public string VoterNames
        {
            get { return voterNames; }
            set { this.setProperty(ref this.voterNames, value); }
        }

        private bool isLastVoteResultInList;
        /// <summary>
        /// Gibt an, ob das Element das letzte der Liste von Abstimmungsergebnissen ist.
        /// Property ist rein zu Anzeigezwecken!
        /// </summary>
        public bool IsLastVoteResultInList
        {
            get { return isLastVoteResultInList; }
            set { this.setProperty(ref this.isLastVoteResultInList, value); }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse VoteResult.
        /// </summary>
        public VoteResult()
        {

        }

        /// <summary>
        /// Erzeugt die Zeichenkette VoterNames und setzt die Eigenschaft.
        /// </summary>
        /// <param name="voters">Die Menge an Nutzern, aus denen die Zeichenkette erstellt werden soll.</param>
        public void GenerateVoterNamesString(List<User> voters)
        {
            StringBuilder sb = new StringBuilder();
            foreach (User voter in voters)
            {
                sb.Append(voter.Name + " - ");
            }
            // Entferne letztes '-' Zeichen.
            if (sb.Length > 2)
            {
                sb.Remove(sb.Length - 2, 2);
            }            

            VoterNames = sb.ToString();
        }

        /// <summary>
        /// Berechnet das Ergebnis für diese Abstimmungsoption in Prozent. 
        /// Nutzt die Anzahl an Stimmen für diese Abstimmungsoption (VoteCount Eigenschaft)
        /// und die Anzahl aller Stimmen, die für diese Abstimmung abgegeben wurden.
        /// </summary>
        /// <param name="totalVotes">Die Gesamtmenge an Stimmen, die für die Abstimmung abgegeben wurden.</param>
        public void CalculateVoteResultInPercentage(int totalVotes)
        {
            if (totalVotes <= 0)
            {
                VoteResultPercentage = 0.0f;
            }
            else
            {
                VoteResultPercentage = ((float)VoteCount) / ((float)totalVotes);
            }

            System.Diagnostics.Debug.WriteLine("CalculateVoteResultInPercentage: Result is {0}.", VoteResultPercentage);
        }
    }
}
