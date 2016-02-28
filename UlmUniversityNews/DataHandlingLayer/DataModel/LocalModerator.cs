using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse LocalModerator ist eine Singleton-Klasse, die ein Objekt der Klasse Moderator kapselt und 
    /// somit anderen Klassen zur Verfügung stellt. Ein eingeloggter Moderator wird nicht in der Datenbank abgelegt, sondern
    /// nur im lokalen Speicher gehalten. Diese Speicherung und Bereitstellung übernimmt LocalModerator.
    /// </summary>
    public class LocalModerator
    {
        // Lokale Referenz auf die Singleton Klasse LocalModerator.
        private static LocalModerator _instance;

        // Referenz auf das Moderatorenobjekt, welches durch LocalModerator gekapselt werden soll.
        private Moderator moderator;

        /// <summary>
        /// Erzeugt eine Instanz der Klasse LocalModerator.
        /// </summary>
        private LocalModerator()
        {

        }

        /// <summary>
        /// Gibt eine Instanz der Singleton-Klasse LocalModerator zurück.
        /// </summary>
        /// <returns>Instanz von LocalModerator.</returns>
        public static LocalModerator GetInstance()
        {
            lock (typeof(LocalModerator))
            {
                if (_instance == null)
                {
                    _instance = new LocalModerator();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Fügt das übergebene Moderatorenobjekt dem durch die Singleton Klasse repräsentierten 
        /// Cache hinzu.
        /// </summary>
        /// <param name="moderator">Das zu speichernde Moderatorenobjekt.</param>
        public void CacheModeratorObject(Moderator moderator)
        {
            this.moderator = moderator;
        }

        /// <summary>
        /// Gibt das von der Singleton-Klasse gehaltene Moderatorenobjekt zurück.
        /// </summary>
        /// <returns>Das gespeicherte Moderatorenobjekt.</returns>
        public Moderator GetCachedModerator()
        {
            return this.moderator;
        }
    }
}
