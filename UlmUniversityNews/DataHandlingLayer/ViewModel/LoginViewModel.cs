using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Controller;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class LoginViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des LoginController.
        /// </summary>
        private LoginController loginController;
        #endregion Fields

        #region Properties
        private string username;
        /// <summary>
        /// Der Nutzername, der vom Nutzer der Anwendung in das Textfeld geschrieben wurde.
        /// </summary>
        public string Username
        {
            get { return username; }
            set { this.setProperty(ref this.username, value); }
        }

        private string password;
        /// <summary>
        /// Das Passwort, das vom Nutzer der Anwendung in das Passwort-Feld geschrieben wurde.
        /// </summary>
        public string Password
        {
            get { return password; }
            set { this.setProperty(ref this.password, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand loginCommand;
        /// <summary>
        /// Befehl zum Auslösen des Log-In Vorgangs.
        /// </summary>
        public AsyncRelayCommand LoginCommand
        {
            get { return loginCommand; }
            set { loginCommand = value; }
        }      
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse LoginViewModel.
        /// </summary>
        /// <param name="navService">Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Referenz auf den Fehlerdienst der Anwendung.</param>
        public LoginViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            loginController = new LoginController(this);

            LoginCommand = new AsyncRelayCommand(param => executeLoginCommand());
        }

        /// <summary>
        /// Wird ausgeführt, wenn der Login Vorgang durch das Feuern des Befehls "LoginCommand" angestoßen wurde.
        /// </summary>
        private async Task executeLoginCommand()
        {
            Debug.WriteLine("Start login process.");
            try
            {
                displayIndeterminateProgressIndicator();
                bool successful = await loginController.PerformLoginAsync(Username, Password);
                
                if(successful)
                {
                    // Navigation zur Moderator View
                    _navService.Navigate("HomescreenModerator");

                    // Clear Back Stack. Nutzer soll nicht durch Back-Taste auf die Login Seite zurück kommen.
                    _navService.ClearBackStack();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during login process.");
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
    }
}
