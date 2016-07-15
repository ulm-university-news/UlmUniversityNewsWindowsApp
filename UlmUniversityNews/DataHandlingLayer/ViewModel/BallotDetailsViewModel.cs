using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.Controller;

namespace DataHandlingLayer.ViewModel
{
    public class BallotDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz einer Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties
        private int selectedPivotIndex;
        /// <summary>
        /// Gibt den Index des Pivotelements an, das gerade aktiv ist.
        /// </summary>
        public int SelectedPivotItemIndex
        {
            get { return selectedPivotIndex; }
            set { selectedPivotIndex = value; }
        }

        private Group affectedGroup;
        /// <summary>
        /// Die Gruppe, zu der die angezeigte Abstimmung gehört.
        /// </summary>
        public Group AffectedGroup
        {
            get { return affectedGroup; }
            set { this.setProperty(ref this.affectedGroup, value); }
        }

        private Ballot selectedBallot;
        /// <summary>
        /// Die gewählte Abstimmung zu der die Details angezeigt werden.
        /// </summary>
        public Ballot SelectedBallot
        {
            get { return selectedBallot; }
            set { this.setProperty(ref this.selectedBallot, value); }
        }

        private ObservableCollection<Option> ballotOptionCollection;
        /// <summary>
        /// Enthält alle Abstimmungsoptionen der gewählten Abstimmung.
        /// </summary>
        public ObservableCollection<Option> BallotOptionCollection
        {
            get { return ballotOptionCollection; }
            set { this.setProperty(ref this.ballotOptionCollection, value); }
        }

        private ObservableCollection<VoteResult> voteResultsCollection;
        /// <summary>
        /// Enthält die Ergebnisse der Abstimmung zu jeder Abstimmungsoption.
        /// </summary>
        public ObservableCollection<VoteResult> VoteResultsCollection
        {
            get { return voteResultsCollection; }
            set { this.setProperty(ref this.voteResultsCollection, value); }
        }
        #endregion Properties

        #region Commands
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse BallotDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public BallotDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            groupController = new GroupController(this);

            if (BallotOptionCollection == null)
                BallotOptionCollection = new ObservableCollection<Option>();

            if (VoteResultsCollection == null)
                VoteResultsCollection = new ObservableCollection<VoteResult>();
        }

        /// <summary>
        /// Lädt die Abstimmung aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Abstimmung gehört.</param>
        /// <param name="ballotId">Die Id der Abstimmung, die geladen werden soll.</param>
        public async Task LoadBallotAsync(int groupId, int ballotId)
        {
            try
            {
                AffectedGroup = await Task.Run(() => groupController.GetGroup(groupId));
                SelectedBallot = await Task.Run(() => groupController.GetBallot(ballotId, true));

                if (SelectedBallot != null)
                {
                    if (SelectedBallot.Options != null)
                    {
                        BallotOptionCollection = new ObservableCollection<Option>(SelectedBallot.Options);

                        // TODO - Mark the options that the local user has voted for.
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadBallotAsync: Loading failed. Message is {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }

            // Lade die Abstimmungsergebnisse.
            await LoadBallotOptionResultsAsync();
        }

        /// <summary>
        /// Lädt die Abstimmungsergebnisse der gewählten Abstimmung und bereitet deren Anzeige vor. 
        /// </summary>
        public async Task LoadBallotOptionResultsAsync()
        {
            if (SelectedBallot == null || AffectedGroup == null)
                return;

            List<VoteResult> voteResults = new List<VoteResult>();
            try
            {
                List<Option> options = await Task.Run(() => groupController.GetOptions(SelectedBallot.Id, true));
                Dictionary<int, User> groupParticipants = await Task.Run(() => groupController.GetParticipantsLookupDirectory(AffectedGroup.Id));

                int totalAmountOfVotes = 0;
                foreach (Option option in options)
                {
                    VoteResult result = new VoteResult()
                    {
                        OptionId = option.Id,
                        OptionText = option.Text,
                        IsLastVoteResultInList = false
                    };

                    if (SelectedBallot.HasPublicVotes.HasValue)
                    {
                        result.IsPublic = SelectedBallot.HasPublicVotes.Value;
                    }

                    if (option.VoterIds != null)
                    {
                        // Setze Anzahl Stimmen.
                        result.VoteCount = option.VoterIds.Count;

                        // Addiere Anzahl Stimmen auf Gesamtzahl auf.
                        totalAmountOfVotes += option.VoterIds.Count;

                        if (result.IsPublic)
                        {
                            List<User> voters = new List<User>();

                            foreach (var voter in option.VoterIds)
                            {
                                if (groupParticipants.ContainsKey(voter))
                                {
                                    voters.Add(groupParticipants[voter]);
                                }
                                else
                                {
                                    User dummy = new User()
                                    {
                                        Name = "Unknown user"
                                    };
                                    voters.Add(dummy);
                                }
                            }

                            // Generiere String mit Nutzern, die abgestimmt haben.
                            result.GenerateVoterNamesString(voters);
                        }
                    }

                    voteResults.Add(result);
                }

                // Berechne Abstimmungsergebnis.
                foreach (VoteResult result in voteResults)
                {
                    result.CalculateVoteResultInPercentage(totalAmountOfVotes);
                }

                // Lade Collection.
                VoteResultsCollection = new ObservableCollection<VoteResult>(voteResults);

                if (VoteResultsCollection.Count > 0)
                    VoteResultsCollection.Last<VoteResult>().IsLastVoteResultInList = true;
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("LoadBallotOptionResultsAsync: Loading failed. Message is {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
        }
    }
}
