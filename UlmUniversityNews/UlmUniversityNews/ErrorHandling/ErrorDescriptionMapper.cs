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
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            string errorDescription = string.Empty;
            switch (errorCode)
            {
                case ErrorCodes.UserNotFound:
                    errorDescription = loader.GetString("ApplicationErrorUserNotFoundError");
                    break;
                case ErrorCodes.UserForbidden:
                    errorDescription = loader.GetString("ApplicationErrorUserForbiddenError");
                    break;
                case ErrorCodes.UserDataIncomplete:
                    errorDescription = loader.GetString("ApplicationErrorUserDataIncompleteError");
                    break;
                case ErrorCodes.UserNameInvalid:
                    errorDescription = loader.GetString("ApplicationErrorUserNameInvalidError");
                    break;
                case ErrorCodes.UserPushTokenInvalid:
                    errorDescription = loader.GetString("ApplicationErrorUserPushTokenInvalidError");
                    break;
                case ErrorCodes.ChannelNotFound:
                    errorDescription = loader.GetString("ApplicationErrorChannelNotFoundError");
                    break;
                case ErrorCodes.ChannelNameAlreadyExists:
                    errorDescription = loader.GetString("ApplicationErrorChannelNameAlreadyExistsError");
                    break;
                case ErrorCodes.ChannelInvalidWebsite:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidWebsiteError");
                    break;
                case ErrorCodes.ChannelInvalidType:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidTypeError");
                    break;
                case ErrorCodes.ChannelInvalidTerm:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidTermError");
                    break;
                case ErrorCodes.ChannelInvalidStartDate:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidStartDateError");
                    break;
                case ErrorCodes.ChannelInvalidParticipants:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidParticipantsError");
                    break;
                case ErrorCodes.ChannelInvalidOrganizer:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidOrganizerError");
                    break;
                case ErrorCodes.ChannelInvalidName:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidNameError");
                    break;
                case ErrorCodes.ChannelInvalidLocations:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidLocationsError");
                    break;
                case ErrorCodes.ChannelInvalidLecturer:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidLecturerError");
                    break;
                case ErrorCodes.ChannelInvalidEndDate:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidEndDateError");
                    break;
                case ErrorCodes.ChannelInvalidDescription:
                    errorDescription = loader.GetString("ApplicationErrorChannelDescriptionError");
                    break;
                case ErrorCodes.ChannelInvalidDates:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidDatesError");
                    break;
                case ErrorCodes.ChannelInvalidCost:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidCostError");
                    break;
                case ErrorCodes.ChannelInvalidContacts:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidContacts");
                    break;
                case ErrorCodes.ChannelInvalidAssistant:
                    errorDescription = loader.GetString("ApplicationErrorChannelInvalidAssistantError");
                    break;
                case ErrorCodes.ChannelDataIncomplete:
                    errorDescription = loader.GetString("ApplicationErrorChannelDataIncompleteError");
                    break;
                case ErrorCodes.ReminderNotFound:
                    errorDescription = loader.GetString("ApplicationErrorReminderNotFoundError");
                    break;
                case ErrorCodes.BadRequest:
                    errorDescription = loader.GetString("ApplicationErrorBadRequestError");;
                    break;
                case ErrorCodes.MethodNotAllowed:
                    errorDescription = loader.GetString("ApplicationErrorMethodNotAllowedError");
                    break;
                case ErrorCodes.NotFound:
                    errorDescription = loader.GetString("ApplicationErrorNotFoundError");
                    break;
                case ErrorCodes.UnsupportedMediaType:
                    errorDescription = loader.GetString("ApplicationErrorUnsupportedMediaTypeError");
                    break;
                case ErrorCodes.WnsChannelInitializationFailed:
                    errorDescription = loader.GetString("ApplicationErrorWnsChannelInitializationFailed");
                    break;
                case ErrorCodes.LocalDatabaseException:
                    errorDescription = loader.GetString("ApplicationErrorLocalDatabaseError");
                    break;
                case ErrorCodes.ServerUnreachable:
                    errorDescription = loader.GetString("ApplicationErrorServerUnreachableError");
                    break;
                case ErrorCodes.ServerDatabaseFailure:
                    errorDescription = loader.GetString("ApplicationErrorErrorServerDatabaseFailure");
                    break;
                default:
                    errorDescription = loader.GetString("ApplicationErrorDefaultCase");
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
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            string title = loader.GetString("ErrorDialogBoxTitle");

            var dialog = new Windows.UI.Popups.MessageDialog(content, title);
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Ok") { Id = 0 });
            dialog.DefaultCommandIndex = 0;

            var result = await dialog.ShowAsync();
        }

    }
}
