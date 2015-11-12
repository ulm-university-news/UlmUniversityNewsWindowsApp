using UlmUniversityNews.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Background;
using System.Diagnostics;
using System.Threading.Tasks;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ViewModel;
using DataHandlingLayer.Exceptions;

// Die Vorlage "Pivotanwendung" ist unter http://go.microsoft.com/fwlink/?LinkID=391641 dokumentiert.

namespace UlmUniversityNews
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;
        private LocalUserViewModel localUserViewModel;

        /// <summary>
        /// Initialisiert das Singletonanwendungsobjekt. Dies ist die erste Zeile von erstelltem Code
        /// und daher das logische Äquivalent von main() bzw. WinMain().
        /// </summary>
        public App()
        {
            Debug.WriteLine("Starting App() constructor.");
            this.InitializeComponent();
            this.Suspending += this.OnSuspending;
            this.Resuming += App_Resuming;
            // Erstelle Instanz des LocalUserViewModel.
            localUserViewModel = new LocalUserViewModel();
            Debug.WriteLine("Finished App constructor.");
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Anwendung durch den Endbenutzer normal gestartet wird.  Weitere Einstiegspunkte
        /// werden verwendet, wenn die Anwendung zum Öffnen einer bestimmten Datei, zum Anzeigen
        /// von Suchergebnissen usw. gestartet wird.
        /// </summary>
        /// <param name="e">Details über Startanforderung und -prozess.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
#if DEBUG
            if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }
#endif
            Debug.WriteLine("In OnLaunched method.");
            Frame rootFrame = Window.Current.Content as Frame;

            // Lade die Datenbank.
            DatabaseManager.LoadDatabase();

            // Test start
            // Lösche lokalen Nutzer testweise:
            LocalUserDatabaseManager.DeleteLocalUser();
            // Test end

            // Prüfe, ob bereits ein lokaler Nutzer angelegt ist.
            bool localUserExists = checkLocalUserExistence();

            // Registriere die Hintergrundaufgaben.
            await registerBackgroundTasksAsnyc();

            // App-Initialisierung nicht wiederholen, wenn das Fenster bereits Inhalte enthält.
            // Nur sicherstellen, dass das Fenster aktiv ist.
            if (rootFrame == null)
            {
                // Frame erstellen, der als Navigationskontext fungiert und zum Parameter der ersten Seite navigieren.
                rootFrame = new Frame();

                // Verknüpfen Sie den Frame mit einem SuspensionManager-Schlüssel.
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                // TODO: diesen Wert auf eine Cachegröße ändern, die für Ihre Anwendung geeignet ist
                // siehe: https://msdn.microsoft.com/en-us/library/system.windows.controls.frame.cachesize%28v=vs.95%29.aspx
                rootFrame.CacheSize = 1;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Den gespeicherten Sitzungszustand nur bei Bedarf wiederherstellen.
                    try
                    {
                        await SuspensionManager.RestoreAsync();
                    }
                    catch (SuspensionManagerException)
                    {
                        // Fehler beim Wiederherstellen des Zustands.
                        // Annehmen, dass kein Zustand vorhanden ist und Vorgang fortsetzen.
                    }
                }

                // Den Frame im aktuellen Fenster platzieren.
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // Entfernt die Drehkreuznavigation für den Start.
                if (rootFrame.ContentTransitions != null)
                {
                    this.transitions = new TransitionCollection();
                    foreach (var c in rootFrame.ContentTransitions)
                    {
                        this.transitions.Add(c);
                    }
                }

                rootFrame.ContentTransitions = null;
                rootFrame.Navigated += this.RootFrame_FirstNavigated;

                // Navigiere zum Homescreen, wenn der lokale Nutzeraccount bereits existiert.
                if (localUserExists) {
                    if (!rootFrame.Navigate(typeof(Views.Homescreen.Homescreen), e.Arguments))
                    {
                        throw new Exception("Failed to create homepage");
                    }
                }
                else
                {
                    // Navigiere zur Startseite. Dort kann beim ersten Start der App der lokale Nutzeraccount angelegt werden.
                    if (!rootFrame.Navigate(typeof(Views.StartPage), e.Arguments))
                    {
                        throw new Exception("Failed to create start page");
                    }
                }
            }

            // Sicherstellen, dass das aktuelle Fenster aktiv ist.
            Window.Current.Activate();
            Debug.WriteLine("Finished OnLaunched Method.");
        }

        /// <summary>
        /// Wird aufgerufen wenn die App fortgesetzt wird, d.h. aus einem angehaltenen Zustand heraus wieder aktiv wird.
        /// </summary>
        /// <param name="sender">Die Quelle der Anforderung.</param>
        /// <param name="e">Details über das Fortsetzen der App.</param>
        void App_Resuming(object sender, object e)
        {
            Debug.WriteLine("In AppResuming EventHandler.");
            // TODO
            Debug.WriteLine("Finished AppResuming EventHandler.");
        }

        /// <summary>
        /// Fordert den Zugriff auf den LockScreen an, um Hintergrundaufgaben ausführen zu dürfen. Wird dieser gewährt so werden die 
        /// Hintergrundaufgaben registriert. Wird der Zugriff abgelehnt, so speichert die Methode dies in the lokalen Anwendungseinstellungen
        /// und führt die Registrierung nicht durch.
        /// </summary>
        private async Task registerBackgroundTasksAsnyc()
        {
            Debug.WriteLine("Starting registerBackgroundTasks.");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            // Fordere Zugriff auf LockScreen an, um die vollen Ressourcen Quotas zu erhalten.
            var result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.Denied){
                Debug.WriteLine("Access to lock screen denied. No background tasks can be registered.");
                // Das Windows Phone Gerät hat der Anwendung nicht gestattet Hintergrundaufgaben zu definieren,
                // oder der Nutzer hat das für diese Anwendung explizit deaktiviert.
                // Speichere in den LocalSettings, dass der Zugriff verweigert wurde.
                localSettings.Values[Constants.Constants.AccessToLockScreenKey] = Constants.Constants.AccessToLockScreenDenied;
                if (localSettings.Values[Constants.Constants.ShowLockScreenMessageKey] == null){
                    // Zeige Benachrichtigung, falls diese noch nicht gezeigt wurde. 
                    localSettings.Values[Constants.Constants.ShowLockScreenMessageKey] = Constants.Constants.ShowLockScreenMessageYes;
                }
            }
            else
            {
                Debug.WriteLine("Access to lock screen granted. Starting to register background tasks.");
                // Hintergrundaufgaben dürfen hinzu gefügt werden und erhalten das volle Ressourcen-Quota.
                // Registriere den BackgroundTask, der beim Empfang von RawPushNotifications gestartet wird.
                PushNotificationManagerBackground.PushNotificationManager.Register();
                localSettings.Values[Constants.Constants.AccessToLockScreenKey] = Constants.Constants.AccessToLockScreenGranted;
            }
            Debug.WriteLine("Finished registerBackgroundTasks.");
        }

        /// <summary>
        /// Prüft, ob es bereits einen lokalen Nutzeraccount gibt.
        /// </summary>
        /// <returns>Liefert true zurück wenn bereits ein lokaler Nutzeraccount existiert, ansonsten false.</returns>
        private bool checkLocalUserExistence()
        {
            User localUser = null;
            try
            {
                localUser = localUserViewModel.GetLocalUser();
            }
            catch(ClientException ex){
                // TODO - How to handle this error?
                Debug.WriteLine("Error while trying to retrieve local user account.");
            }

            if(localUser == null){
                return false;
            }
            return true;
        }

        /// <summary>
        /// Stellt die Inhaltsübergänge nach dem Start der App wieder her.
        /// </summary>
        private void RootFrame_FirstNavigated(object sender, NavigationEventArgs e)
        {
            var rootFrame = sender as Frame;
            rootFrame.ContentTransitions = this.transitions ?? new TransitionCollection() { new NavigationThemeTransition() };
            rootFrame.Navigated -= this.RootFrame_FirstNavigated;
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Ausführung der Anwendung angehalten wird.  Der Anwendungszustand wird gespeichert,
        /// ohne zu wissen, ob die Anwendung beendet oder fortgesetzt wird und die Speicherinhalte dabei
        /// unbeschädigt bleiben.
        /// </summary>
        /// <param name="sender">Die Quelle der Anhalteanforderung.</param>
        /// <param name="e">Details zur Anhalteanforderung.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }
    }
}
