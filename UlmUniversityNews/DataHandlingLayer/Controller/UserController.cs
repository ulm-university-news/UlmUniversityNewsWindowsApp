using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller
{
    public class UserController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse UserDatabaseManager.
        /// </summary>
        UserDatabaseManager userDBManager;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse UserController.
        /// </summary>
        public UserController()
            : base()
        {
            userDBManager = new UserDatabaseManager();
        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse UserController.
        /// </summary>
        /// <param name="errorReport">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public UserController(IValidationErrorReport errorReport)
            : base (errorReport)
        {
            userDBManager = new UserDatabaseManager();
        }

        /// <summary>
        /// Speichert einen Datensatz einer Nutzer-Ressource lokal ab.
        /// </summary>
        /// <param name="user">Der zu speichernde Datensatz in Form eines Nutzer Objekts.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public void StoreUserLocally(User user)
        {
            try
            {
                userDBManager.StoreUser(user);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreUserLocally: Failed to store the user in local DB. Msg is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Prüft, ob für den Nutzer mit der übergebenen Id lokal bereits ein Datensatz vorhanden ist.
        /// </summary>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Liefert true, wenn Datensatz gespeichert, ansonsten false.</returns>
        public bool IsUserLocallyStored(int userId)
        {
            return userDBManager.IsUserStored(userId);
        }
    }
}
