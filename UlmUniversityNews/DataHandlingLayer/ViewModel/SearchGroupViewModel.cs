using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.CommandRelays;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.Controller;

namespace DataHandlingLayer.ViewModel
{
    public class SearchGroupViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Gibt an, ob man aktuell nach Gruppen suchen kann.
        /// </summary>
        private bool searchGroupsEnabled;

        /// <summary>
        /// Eine Instanz der Klasse GroupController.
        /// </summary>
        private GroupController groupController;
        #endregion Fields

        #region Properties

        #region SearchParameterSettings
        private bool searchForNameEnabled;
        /// <summary>
        /// Gibt an, ob die Suche nach Gruppen anhand des Namens erfolgen soll, d.h. der
        /// Nutzer den entsprechenden Eintrag angewählt hat.
        /// </summary>
        public bool SearchForNameEnabled
        {
            get { return searchForNameEnabled; }
            set { this.setProperty(ref this.searchForNameEnabled, value); }
        }

        private bool searchForIdEnabled;
        /// <summary>
        /// Gibt an, ob die Suche nach Gruppen anhand der Id erfolgen soll, d.h. der
        /// Nutzer den entsprechenden Eintrag angewählt hat.
        /// </summary>
        public bool SearchForIdEnabled
        {
            get { return searchForIdEnabled; }
            set { this.setProperty(ref this.searchForIdEnabled, value); }
        }

        private bool workingGroupSelected;
        /// <summary>
        /// Gibt an, ob der Typ Arbeitsgruppe im Auswahlmenü angewählt ist.
        /// </summary>
        public bool WorkingGroupSelected
        {
            get { return workingGroupSelected; }
            set { this.setProperty(ref this.workingGroupSelected, value); }
        }

        private bool tutorialGroupSelected;
        /// <summary>
        /// Gibt an, ob der Typ Tutoriumsgruppe im Auswahlmenü angewählt ist.
        /// </summary>
        public bool TutorialGroupSelected
        {
            get { return tutorialGroupSelected; }
            set { this.setProperty(ref this.tutorialGroupSelected, value); }
        }
        #endregion SearchParameterSettings

        private string searchTerm;
        /// <summary>
        /// Der vom Nutzer eingegebene Suchbegriff.
        /// </summary>
        public string SearchTerm
        {
            get { return searchTerm; }
            set { this.setProperty(ref this.searchTerm, value); }
        }

        private bool hasEmptySearchResult;
        /// <summary>
        /// Gibt an, ob bei der Suche eine leere Ergebnismenge zurückgeliefert wurde.
        /// </summary>
        public bool HasEmptySearchResult
        {
            get { return hasEmptySearchResult; }
            set { this.setProperty(ref this.hasEmptySearchResult, value); }
        }
        
        private ObservableCollection<Group> groups;
        /// <summary>
        /// Die Gruppen, die bei der Suche als Ergebnis vom Server zurückgeliefert wurden.
        /// </summary>
        public ObservableCollection<Group> Groups
        {
            get { return groups; }
            set { this.setProperty(ref this.groups, value); }
        }

        #endregion Properties

        #region Commands
        private AsyncRelayCommand searchGroupsCommand;
        /// <summary>
        /// Befehl zur Suche nach Gruppen.
        /// </summary>
        public AsyncRelayCommand SearchGroupsCommand
        {
            get { return searchGroupsCommand; }
            set { searchGroupsCommand = value; }
        }

        private AsyncRelayCommand groupSelectedCommand;
        /// <summary>
        /// Befehl, der genutzt werden kann um eine Gruppe auszuwählen und sich die Details
        /// dieser Gruppe anzeigen zu lassen.
        /// </summary>
        public AsyncRelayCommand GroupSelectedCommand
        {
            get { return groupSelectedCommand; }
            set { groupSelectedCommand = value; }
        }
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse SearchGroupViewModel.
        /// </summary>
        /// <param name="navService"></param>
        /// <param name="errorMapper"></param>
        public SearchGroupViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            // Erzeuge Instanz von GroupController.
            groupController = new GroupController();

            // Setze initiale Parameter.
            WorkingGroupSelected = true;
            TutorialGroupSelected = true;
            HasEmptySearchResult = false;

            // Befehle erzeugen
            GroupSelectedCommand = new AsyncRelayCommand(param => executeGroupSelectedCommandAsync(param));
            SearchGroupsCommand = new AsyncRelayCommand(
                param => executeSearchGroupsCommandAsync(),
                param => canSearchGroups()
                );
        }

        /// <summary>
        /// Aktualisiert die Collection mit Group Instanzen. Die Collection
        /// wird komplett neu geladen. Die übergebenen Elemente werden hinzugefügt.
        /// Dadurch wird die Anzeige aktualisiert.
        /// </summary>
        /// <param name="newGroups">Die anzuzeigenden Group Instanzen.</param>
        private void updateGroupsCollection(List<Group> newGroups)
        {
            if (Groups == null)
            {
                Groups = new ObservableCollection<Group>();
            }
            Groups.Clear();
            if (newGroups != null && newGroups.Count > 0)
            {
                // Füge zur Collection hinzu.
                foreach (Group group in newGroups)
                {
                    Groups.Add(group);
                }
            }
        }

        #region CommandFunctionality
        ///// <summary>
        ///// Hilfsmethode, welche die Prüfung der Ausführbarkeit von Befehlen anstößt.
        ///// </summary>
        //private void checkCommandExecution()
        //{
        //    if (searchGroupsEnabled && SearchTerm.Trim().Length == 0)
        //    {
        //        SearchGroupsCommand.OnCanExecuteChanged();
        //    }
        //    else if (!searchGroupsEnabled && SearchTerm.Trim().Length > 0)
        //    {
        //        SearchGroupsCommand.OnCanExecuteChanged();
        //    }
        //}

        /// <summary>
        /// Gibt an, ob der Befehl SearchGroupsCommand bezüglich des
        /// aktuellen Zustands zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSearchGroups()
        {
            return true;
        }

        /// <summary>
        /// Ausführung des Befehls SearchGroupsCommand. Stößt Suche nach Gruppen 
        /// auf dem Server an.
        /// </summary>
        private async Task executeSearchGroupsCommandAsync()
        {
            // Setze Parameter zurück.
            HasEmptySearchResult = false;

            try
            {
                displayIndeterminateProgressIndicator("SearchGroupStatus");

                if (SearchForNameEnabled)
                {
                    // Typ wird nur gesetzt, wenn einer von beiden aktiv ist.
                    GroupType? type = null;     // Suchparameter Gruppentyp.
                    if (WorkingGroupSelected && !TutorialGroupSelected)
                    {
                        type = GroupType.WORKING;
                    }
                    else if (!WorkingGroupSelected && TutorialGroupSelected)
                    {
                        type = GroupType.TUTORIAL;
                    }

                    // Starte online Suche.
                    List<Group> retrievedGroups = await groupController.SearchGroupsAsync(
                        SearchTerm,
                        type
                        );

                    // Aktualisiere Anzeige.
                    updateGroupsCollection(retrievedGroups);
                }
                else if (SearchForIdEnabled)
                {
                    int id;
                    bool isInteger = int.TryParse(SearchTerm, out id);
                    Group retrievedGroup = null;

                    if (isInteger)
                    {
                        // Suche nach Id.
                         retrievedGroup = await groupController.GetGroupAsync(id, false);
                    }

                    // Aktualisiere Anzeige.
                    List<Group> groups = new List<Group>();
                    groups.Add(retrievedGroup);
                    updateGroupsCollection(groups);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Errors occurred during search requests.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }

            // Prüfe, ob das Suchergebnis leer ist.
            if (Groups != null && Groups.Count == 0)
            {
                HasEmptySearchResult = true;
            }
        }

        /// <summary>
        /// Ausführung des Befehls GroupSelectedCommand. Stößt die Anzeige
        /// der Details der gewählten Gruppe an.
        /// <param name="selectedItem">Die angeklickte Gruppe.</param>
        /// </summary>
        private async Task executeGroupSelectedCommandAsync(object selectedItem)
        {
            Debug.WriteLine("The selected item is: " + (selectedItem as Group).Name);

            // Cast object to group.
            Group selectedGroup = selectedItem as Group;

            string key = "testKey";
            // Store to temporary cache.
            await Common.TemporaryCacheManager.StoreObjectInTmpCacheAsync(key, selectedGroup);

            // Stoße Navigation an.
            if (_navService != null)
                _navService.Navigate("GroupDetails", key);
        }
        #endregion CommandFunctionality
    }
}
