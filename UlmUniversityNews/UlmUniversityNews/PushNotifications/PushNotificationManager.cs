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
    public class PushNotificationManager
    {
        private static PushNotificationManager _instance = null;

        /// <summary>
        /// Der Kanal für die Push Nachrichten.
        /// </summary>
        private PushNotificationChannel _pushChannel;

        /// <summary>
        /// Eine Instanz der LocalUserViewModel Klasse.
        /// </summary>
        private LocalUserViewModel localUserViewModel;

        /// <summary>
        /// Erzeugt eine Instanz der PushNotificationManager Klasse.
        /// </summary>
        private PushNotificationManager(){
            localUserViewModel = new LocalUserViewModel();
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
        /// Aktualisiert die Kanal-URI lokal in der Anwendung und auf dem Server falls notwendig.
        /// Eine Aktualisierung ist nur dann erforderlich, wenn die Kanal-URI sich seit der letzten
        /// Aktualisierung geändert hat. Ist keine Aktualisierung erforderlich, so wird keine Aktion
        /// durchgeführt.
        /// </summary>
        /// <returns>Liefert true zurück, wenn die Aktualisierung erfolgreich war, sonst false.</returns>
        public async Task<bool> UpdateRemoteChannelURIAsync()
        {
            bool successful = false;

            String localUserChannelURI = localUserViewModel.GetPushChannelURIOfLocalUser();
            if(localUserChannelURI != null && _pushChannel != null){
                String currentChannelURI = _pushChannel.Uri;
                if(String.Compare(localUserChannelURI,currentChannelURI) == 0){
                    Debug.WriteLine("Channel URI hasn't changed. No update required.");
                    // Keine Aktualisierung erforderlich.
                    successful = true;
                }
                else
                {
                    Debug.WriteLine("Need to update the channel URI, i.e. the push token of the local user. Starting updating process.");
                    // Aktualisierung des Push Access Token des lokalen Nutzers erforderlich.
                    try
                    {
                        await localUserViewModel.UpdateLocalUserAsync(string.Empty, currentChannelURI);
                        successful = true;
                        Debug.WriteLine("Updated channel URI (push access token) successfully.");
                    }
                    catch(ClientException ex)
                    {
                        Debug.WriteLine("Client exception occurred. Updating channel URI (push access token) has failed. Error code is: " + ex.ErrorCode);
                    }
                }
            }

            return successful;
        }

        /// <summary>
        /// Handler Methode, die das PushNotificationReceived Event behandelt. Bei einer eingehenden Push Nachricht wird dieses Event gefeuert
        /// und der Handler aufgerufen. 
        /// </summary>
        /// <param name="sender">Der Kanal über den die Push Nachricht geschickt wurde.</param>
        /// <param name="args">Die eingegangene Push Nachricht.</param>
        void _pushChannel_PushNotificationReceived(PushNotificationChannel sender, PushNotificationReceivedEventArgs args)
        {
            if (args.NotificationType == PushNotificationType.Raw){
                Debug.WriteLine("RawNotification received within the app.");
                // TODO

                // Spezifiziere das Event als behandelt, so dass es nicht an die Background Task geht.
                args.Cancel = true;
            }
        }
    }
}
