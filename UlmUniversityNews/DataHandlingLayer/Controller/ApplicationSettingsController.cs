using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.DataModel;
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
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsController.
        /// </summary>
        public ApplicationSettingsController()
            : base()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ApplicationSettingsController.
        /// </summary>
        /// <param name="validationErrorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReport Interface.</param>
        public ApplicationSettingsController(IValidationErrorReport validationErrorReporter)
            : base(validationErrorReporter)
        {

        }

        /// <summary>
        /// Gibt den aktuellen lokalen Nutzer zurück.
        /// </summary>
        /// <returns>Ein Objekt vom Typ User.</returns>
        public User GetCurrentLocalUser()
        {
            return base.getLocalUser();
        }
    }
}
