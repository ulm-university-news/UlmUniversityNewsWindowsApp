using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.ViewModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;

namespace UlmUniversityNews.PushNotifications
{
    /// <summary>
    /// Die PushNotificationManager Klasse ist eine Singleton-Klasse, die die notwendige Interaktion mit dem WNS
    /// kapselt. Die Klasse bietet Funktionalität an, um eine Kanal für PushNachrichten vom WNS zu erhalten.
    /// Zudem werden PushNotificiations, die zur Laufzeit der Anwendung eintreffen, in dieser Klasse behandelt.
    /// </summary>
    public class PushNotificationManager
    {
        #region Fields
        private static PushNotificationManager _instance = null;

        /// <summary>
        /// Der Kanal für die Push Nachrichten.
        /// </summary>
        private PushNotificationChannel _pushChannel;

        /// <summary>
        /// Eine Instanz der Klasse PushNotificationController.
        /// </summary>
        private PushNotificationController pushController;

        /// <summary>
        /// Eine Instanz der LocalUserViewModel Klasse.
        /// </summary>
        //private LocalUserViewModel localUserViewModel;
        #endregion Fields

        #region Events
        public event EventHandler ReceivedAnnouncement;
        #endregion Events

        /// <summary>
        /// Erzeugt eine Instanz der PushNotificationManager Klasse.
        /// </summary>
        private PushNotificationManager(){
            pushController = new PushNotificationController();
        }

        /// <summary>
        /// Liefert eine Instanz der Singleton-Klasse PushNotificationManager zurück.
        /// </summary>
        /// <returns>Instanz von PushNotificationManager.</returns>
        public static PushNotificationManager GetInstance()
        {
            lock (typeof(PushNotificationManager)){
                if (_instance == null){
                    _instance = new PushNotificationManager();
                }
            }
            return _instance;
        }

        /// <summary>
        /// Initialisiere den PushNotificationManager. Es wird eine Kanal-URI angefordert und 
        /// diese lokal gespeichert. Der Event-Handler zur Behandlung von eingehenden Push Nachrichten
        /// wird gesetzt.
        /// </summary>
        public async Task InitializeAsync()
        {
            Debug.WriteLine("Start initializing the PushNotificationManager.");

            // Potentielle Fehlercodes.
            int noDataConnectivity = Convert.ToInt32("0x800704C6", 16);
            int internalWnsError = Convert.ToInt32("0x803E011B", 16);
            int notificationPlatformBusy = Convert.ToInt32("0x880403E9", 16);
            int channelRequestTimeout = Convert.ToInt32("0x80070102", 16);

            try
            {
                _pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                Debug.WriteLine("Received a channel URI: " + _pushChannel.Uri);
            }
            catch (Exception e) 
            {
                // TODO Implementierung einer Retry Strategie.
                if (e.HResult == noDataConnectivity){
                    Debug.WriteLine("No internet connectivity. The WNS cloud could not be reached.");
                }
                else if (e.HResult == internalWnsError){
                    Debug.WriteLine("WNS platform could not be reached. The WNS might not respond or has shown an internal error.");
                }
                else if (e.HResult == notificationPlatformBusy){
                    Debug.WriteLine("The notification platform is in the process of reconnecting back to the WNS cloud due to a earlier network connectivity change");
                }
                else if (e.HResult == channelRequestTimeout){
                    Debug.WriteLine("The earlier channel request async operation has timed out");
                }
                else
                {
                    Debug.WriteLine("Something went terribly wrong with the CreatePushNotificationChannelForApplicationAsync.");
                }
            }

            if (_pushChannel != null){
                // Registrierung des Handlers, welcher zur Laufzeit der App eingehende Push Nachrichten behandelt.
                _pushChannel.PushNotificationReceived += _pushChannel_PushNotificationReceived;
            }

            Debug.WriteLine("Finished initializing the PushNotificationManager.");
        }

        /// <summary>
        /// Prüft, ob der Kanal für die Push-Benachrichtigungen initialisiert ist.
        /// </summary>
        /// <returns>Liefert true zurück, wenn der Kanal initialisiert ist, ansonsten false.</returns>
        public bool IsInitialized()
        {
            if(_pushChannel != null && _pushChannel.Uri != string.Empty){
                Debug.WriteLine("The push channel seems to be initialized.");
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gibt die Kanal-URI des Kanals für die Push Nachrichten zurück. Wurde kein Kanal gesetzt, so wird null 
        /// zurückgegeben.
        /// </summary>
        /// <returns>Die Kanal-URI als String, oder null wenn kein Kanal gesetzt ist.</returns>
        public String GetChannelURIAsString()
        {
            String channelURI = null;
            if(_pushChannel != null){
                channelURI = _pushChannel.Uri;
            }
            return channelURI;
        }

        /// <summary>
        /// Handler Methode, die das PushNotificationReceived Event behandelt. Bei einer eingehenden Push Nachricht wird dieses Event gefeuert
        /// und der Handler aufgerufen. 
        /// </summary>
        /// <param name="sender">Der Kanal über den die Push Nachricht geschickt wurde.</param>
        /// <param name="args">Die eingegangene Push Nachricht.</param>
        async void _pushChannel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            if (args.NotificationType == PushNotificationType.Raw){
                Debug.WriteLine("RawNotification received within the app.");
                
                // Extrahiere die PushNotification.
                RawNotification receivedNotification = args.RawNotification;
                // Spezifiziere das Event als behandelt, so dass es nicht an die Background Task geht.
                args.Cancel = true;

                // Stoße Behandlung der Push Notification an.
                bool handledSuccessfully = await pushController.HandlePushNotificationAsync(receivedNotification);

                if(handledSuccessfully)
                {
                    PushMessage pushMsg = pushController.GetPushMessageFromNotification(receivedNotification);
                    // TODO - Benachrichtige View, so dass diese sich aktualisieren können.
                    if (pushMsg != null)
                    {
                        switch (pushMsg.PushType)
                        {
                            case DataHandlingLayer.DataModel.Enums.PushType.ANNOUNCEMENT_NEW:
                                // Sende Event an Listener.
                                if (ReceivedAnnouncement != null)
                                {
                                    ReceivedAnnouncement(this, new EventArgs());
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }

                // TODO - Benachrichtigung des Nutzers?
            }
        }
    }
}
