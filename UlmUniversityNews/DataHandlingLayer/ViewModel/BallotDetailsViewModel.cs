﻿using DataHandlingLayer.DataModel;
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
using DataHandlingLayer.CommandRelays;

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
        private AsyncRelayCommand placeVotesCommand;
        /// <summary>
        /// Befehl zur Bestätigung der Auswahl. Es werden die entsprechenden
        /// Abstimmungsoptionen als gewählt/abgewählt markiert.
        /// </summary>
        public AsyncRelayCommand PlaceVotesCommand
        {
            get { return placeVotesCommand; }
            set { placeVotesCommand = value; }
        }

        private AsyncRelayCommand synchronizeBallotCommand;
        /// <summary>
        /// Befehl zur Synchronisation der gewählten Abstimmung mit dem Server.
        /// </summary>
        public AsyncRelayCommand SynchronizeBallotCommand
        {
            get { return synchronizeBallotCommand; }
            set { synchronizeBallotCommand = value; }
        }
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

            PlaceVotesCommand = new AsyncRelayCommand(
                param => executePlaceVotesCommand(),
                param => canPlaceVotes());
            SynchronizeBallotCommand = new AsyncRelayCommand(
                param => executeSynchronizeBallotCommand(),
                param => canSynchronizeBallot());
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

                        // Markiere die vom Nutzer gewählten Abstimmungsoptionen der Abstimmung.
                        List<Option> selectedOptions = await Task.Run(() =>
                            groupController.GetSelectedOptionsInBallot(SelectedBallot.Id, groupController.GetLocalUser().Id));

                        foreach (Option selectedOption in selectedOptions)
                        {
                            Option affectedOption = BallotOptionCollection.Where(item => item.Id == selectedOption.Id).FirstOrDefault<Option>();

                            if (affectedOption != null)
                            {
                                affectedOption.IsChosen = true;
                            }
                        }
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

            checkCommandExecution();
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

        #region CommandFunctionality
        /// <summary>
        /// Hilfsmethode um die Überprüfung der Ausführbarkeit der Befehle anzustoßen.
        /// </summary>
        private void checkCommandExecution()
        {
            PlaceVotesCommand.OnCanExecuteChanged();
            SynchronizeBallotCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Prüft, ob der Befehl PlaceVotesCommand zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canPlaceVotes()
        {
            // Pivot Item Index 0 -> Abstimmung
            if (AffectedGroup != null && 
                SelectedBallot != null && 
                !AffectedGroup.Deleted &&
                SelectedBallot.IsClosed.HasValue && SelectedBallot.IsClosed.Value != true && 
                BallotOptionCollection != null && BallotOptionCollection.Count > 0 && 
                SelectedPivotItemIndex == 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl PlaceVotesCommand aus. Speichert die gewählten bzw. abgewählten
        /// Abstimmungsoptionen ab.
        /// </summary>
        private async Task executePlaceVotesCommand()
        {
            try
            {
                displayIndeterminateProgressIndicator("BallotDetailsPlaceVotesStatus");

                foreach (Option option in BallotOptionCollection)
                {
                    bool successful = false;

                    if (option.IsChosen)
                    {
                        successful = await groupController.PlaceVoteAsync(AffectedGroup.Id, SelectedBallot.Id, option.Id);
                    }
                    else
                    {
                        successful = await groupController.RemoveVoteAsync(AffectedGroup.Id, SelectedBallot.Id, option.Id);
                    }

                    if (!successful)
                        Debug.WriteLine("executePlaceVotesCommand: No action taken for option with id {0}.", option.Id);
                }

                // Anzeige der Ergebnisse aktualisieren.
                await LoadBallotOptionResultsAsync();

                hideIndeterminateProgressIndicator();
                displayStatusBarText("BallotDetailsVotesSavedStatus", 3.0f);
            }
            catch (ClientException ex)
            {
                hideIndeterminateProgressIndicator();
                Debug.WriteLine("executePlaceVotesCommand: Failed to place votes. Msg is {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Prüft, ob der Befehl SynchronizeBallotCommand zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeBallot()
        {
            if (AffectedGroup != null && SelectedBallot != null &&
                !AffectedGroup.Deleted && 
                SelectedBallot.IsClosed.HasValue && SelectedBallot.IsClosed.Value == false)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeBallotCommand aus. Stößt die Synchronisation
        /// der Abstimmungsdaten mit den Daten des Servers an.
        /// </summary>
        private async Task executeSynchronizeBallotCommand()
        {
            try
            {
                displayIndeterminateProgressIndicator("BallotDetailsSynchronizationStatus");

                // Perform synchronization.
                await Task.Run(() => groupController.SynchronizeBallotWithServerAsync(AffectedGroup.Id, SelectedBallot.Id));

                // Aktualisiere Anzeige, indem Daten neu geladen werden.
                await LoadBallotAsync(AffectedGroup.Id, SelectedBallot.Id);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeBallotCommand: Synchronization failed. Error code is {0}. " + 
                    "Msg is: {1}.", ex.ErrorCode, ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
        #endregion CommandFunctionality
    }
}