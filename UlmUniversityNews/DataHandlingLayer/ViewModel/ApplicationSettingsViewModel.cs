using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class ApplicationSettingsViewModel : ViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        private int selectedPivotElementIndex;
        /// <summary>
        /// Gibt den Index des Pivot-Elements an, das gerade aktiv ist.
        /// </summary>
        public int SelectedPivotItemIndex
        {
            get { return selectedPivotElementIndex; }
            set { selectedPivotElementIndex = value; }
        }

        private bool isGermanLanguageSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Sprache Deutsch gewählt ist.
        /// </summary>
        public bool IsGermanLanguageSelected
        {
            get { return isGermanLanguageSelected; }
            set { isGermanLanguageSelected = value; }
        }

        private bool isEnglishLanugageSelected;
        /// <summary>
        /// Gibt an, ob aktuell die Sprache Englisch gewählt ist.
        /// </summary>
        public bool IsEnglishLanguageSelected
        {
            get { return isEnglishLanugageSelected; }
            set { isEnglishLanugageSelected = value; }
        }  
        #endregion Properties

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ApplicationSettingsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {

        }

    }
}
