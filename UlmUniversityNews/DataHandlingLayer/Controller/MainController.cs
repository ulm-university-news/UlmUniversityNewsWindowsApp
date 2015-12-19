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
    /// <summary>
    /// Die MainController Klassen enthält Funktionalität, die von allen Controllern benötigt wird.
    /// Dazu gehört die Bereitstellung des lokalen Nutzerobjekts.
    /// </summary>
    public class MainController
    {

        /// <summary>
        /// Konstruktor zur Initialisierung des MainController.
        /// </summary>
        protected MainController()
        {

        }

        /// <summary>
        /// Gibt den lokalen Nutzer zurück. Liefert null zurück wenn kein lokaler
        /// Nutzer definiert ist. Da diese Methode in allen ViewModel Klassen benötigt wird
        /// ist sie in der abstrakten Basisklasse implementiert.
        /// </summary>
        /// <returns>Instanz der User Klasse, oder null wenn kein lokaler Nutzer definiert ist.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn beim Ermitteln des lokalen Nutzers ein Fehler aufgetreten ist.</exception>
        protected User getLocalUser()
        {
            Debug.WriteLine("Get the local user.");
            // Frage zuerst das lokale Nutzerobjekt aus dem Cache ab.
            User localUser = LocalUser.GetInstance().GetCachedLocalUserObject();
            if (localUser == null)
            {
                // Lokales Nutzerobjekt noch nicht im Cache. Frage es aus der DB ab.
                try
                {
                    LocalUserDatabaseManager localUserDB = new LocalUserDatabaseManager();
                    localUser = localUserDB.GetLocalUser();

                    // Speichere Objekt im Cache.
                    LocalUser.GetInstance().CacheLocalUserObject(localUser);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("Database exception occurred in GetLocalUser(). Message of exception is: " + ex.Message);
                    // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                    // TODO - hier Exception werfen?
                    //throw new ClientException(ErrorCodes.LocalDatabaseException, "Retrieval of local user account has failed.");
                }
            }

            return localUser;
        }
    }
}
