using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class LoginViewModel : ViewModel
    {
        #region Fields
        #endregion Fields

        #region Properties
        #endregion Properties

        #region Commands
        #endregion Commands

        public LoginViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {

        }
    }
}
