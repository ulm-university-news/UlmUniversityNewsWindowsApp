using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller
{
    public class ApplicationSettingsController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse LocalUserController.
        /// </summary>
        LocalUserController localUserController;

        /// <summary>
        /// Eine Referenz auf den ApplicationSettingsDatabaseManager.
        /// </summary>
        ApplicationSettingsDatabaseManager applicationSettingsDatabaseManager;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsController.
        /// </summary>
        public ApplicationSettingsController()
            : base()
        {
            localUserController = new LocalUserController();
            applicationSettingsDatabaseManager = new ApplicationSettingsDatabaseManager();

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsController.
        /// </summary>
        /// <param name="validationErrorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public ApplicationSettingsController(IValidationErrorReport validationErrorReporter)
            : base(validationErrorReporter)
        {
            localUserController = new LocalUserController();
            applicationSettingsDatabaseManager = new ApplicationSettingsDatabaseManager();
        }

        /// <summary>
        /// Gibt den aktuellen lokalen Nutzer zurück.
        /// </summary>
        /// <returns>Ein Objekt vom Typ User.</returns>
        public User GetCurrentLocalUser()
        {
            return base.getLocalUser();
        }

        /// <summary>
        /// Aktualisiert den Nutzernamen des lokalen Nutzers der Anwendung, falls eine Änderung
        /// gegenüber dem aktuellen Nutzernamen vorliegt. Die Aktualisierung erfolgt sowohl lokal, 
        /// als auch auf dem REST Server.
        /// </summary>
        /// <param name="username">Der neue Nutzername des lokalen Nutzers.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Aktualisierung nicht durchgeführt werden konnte.</exception>
        public async Task<bool> UpdateLocalUsernameAsync(string username)
        {
            User currentLocalUser = base.getLocalUser();

            // Prüfe, ob der Name aktualisiert werden muss.
            if(String.Compare(currentLocalUser.Name, username) != 0)
            {
                // Setze neuen Namen in ein Nutzer Objekt und führe Eingabevalidierung durch.
                User tmpLocalUser = new User();
                tmpLocalUser.Name = username;

                tmpLocalUser.ValidateNameProperty();
                if(tmpLocalUser.HasValidationError("Name"))
                {
                    // Melde Validationsfehler und breche ab.
                    base.reportValidationErrors(tmpLocalUser.GetValidationErrors());
                    return false;
                }
                else
                {
                    tmpLocalUser.ClearValidationErrors();
                    base.clearValidationErrorForProperty("Name");
                }

                // Verwende Funktionalität im LocalUserController, um die Aktualisierung des Namens durchzuführen.
                await localUserController.UpdateLocalUserAsync(username, null);
            }
            return true;
        }

        /// <summary>
        /// Aktualisiert die Benachrichtigungseinstellungen für die Anwendung.
        /// Diese Einstellungen bezüglich der Ankündigung von Nachrichten gelten grundsätzlich 
        /// für die gesamte Anwendung. Es können jedoch für einzelne Kanäle und Gruppen spezifische 
        /// Einstellungen vorgenommen werden, welche dann diese Einstellungen für die jeweilige Ressource überschreiben. 
        /// </summary>
        /// <param name="newSetting">Die neu gewählte Option bezüglich Nachrichtenankündigung.</param>
        public void UpdateNotificationSettings(NotificationSetting newSetting)
        {
            // Hole die aktuellen Anwendungseinstellungen.
            AppSettings appSettings = GetApplicationSettings();

            // Führe Aktualisierung aus.
            appSettings.MsgNotificationSetting = newSetting;

            try
            {
                Debug.WriteLine("Update notification settings. Set to new value {0}.", newSetting.ToString());
                applicationSettingsDatabaseManager.UpdateApplicationSettings(appSettings);
            }
            catch(DatabaseException ex)
            {
                Debug.WriteLine("An error occurred during the notification settings update. The error message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            // Lege neues AppSettings Objekt in den Cache.
            AppSettingsCache.GetInstance().CacheApplicationSettings(appSettings);
        }

        /// <summary>
        /// Aktualisiert die Anordnungseinstellungen bezüglich Auflistungen in der Anwendung.
        /// </summary>
        /// <param name="generalListSettings">Angabe, ob Listeninhalt grundsätzlich aufsteigend oder absteigend nach Ordnungskriterium sortiert werden soll.</param>
        /// <param name="announcementOrderSetting">Angabe, ob Nachrichten von unten nach oben oder von oben nach unten augelistet werden sollen.</param>
        /// <param name="channelOrderSettings">Ordnungskriterium für Auflistungen von Kanälen.</param>
        /// <param name="groupOrderSettings">Ordnungskriterium für Auflistungen von Gruppen.</param>
        /// <param name="conversationOrderSettings">Ordnungskriterium für Auflistungen von Konversationen.</param>
        /// <param name="ballotOrderSettings">Ordnungskriteriumg für Auflistungen von Abstimmungen.</param>
        public void UpdateListSettings(OrderOption generalListSettings, OrderOption announcementOrderSetting, OrderOption channelOrderSettings, 
            OrderOption groupOrderSettings, OrderOption conversationOrderSettings, OrderOption ballotOrderSettings)
        {
            // Hole die aktuellen Anwendungseinstellungen.
            AppSettings appSettings = GetApplicationSettings();

            appSettings.GeneralListOrderSetting = generalListSettings;
            appSettings.AnnouncementOrderSetting = announcementOrderSetting;
            appSettings.ChannelOderSetting = channelOrderSettings;
            appSettings.GroupOrderSetting = groupOrderSettings;
            appSettings.ConversationOrderSetting = conversationOrderSettings;
            appSettings.BallotOrderSetting = ballotOrderSettings;

            try
            {
                Debug.WriteLine("Update list settings. New values are {0}, {1}, {2}, {3}, {4}, {5}.",
                    generalListSettings,
                    announcementOrderSetting,
                    channelOrderSettings,
                    groupOrderSettings,
                    conversationOrderSettings,
                    ballotOrderSettings);
                applicationSettingsDatabaseManager.UpdateApplicationSettings(appSettings);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("An error occurred during the list settings update. The error message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            // Lege neues AppSettings Objekt in den Cache.
            AppSettingsCache.GetInstance().CacheApplicationSettings(appSettings);
        }

        /// <summary>
        /// Aktualisiert die Einstellungen bezüglich der Sprache der Anwendung.
        /// </summary>
        /// <param name="favoredLanguage">Die vom Nutzer als bevorzugt gewählte Sprache.</param>
        public void UpdateFavoredLanguageSettings(Language favoredLanguage)
        {
            // Hole die aktuellen Anwendungseinstellungen.
            AppSettings appSettings = GetApplicationSettings();

            appSettings.LanguageSetting = favoredLanguage;

            try
            {
                applicationSettingsDatabaseManager.UpdateApplicationSettings(appSettings);

                if (appSettings.LanguageSetting == Language.ENGLISH)
                {
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "en";
                }
                else if (appSettings.LanguageSetting == Language.GERMAN)
                {
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride = "de";
                }

                Debug.WriteLine("In UpdateFavoredLanguageSettings. The curren PrimaryLanguageOverride is {0}.",
                    Windows.Globalization.ApplicationLanguages.PrimaryLanguageOverride);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("An error occurred during the favored language settings update. The error message is {0}.", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            // Lege neues AppSettings Objekt in den Cache.
            AppSettingsCache.GetInstance().CacheApplicationSettings(appSettings);
        }

        /// <summary>
        /// Liefert die bevorzugte Sprache des Nutzers zurück.
        /// </summary>
        /// <returns>Die bevorzugte Sprache.</returns>
        public Language GetFavoredLanguage()
        {
            return applicationSettingsDatabaseManager.GetFavoredLanguage();
        }
    }
}
