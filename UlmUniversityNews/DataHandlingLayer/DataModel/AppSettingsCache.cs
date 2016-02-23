using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Singleton-Klasse AppSettingsCache wird als Cache für eine Instanz der Klasse AppSettings verwendet. Diese Instanz der Klasse AppSettings
    /// representiert die aktuellen Anwendungseinstellungen.
    /// </summary>
    public class AppSettingsCache
    {
        // Lokale Referenz auf die Instanz der Singleton Klasse AppSettingsCache.
        private static AppSettingsCache _instance;

        // Speichert die Referenz auf das AppSettings Objekt, welches im Speicher bereitgehalten werden soll.
        private AppSettings appSettings;

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AppSettingsCache.
        /// </summary>
        private AppSettingsCache()
        {

        }

        /// <summary>
        /// Gibt eine Instanz der Singleton-Klasse LocalUser zurück.
        /// </summary>
        /// <returns>Instanz von LocalUser.</returns>
        public static AppSettingsCache GetInstance()
        {
            lock (typeof(AppSettingsCache))
            {
                if (_instance == null)
                {
                    _instance = new AppSettingsCache();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Speichert die Referenz auf das Objekt mit den Anwendungseinstellungen im Speicherbereich der Singleton-Klasse AppSettingsCache,
        /// so dass es zur Laufzeit der Anwendung aus diesem Speicher abgefragt werden kann.
        /// </summary>
        /// <param name="appSettings">Das Objekt der Klasse AppSettings, welches gespeichert werden soll.</param>
        public void CacheApplicationSettings(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        /// <summary>
        /// Frage das Objekt mit den Anwendungseinstellungen aus dem Speicherbereich der Singleton-Klasse AppSettingsCache ab.
        /// </summary>
        /// <returns>Eine Instanz der Klasse AppSettings. Liefert null, wenn keine Instanz aktuell im Speicher gehalten wird.</returns>
        public AppSettings GetCachedApplicationSettings()
        {
            return this.appSettings;
        }
    }
}
