using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Networking.PushNotifications;

namespace PushNotificationManagerBackground
{
    /// <summary>
    /// Diese Klasse realisiert eine Hintergrundaufgabe, die von einem Maintenance Trigger ausgelöst wird. 
    /// Sie führt in einem regelmäßigen Wartungsintervall eine Aktualisierung des Push Nachrichten Kanals durch,
    /// um einen stets aktuellen Kanal zu garantieren, auch dann wenn die Anwendung über einen längeren Zeitraum nicht
    /// ausgeführt wird.
    /// </summary>
    public sealed class PushNotificationMaintenanceTask : IBackgroundTask
    {
        private const string BG_TASK_NAME = "PushNotificationMaintenanceBG";
        private const int MAINTENANCE_INTERVAL = 15 * 24 * 60;      // Führe die Wartung alle 15 Tage aus.

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            Debug.WriteLine("Started PushNotificationMaintenanceTask background task.");

            // Füge einen CancelationHandler hinzu, der gerufen wird sollte die BackgroundTask abgebrochen werden.
            taskInstance.Canceled += new BackgroundTaskCanceledEventHandler(OnCanceled);

            // Fordere ein BackgroundTaskDeferral Objekt an. Wenn asynchrone Operationen innerhalb der BackgroundTask ausgeführt werden
            // dann wird ein solches Objekt benötigt, um das vorzeitige Schließen der Task zu verhindern.
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            PushNotificationChannel pushChannel = null;
            try
            {
                // Hole eine Instanz des lokalen Nutzers.
                DataHandlingLayer.ViewModel.LocalUserViewModel localUserViewModel = new DataHandlingLayer.ViewModel.LocalUserViewModel();
                DataHandlingLayer.DataModel.User localUser = localUserViewModel.GetLocalUser();
                
                // Frage einen neuen PushNotificationChannel an.
                pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                Debug.WriteLine("Received a channel URI: " + pushChannel.Uri);

                if (pushChannel != null && localUser != null)
                {
                    // Prüfe, ob die Kanal-URI aktualisiert werden muss
                    if (String.Compare(pushChannel.Uri, localUser.PushAccessToken) == 0)
                    {
                        Debug.WriteLine("No need to update the channel URI. It is still the same.");
                    }
                    else
                    {
                        Debug.WriteLine("Updating the push access token.");
                        await localUserViewModel.UpdateLocalUserAsync(string.Empty, pushChannel.Uri);
                    }
                }
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception occurred. The exception message is {0}.", ex.Message);
            }

            // Task als abgeschlossen kennzeichnen.
            deferral.Complete();
        }

        /// <summary>
        /// Behandelt den Abbruch der Hintergrundaufgabe.
        /// </summary>
        /// <param name="sender">Die Instanz der IBackgroundTask, die das Canceled Event ausgelöst hat.</param>
        /// <param name="reason">Der Grund für den Abbruch.</param>
        private void OnCanceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // Mache hier vorest nichts. Nur Fehler loggen.
            Debug.WriteLine("Abort PushNotificationMaintenanceTask background task. Sender: " + sender.ToString() + ", Reason: " + reason.ToString());
        }

        /// <summary>
        /// Registriert eine Hintergrundaufgabe, die durch einen MaintenanceTrigger ausgelöst wird. MaintenanceTrigger bedeutet, dass der Aufruf
        /// nur erfolgt, wenn das Gerät ans Stromnetz angeschlossen ist. Die Hintergrundaufgabe wird in einem gewissen Wartungsintervall durchgeführt.
        /// </summary>
        public static void Register()
        {
            // Registriere die Task, wenn sie nicht bereits registriert wurde.
            if (!IsTaskRegistered())
            {
                var builder = new BackgroundTaskBuilder();

                // Setze den eindeutigen Namen, den EntryPoint für die BG Task und den Trigger, der die BG Task auslöst.
                builder.Name = BG_TASK_NAME;
                builder.TaskEntryPoint = typeof(PushNotificationMaintenanceTask).FullName;
                builder.SetTrigger(new MaintenanceTrigger(MAINTENANCE_INTERVAL, false));

                // Lege Bedingung für die Ausführung der BackgroundTask fest. Hier, InternetAvailable und UserNotPresent muss zutreffen.
                // UserNotPresent wird hier gesetzt, um mögliche Konflikte durch die parallele Ausführung von Anwendung und BackgroundTask zu vermeiden.
                SystemCondition internetCondition = new SystemCondition(SystemConditionType.InternetAvailable);
                SystemCondition userNotPresentCondition = new SystemCondition(SystemConditionType.UserNotPresent);
                builder.AddCondition(internetCondition);
                builder.AddCondition(userNotPresentCondition);
                

                // Registriere die Task.
                BackgroundTaskRegistration task = builder.Register();
                Debug.WriteLine("Background task PushNotificationMaintenanceTask is registered.");
            }
            else
            {
                Debug.WriteLine("PushNotificationMaintenanceTask background task already registered. Not necessary to perform registration once again.");
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
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == BG_TASK_NAME)
                {
                    isRegistered = true;
                }
            }

            return isRegistered;
        }
    }
}
