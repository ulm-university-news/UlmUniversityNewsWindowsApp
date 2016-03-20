using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class AboutUniversityNewsViewModel : ViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        #endregion Properties

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AboutUniversityNewsViewModel.
        /// </summary>
        /// <param name="navService"></param>
        /// <param name="errorMapper"></param>
        public AboutUniversityNewsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {

        }
    }
}
