using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using System;
using System.Collections.Generic;
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
        public async Task UpdateLocalUsernameAsync(string username)
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
                    return;
                }

                // Verwende Funktionalität im LocalUserController, um die Aktualisierung des Namens durchzuführen.
                await localUserController.UpdateLocalUserAsync(username, null);
            }
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
