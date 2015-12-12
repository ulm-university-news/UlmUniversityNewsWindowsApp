using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Singleton-Klasse LocalUser wird als Cache für eine Instanz der Klasse User verwendet. Diese Instanz der Klasse User
    /// representiert den aktuellen lokalen Nutzer der Anwendung, d.h. das lokale Nutzerobjekt. Da das lokale Nutzerobjekt sehr oft in der
    /// Anwendung benötigt wird, insbesondere in der API (wegen des darin enthaltenen ServerAccessToken), wird das Objekt über diese
    /// Klasse zugreifbar gemacht.
    /// </summary>
    public class LocalUser
    {

        // Lokale Referenz auf die Instanz der Singleton Klasse LocalUser.
        private static LocalUser _instance = null;

        // Speichert die Referenz auf das User Objekt, welches im Speicher bereitgehalten werden soll.
        private User localUser;

        /// <summary>
        /// Erstellt eine Instanz der Klasse LocalUser.
        /// </summary>
        private LocalUser()
        {

        }

        /// <summary>
        /// Gibt eine Instanz der Singleton-Klasse LocalUser zurück.
        /// </summary>
        /// <returns>Instanz von LocalUser.</returns>
        public static LocalUser GetInstance()
        {
            lock (typeof(LocalUser)){
                if(_instance == null){
                    _instance = new LocalUser();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Speichert die Referenz auf das lokale Nutzerobjekt im Speicherbereich der Singleton-Klasse LocalUser,
        /// so dass es zur Laufzeit der Anwendung aus diesem Speicher abgefragt werden kann.
        /// </summary>
        /// <param name="localUser">Das Objekt der Klasse User, welches gespeichert werden soll.</param>
        public void CacheLocalUserObject(User localUser)
        {
            this.localUser = localUser;
        }

        /// <summary>
        /// Frage das lokale Nutzerobjekt aus dem Speicherbereich der Singleton-Klasse LocalUser ab.
        /// </summary>
        /// <returns>Eine Instanz der Klasse User. Liefert null, wenn keine Instanz aktuell im Speicher gehalten wird.</returns>
        public User GetCachedLocalUserObject()
        {
            return localUser;
        }
    }
}
