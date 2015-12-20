using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.NavigationService;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;
using DataHandlingLayer.ErrorMapperInterface;

namespace DataHandlingLayer.ViewModel
{
    public class StartPageViewModel : ViewModel
    {
        // Eine Referenz auf die Controller Klasse für das lokale Nutzerobjekt.
        private LocalUserController localUserController;

        #region Properties
        private string userName;

        public string Name
        {
            get { return userName; }
            set { this.setProperty(ref this.userName, value); }
        }  
        #endregion Properties

        #region Commands
        private AsyncRelayCommand createUserCommand;

        public AsyncRelayCommand CreateUserCommand
        {
            get { return createUserCommand; }
            set { createUserCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der StartPageViewModel Klasse.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        public StartPageViewModel(INavigationService navService, IErrorMapper errorMapper) 
            : base(navService, errorMapper)
        {
            localUserController = new LocalUserController(this);    // Liefere Referenz auf IValidationErrorReport mit.
            // Erstelle Commands
            CreateUserCommand = new AsyncRelayCommand(param => createLocalUser(), param => canCreateLocalUser());
        }

        /// <summary>
        /// Determines if a local user account can be created.
        /// </summary>
        /// <returns>Returns true if the local user account can be created.</returns>
        private bool canCreateLocalUser()
        {
            return true;
        }

        /// <summary>
        /// Creates a local user account.
        /// </summary>
        /// <returns></returns>
        private async Task createLocalUser()
        {
            try
            {
                Debug.WriteLine("In create local user method. The current userName is: " + userName);
                displayProgressBar();

                bool successful = await localUserController.CreateLocalUserAsync(userName);
                if(successful){
                    // Navigiere zum Homescreen.
                    _navService.Navigate("Homescreen");
                }
            }
            catch (ClientException e)
            {
                Debug.WriteLine("Exception occured in createLocalUser. Error code is: " + e.ErrorCode + ".");
                displayError(e.ErrorCode);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.StackTrace);
            }
            finally
            {
                hideProgressBar();
            }
        }
    }
}
