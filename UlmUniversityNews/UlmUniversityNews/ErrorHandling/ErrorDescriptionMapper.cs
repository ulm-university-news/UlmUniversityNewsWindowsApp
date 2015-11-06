using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.ErrorHandling
{
    /// <summary>
    /// Die ErrorDescriptionMapper Klasse ist eine Singleton-Klasse, die Funktionalität zur Abbildung von
    /// Fehlercodes auf entsprechende Fehlernachrichten bereitstellt. Die Fehlernachricht wird dann abhängig von
    /// der eingestellten bevorzugten Sprache des Nutzers zurückgeliefert.
    /// </summary>
    public class ErrorDescriptionMapper
    {
        private static ErrorDescriptionMapper _instance;

        /// <summary>
        /// Liefert eine Instanz der ErrorDescriptionMapper Klasse zurück.
        /// Diese Klasse kann dazu genutzt werden, Fehlercodes auf die entsprechende Fehlerbeschreibung abzubilden.
        /// Die Fehlerbeschreibung wird dabei abhängig von der eingestellten Sprache zurückgeliefert.
        /// </summary>
        /// <returns>Eine Instanz der ErrorDescriptionMapper Klasse.</returns>
        public static ErrorDescriptionMapper GetInstance()
        {
            lock (typeof(ErrorDescriptionMapper))
            {
                if (_instance == null)
                {
                    _instance = new ErrorDescriptionMapper();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Liefert eine Fehlerbeschreibung zum übergebenen Fehlercode zurück.
        /// Die Fehlerbeschreibung wird dabei abhängig von der eingestellten Sprache zurückgeliefert.
        /// </summary>
        /// <param name="errorCode">Der Fehlercode, zu dem die Fehlerbeschreibung zurückgegeben werden soll.</param>
        /// <returns>Eine Fehlerbeschreibung.</returns>
        public string GetErrorDescription(int errorCode)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string errorDescription = string.Empty;
            switch (errorCode)
            {
                case ErrorCodes.UserNotFound:
                    errorDescription = loader.GetString("UserNotFoundError");
                    break;
                case ErrorCodes.UserForbidden:
                    errorDescription = loader.GetString("UserForbiddenError");
                    break;
                case ErrorCodes.UserDataIncomplete:
                    errorDescription = loader.GetString("UserDataIncompleteError");
                    break;
                case ErrorCodes.UserNameInvalid:
                    errorDescription = loader.GetString("UserNameInvalidError");
                    break;
                case ErrorCodes.UserPushTokenInvalid:
                    errorDescription = loader.GetString("UserPushTokenInvalidError");
                    break;
                case ErrorCodes.BadRequest:
                    errorDescription = loader.GetString("BadRequestError");;
                    break;
                case ErrorCodes.MethodNotAllowed:
                    errorDescription = loader.GetString("MethodNotAllowedError");
                    break;
                case ErrorCodes.NotFound:
                    errorDescription = loader.GetString("NotFoundError");
                    break;
                case ErrorCodes.UnsupportedMediaType:
                    errorDescription = loader.GetString("UnsupportedMediaTypeError");
                    break;
                case ErrorCodes.WnsChannelInitializationFailed:
                    errorDescription = loader.GetString("WnsChannelInitializationFailed");
                    break;
                case ErrorCodes.LocalDatabaseException:
                    errorDescription = loader.GetString("LocalDatabaseError");
                    break;
                default:
                    errorDescription = "Something went wrong. We are sorry!";
                    break;
            }

            return errorDescription;
        }

    }
}
