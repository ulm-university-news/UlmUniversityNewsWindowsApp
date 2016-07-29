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
    public class ModeratorController : MainController
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ModeratorDatabaseManager.
        /// </summary>
        private ModeratorDatabaseManager moderatorDatabaseManager;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorController.
        /// </summary>
        /// <param name="errorReporter">Eine Referenz auf eine Realisierung des IValidationErrorReporter Interface.</param>
        public ModeratorController(IValidationErrorReport errorReporter)
            : base(errorReporter)
        {
            moderatorDatabaseManager = new ModeratorDatabaseManager();
        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorController.
        /// </summary>
        public ModeratorController()
            : base()
        {
            moderatorDatabaseManager = new ModeratorDatabaseManager();
        }

        #region LocalModeratorFunctions
        /// <summary>
        /// Liefert den Moderator mit der angegebenen Id.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <returns>Eine Instanz der Klasse Moderator, oder null, wenn lokal 
        ///     kein Moderator mit der angegebnen Id verwaltet wird.</returns>
        public Moderator GetModerator(int moderatorId)
        {
            Moderator moderator = null;
            try
            {
                moderator = moderatorDatabaseManager.GetModerator(moderatorId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("Database failure. Couldn't extract moderator object. " + 
                    "Message is: {0}", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return moderator;
        }

        /// <summary>
        /// Gibt an, ob der Moderator mit der angegebenen Id bereits in den lokalen Datensätzen gespeichert ist.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <returns>Liefert true, wenn der Moderator bereits lokal gespeichert ist, ansonsten false.</returns>
        /// <exception cref="ClientException">Wirft ClientException, wenn Status nicht ermittelt werden konnte.</exception>
        public bool IsModeratorStored(int moderatorId)
        {
            bool isStored = false;
            try
            {
                isStored = moderatorDatabaseManager.IsModeratorStored(moderatorId);
            }
            catch (DatabaseException ex)
            {
                Debug.WriteLine("IsModeratorStored: Database failure. Couldn't determine stored status of moderator. " +
                    "Message is: {0}", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }

            return isStored;
        }

        /// <summary>
        /// Speichert den Moderator in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="moderator">Der zu speichernde Datensatz in Form eines Moderator Objekts.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn Speicherung fehlschlägt.</exception>
        public void StoreModerator(Moderator moderator)
        {
            if (moderator == null)
                return;

            try
            {
                moderatorDatabaseManager.StoreModerator(moderator);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("IsModeratorStored: Database failure. Couldn't store moderator. " +
                    "Message is: {0}", ex.Message);
                throw new ClientException(ErrorCodes.LocalDatabaseException, ex.Message);
            }
        }
        #endregion LocalModeratorFunctions
    }
}
