using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UlmUniversityNews.ErrorHandling
{
    /// <summary>
    /// Die ErrorDescriptionMapper Klasse ist eine Klasse, die Funktionalität zur Abbildung von
    /// Fehlercodes auf entsprechende Fehlernachrichten beinhaltet und das Anzeigen von Fehlernachrichten
    /// übernimmt. Die Fehlernachricht wird abhängig von der eingestellten bevorzugten Sprache des Nutzers angezeigt.
    /// </summary>
    public class ErrorDescriptionMapper : IErrorMapper
    {
        /// <summary>
        /// Zeigt eine Fehlernachricht auf dem Bildschirm in Form einer MessageBox an. Die Fehlernachricht wird unter
        /// Verwendung des ErrorCodes und der eingestellten Sprache der Anwendung generiert.
        /// </summary>
        /// <param name="errorCode">Der Fehlercode des aufgetretenen Fehlers.</param>
        public void DisplayErrorMessage(int errorCode)
        {
            Debug.WriteLine("Received displaying request. Error code is: " + errorCode);
            string errorMessage = getErrorDescription(errorCode);

            showErrorMessageDialogAsync(errorMessage);
        }

        /// <summary>
        /// Liefert eine Fehlerbeschreibung zum übergebenen Fehlercode zurück.
        /// Die Fehlerbeschreibung wird dabei abhängig von der eingestellten Sprache zurückgeliefert.
        /// </summary>
        /// <param name="errorCode">Der Fehlercode, zu dem die Fehlerbeschreibung zurückgegeben werden soll.</param>
        /// <returns>Eine Fehlerbeschreibung.</returns>
        private string getErrorDescription(int errorCode)
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
                case ErrorCodes.ServerUnreachable:
                    errorDescription = loader.GetString("ServerUnreachableError");
                    break;
                default:
                    errorDescription = "Something went wrong. We are sorry!";
                    break;
            }

            return errorDescription;
        }

        /// <summary>
        /// Zeigt eine Fehlernachricht innerhalb eines MessageDialog Elements an.
        /// </summary>
        /// <param name="content">Der Inhalt des MessageDialog Elements, d.h. die Beschreibung des Fehlers.</param>
        private async void showErrorMessageDialogAsync(string content)
        {
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            string title = loader.GetString("ErrorDialogBoxTitle");

            var dialog = new Windows.UI.Popups.MessageDialog(content, title);
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Ok") { Id = 0 });
            dialog.DefaultCommandIndex = 0;

            var result = await dialog.ShowAsync();
        }

    }
}
