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

        /// <summary>
        /// Speicherung der Nutzer in den lokalen Datensätzen.
        /// </summary>
        /// <param name="users">Die zu speichernden Nutzer.</param>
        /// <exception cref="ClientException">Wenn die Speicherung fehlschlägt.</exception>
        public void StoreUsersLocally(List<User> users)
        {
            List<User> usersToStore = new List<User>();

            try
            {
                foreach (User user in users)
                {
                    if (!userDBManager.IsUserStored(user.Id))
                        usersToStore.Add(user);
                }

                userDBManager.BulkInsertUsers(usersToStore);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("StoreUsersLocally: Problem occurred during storing process.");
                Debug.WriteLine("Message is: {0}.", ex.Message);

                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Arbeitet eine Liste an Nutzerressourcen ab. Prüft, ob die Datensätze neu
        /// zu den lokalen Datensätzen hinzugenommen werden müssen, oder ob bestehende Datensätze
        /// aktualisiert werden müssen. Führt dann die entsprechenden Einfüge oder Aktualisierungsoperationen
        /// aus.
        /// </summary>
        /// <param name="users">Die Menge an Nutzerdatensätzen, die geprüft und bearbeitet werden soll</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung oder Aktualisierung fehlschlägt.</exception>
        public void AddOrUpdateUsers(List<User> users)
        {
            List<User> usersToStore = new List<User>();
            List<User> usersToUpdate = new List<User>();

            try
            {
                foreach (User user in users)
                {
                    if (!userDBManager.IsUserStored(user.Id))
                        usersToStore.Add(user);
                    else
                        usersToUpdate.Add(user);
                }

                userDBManager.BulkInsertUsers(usersToStore);
                userDBManager.UpdateUsers(usersToUpdate);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("AddOrUpdateUsers: Problem occurred during storing or updating process.");
                Debug.WriteLine("Message is: {0}.", ex.Message);

                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }

        /// <summary>
        /// Liefert das Nutzerobjekt zum Nutzer, der durch die angegebene Id identifiziert wird.
        /// </summary>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Ein Objekt vom Typ User.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Abruf fehlschlägt.</exception>
        public User GetUser(int userId)
        {
            User user = null;

            try
            {
                user = userDBManager.GetUser(userId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("GetUser: Failed to retrieve user object with id {0}.", userId);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return user;
        }
    }
}
