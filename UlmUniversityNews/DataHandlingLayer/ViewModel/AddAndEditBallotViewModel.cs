using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataHandlingLayer.DataModel;

namespace DataHandlingLayer.ViewModel
{
    public class AddAndEditBallotViewModel : DialogBaseViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        private bool isAddDialog;
        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zum Hinzufügen einer neuen Abstimmung handelt.
        /// </summary>
        public bool IsAddDialog
        {
            get { return isAddDialog; }
            set { this.setProperty(ref this.isAddDialog, value); }
        }

        private bool isEditDialog;
        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zum Bearbeiten einer bestehenden Abstimmung handelt.
        /// </summary>
        public bool IsEditDialog
        {
            get { return isEditDialog; }
            set { this.setProperty(ref this.isEditDialog, value); }
        }

        private ObservableCollection<Option> ballotOptionsCollection;
        /// <summary>
        /// Liste von Abstimmungsoptionen, die für diese Abstimmung angelegt wurden.
        /// </summary>
        public ObservableCollection<Option> BallotOptionsCollection
        {
            get { return ballotOptionsCollection; }
            set { this.setProperty(ref this.ballotOptionsCollection, value); }
        }
        #endregion Properties

        #region Commands
        #endregion Commands 

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAndEditBallotViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditBallotViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {

        }

        /// <summary>
        /// Lädt Dialog zum Erstellen einer neuen Abstimmung.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, für die eine neue Abstimmung erstellt werden soll.</param>
        public async Task LoadCreateBallotDialogAsync(int groupId)
        {
            IsAddDialog = true;
            IsEditDialog = false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="groupId"></param>
        /// <param name="ballotId"></param>
        /// <returns></returns>
        public async Task LoadEditBallotDialogAsync(int groupId, int ballotId)
        {
            IsAddDialog = false;
            IsEditDialog = true;
        }

    }
}
