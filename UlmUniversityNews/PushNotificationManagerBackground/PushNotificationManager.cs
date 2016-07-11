using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Networking.PushNotifications;
using Windows.UI.Notifications;

namespace PushNotificationManagerBackground
{
    public sealed class PushNotificationManager : IBackgroundTask
    {
        private const string BG_TASK_NAME = "RawNotificationReceiverBG"; 

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Started RawNotificationReceiver background task.");
            // Füge einen CancelationHandler hinzu, der gerufen wird sollte die BackgroundTask abgebrochen werden.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            // Fordere ein BackgroundTaskDeferral Objekt an. Wenn asynchrone Operationen innerhalb der BackgroundTask ausgeführt werden
            // dann wird ein solches Objekt benötigt, um das vorzeitige Schließen der Task zu verhindern.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            // Hole die RawNotification über die TriggerDetails.
            RawNotification notification = (RawNotification)taskInstance.TriggerDetails;
            
            PushNotificationController pushController = new PushNotificationController();
            // Frage PushMessage Objekt zur Notification ab und stoße Behandlung an.
            PushMessage pm = pushController.GetPushMessageFromNotification(notification);
            bool handledSuccessfully = await pushController.HandlePushNotificationAsync(pm);
            
            // Soll der Nutzer benachrichtigt werden?
            if(handledSuccessfully && pushController.IsUserNotificationRequired(pm))
            {
                string headline = pushController.GetUserNotificationHeadline(pm);
                string resourceKey = pushController.GetUserNotificationContentLocalizationKey(pm);
                showToastNotification(headline, resourceKey);
            }

            Debug.WriteLine("Finished background task.");
            // Task als abgeschlossen kennzeichnen.
            deferral.Complete();
        }
        
        /// <summary>
        /// Zeige den Text in einer ToastNotification an, um den Nutzer über ein Ereignis zu informieren.
        /// </summary>
        /// <param name="headline">Die anzuzeigende Überschrift.</param>
        /// <param name="resourceKey">Der Ressourcenschlüssel für den Inhalt der Benachrichtigung.</param>
        private void showToastNotification(string headline, string resourceKey)
        {
            // Für den Anfang, sende nur eine ToastNotification mit dem Typ der PushNachricht und mache weiter nichts.
            var toastDescriptor = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // Setze den Text - Headline.
            var txtNodes = toastDescriptor.GetElementsByTagName("text");
            txtNodes[0].AppendChild(toastDescriptor.CreateTextNode(headline));

            // Setze den Text - Inhalt.
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            string content = loader.GetString(resourceKey);
            txtNodes[1].AppendChild(toastDescriptor.CreateTextNode(content));

            var toast = new ToastNotification(toastDescriptor);
            var toastNotifier = ToastNotificationManager.CreateToastNotifier();
            toastNotifier.Show(toast);
        }
        
        /// <summary>
        /// Behandelt den Abbruch der Hintergrundaufgabe.
        /// </summary>
        /// <param name="sender">Die Instanz der IBackgroundTask, die das Canceled Event ausgelöst hat.</param>
        /// <param name="reason">Der Grund für den Abbruch.</param>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // Mache hier vorest nichts. Nur Fehler loggen.
            Debug.WriteLine("Abort RawNotification background task. Sender: " + sender.ToString() + ", Reason: " + reason.ToString());
        }

        /// <summary>
        /// Registriert eine Hintergrundaufgabe, die durch eine eingehende RawNotification ausgelöst wird. Es muss beachtet werden, dass 
        /// hierfür Zugriff auf den LockScreen gewährt sein muss. Es muss sichergestellt werden, dass RequestAccessAsync() aufgerufen wurde, 
        /// bevor die Registrierung erfolgt.
        /// </summary>
        public static void Register()
        {
            // Registriere die Task, wenn sie nicht bereits registriert wurde.
            if (!IsTaskRegistered()){
                var builder = new BackgroundTaskBuilder();

                // Setze den eindeutigen Namen, den EntryPoint für die BG Task und den Trigger, der die BG Task auslöst.
                builder.Name = BG_TASK_NAME;
                builder.TaskEntryPoint = typeof(PushNotificationManager).FullName;
                builder.SetTrigger(new PushNotificationTrigger());

                // Lege Bedingung für die Ausführung der BackgroundTask fest. Hier, InternetAvailable muss zutreffen.
                SystemCondition internetCondition = new SystemCondition(SystemConditionType.InternetAvailable);
                builder.AddCondition(internetCondition);

                // Registriere die Task.
                BackgroundTaskRegistration task = builder.Register();
                Debug.WriteLine("Background task PushNotificationManager is registered.");
            }
            else {
                Debug.WriteLine("PushNotificationManager background task already registered. Not necessary to perform registration once again.");
            }
        }

        /// <summary>
        /// Prüft, ob die Hintergrundaufgabe bereits registriert ist.
        /// </summary>
        /// <returns>Liefert true zurück wenn die Hintergrundaufgabe bereits registriert ist, ansonsten false.</returns>
        public static bool IsTaskRegistered()
        {
            bool isRegistered = false;

            // Prüfe alle registrierten Tasks auf den Namen dieses BackgroundTasks ab.
            foreach (var task in BackgroundTaskRegistration.AllTasks){
                if (task.Value.Name == BG_TASK_NAME){
                    isRegistered = true;
                }
            }

            return isRegistered;
        }
    }
}
