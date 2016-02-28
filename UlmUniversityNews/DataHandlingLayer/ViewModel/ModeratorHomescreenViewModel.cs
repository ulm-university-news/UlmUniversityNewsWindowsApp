using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class ModeratorHomescreenViewModel : ViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        #endregion Properties

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorHomescreenViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ModeratorHomescreenViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {

        }
    }
}
