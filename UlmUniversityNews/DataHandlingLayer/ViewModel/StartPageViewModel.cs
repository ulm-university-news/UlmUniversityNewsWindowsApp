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
        /// <summary>
        /// Der vom Nutzer eingegebene Nutzername.
        /// </summary>
        public string Name
        {
            get { return userName; }
            set { this.setProperty(ref this.userName, value); }
        }

        private bool areTermsAndConditionsAccepted;
        /// <summary>
        /// Gibt an, ob die Nutzungsbedingungen aktuell als akzeptiert markiert sind.
        /// </summary>
        public bool AreTermsAndConditionsAccepted
        {
            get { return areTermsAndConditionsAccepted; }
            set 
            { 
                this.setProperty(ref this.areTermsAndConditionsAccepted, value);
                checkCommandExecution();
            }
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
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public StartPageViewModel(INavigationService navService, IErrorMapper errorMapper) 
            : base(navService, errorMapper)
        {
            localUserController = new LocalUserController(this);    // Liefere Referenz auf IValidationErrorReport mit.

            // Erstelle Befehle.
            CreateUserCommand = new AsyncRelayCommand(param => createLocalUser(), param => canCreateLocalUser());
        }

        /// <summary>
        /// Hilfsmethode, welche die Ausführbarkeit von den angebotenen Befehlen prüft.
        /// </summary>
        private void checkCommandExecution()
        {
            CreateUserCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Gibt an, ob der Befehl zum Anlegen eines neuen Nutzerkontos aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht.</returns>
        private bool canCreateLocalUser()
        {
            if (AreTermsAndConditionsAccepted)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Erzeugt einen lokalen Nutzeraccount.
        /// </summary>
        private async Task createLocalUser()
        {
            try
            {
                Debug.WriteLine("In create local user method. The current userName is: " + userName);
                displayIndeterminateProgressIndicator();

                bool successful = await localUserController.CreateLocalUserAsync(userName);
                if(successful){
                    // Navigiere zum Homescreen.
                    _navService.Navigate("Homescreen");
                    // Wurde man auf den Homescreen navigiert von der Startseite aus, so soll man nicht mehr zur Startseite zurückkehren können.
                    _navService.RemoveEntryFromBackStack();
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
                hideIndeterminateProgressIndicator();
            }
        }
    }
}
