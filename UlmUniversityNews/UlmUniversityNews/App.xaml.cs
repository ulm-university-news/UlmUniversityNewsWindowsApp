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
using UlmUniversityNews.Views;
using UlmUniversityNews.ErrorHandling;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Constants;

// Die Vorlage "Pivotanwendung" ist unter http://go.microsoft.com/fwlink/?LinkID=391641 dokumentiert.

namespace UlmUniversityNews
{
    /// <summary>
    /// Stellt das anwendungsspezifische Verhalten bereit, um die Standardanwendungsklasse zu ergänzen.
    /// </summary>
    public sealed partial class App : Application
    {
        private TransitionCollection transitions;

        /// <summary>
        /// Referenz auf den Controller, der Funktionalität bezüglich des lokalen Nutzerobjekts bereitstellt.
        /// </summary>
        private LocalUserController localUserController;

        /// <summary>
        /// Referenz auf den Navigationsdienst der Anwendung.
        /// </summary>
        public static UlmUniversityNews.NavigationService.NavigationService NavigationService;

        /// <summary>
        /// Referenz auf den ErrorMapper der Anwendung.
        /// </summary>
        public static ErrorDescriptionMapper ErrorMapper;

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

            this.UnhandledException += App_UnhandledException;

            // Erstelle Instanz des ErrorMapper.
            ErrorMapper = new ErrorDescriptionMapper();

            // Erstelle Instanz des LocalUserController.
            localUserController = new LocalUserController();

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
            // DatabaseManager.UpgradeDatabase();
            DatabaseManager.LoadDatabase();

            // TODO Test start
            // Lösche lokalen Nutzer testweise:
            // LocalUserDatabaseManager.DeleteLocalUser();
            // Test end

            // TODO Test start
            // Füge lokalen Nutzer wieder ein.
            // LocalUserDatabaseManager.InsertTestLocalUser();
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

                // Erzeuge Instanz des Navigationsdiensts und lade die Seiten.
                initializeNavigationService(rootFrame);

                // Verknüpfen Sie den Frame mit einem SuspensionManager-Schlüssel.
                SuspensionManager.RegisterFrame(rootFrame, "AppFrame");

                // TODO: diesen Wert auf eine Cachegröße ändern, die für Ihre Anwendung geeignet ist
                // siehe: https://msdn.microsoft.com/en-us/library/system.windows.controls.frame.cachesize%28v=vs.95%29.aspx
                rootFrame.CacheSize = 5;

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    // Den gespeicherten Sitzungszustand nur bei Bedarf wiederherstellen.
                    Debug.WriteLine("Previous State was terminated.");
                    try
                    {
                        // Prüfe, ob der Nutzer im Zustand LoggedIn, also in der Moderatorenansicht, war zum
                        // Zeitpunkt der Terminierung der App.
                        var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
                        if (localSettings.Values[Constants.ModeratorLoggedInStatusKey] != null)
                        {
                            int loginStatus = Convert.ToInt32(localSettings.Values[Constants.ModeratorLoggedInStatusKey]);

                            if (loginStatus == Constants.ModeratorNotLoggedIn)
                            {
                                Debug.WriteLine("Trying to restore state of the application.");
                                await SuspensionManager.RestoreAsync();
                            }
                            else
                            {
                                Debug.WriteLine("Moderator was logged in at time of app termination. Session was terminated. Don't " + 
                                    "try to restore state.");

                                // Setze Status auf NotLoggedIn.
                                localSettings.Values[Constants.ModeratorLoggedInStatusKey] = Constants.ModeratorNotLoggedIn;
                            }
                        }
                        else
                        {
                            // Zustand war noch nicht gesetzt. Setze ihn auf NotLoggedIn.
                            localSettings.Values[Constants.ModeratorLoggedInStatusKey] = Constants.ModeratorNotLoggedIn;
                            await SuspensionManager.RestoreAsync();
                        }
                    }
                    catch (SuspensionManagerException)
                    {
                        Debug.WriteLine("Error while trying to restore the app state after a termination.");
                        // Fehler beim Wiederherstellen des Zustands.
                        // Annehmen, dass kein Zustand vorhanden ist und Vorgang fortsetzen.
                    }
                }

                // Den Frame im aktuellen Fenster platzieren.
                Window.Current.Content = rootFrame;
            }
            else
            {
                // Erzeuge Instanz des Navigationsdiensts und lade die Seiten.
                initializeNavigationService(rootFrame);
            }
            
            // Wenn der Inhalt des RootFrames null ist, d.h. kein Content durch eine Wiederherstellung vorhanden ist.
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
                    // Prüfe, ob der Push Notification Manager bereits initialisiert ist und initialisiere falls notwendig.
                    await initializePushNotificationManagerAsync();

                    // Navigiere zum Homescreen.
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
        async void App_Resuming(object sender, object e)
        {
            Debug.WriteLine("In AppResuming EventHandler.");

            await initializePushNotificationManagerAsync();

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
                localSettings.Values[Constants.AccessToLockScreenKey] = Constants.AccessToLockScreenDenied;
                if (localSettings.Values[Constants.ShowLockScreenMessageKey] == null){
                    // Zeige Benachrichtigung, falls diese noch nicht gezeigt wurde. 
                    localSettings.Values[Constants.ShowLockScreenMessageKey] = Constants.ShowLockScreenMessageYes;
                }
            }
            else
            {
                Debug.WriteLine("Access to lock screen granted. Starting to register background tasks.");
                // Hintergrundaufgaben dürfen hinzu gefügt werden und erhalten das volle Ressourcen-Quota.
                // Registriere den BackgroundTask, der beim Empfang von RawPushNotifications gestartet wird.
                PushNotificationManagerBackground.PushNotificationManager.Register();
                // Registriere den BackgroundTask, der in einem bestimmten Wartungsintervall das Push Access Token (Kanal-URI) aktualisiert.
                PushNotificationManagerBackground.PushNotificationMaintenanceTask.Register();
                localSettings.Values[Constants.AccessToLockScreenKey] = Constants.AccessToLockScreenGranted;
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
                localUser = localUserController.GetLocalUser();
            }
            catch(ClientException ex){
                // TODO - How to handle this error?
                Debug.WriteLine("Error while trying to retrieve local user account. Error code is: {0}.", ex.ErrorCode);
            }

            if(localUser == null){
                return false;
            }
            return true;
        }

        /// <summary>
        /// Initialisiert den Navigationsdienst der Anwendung. Registiert
        /// alle Seiten der Anwendung beim Navigationsdienst.
        /// <param name="rootFrame">Der Frame, der vom Navigationsdienst verwendet werden soll.</param>
        /// </summary>
        private void initializeNavigationService(Frame rootFrame)
        {
            NavigationService = new UlmUniversityNews.NavigationService.NavigationService(rootFrame);
            NavigationService.RegisterPage("StartPage", typeof(StartPage));
            NavigationService.RegisterPage("Homescreen", typeof(Views.Homescreen.Homescreen));
            NavigationService.RegisterPage("ChannelSearch", typeof(Views.ChannelSearch.ChannelSearch));
            NavigationService.RegisterPage("ChannelDetails", typeof(Views.ChannelDetails.ChannelDetails));
            NavigationService.RegisterPage("ApplicationSettings", typeof(Views.ApplicationSettings.ApplicationSettings));
            NavigationService.RegisterPage("ChannelSettings", typeof(Views.ChannelDetails.ChannelSettings));
            NavigationService.RegisterPage("LoginPage", typeof(Views.Login.LoginPage));
            NavigationService.RegisterPage("HomescreenModerator", typeof(Views.ModeratorViews.Homescreen.HomescreenModerator));
            NavigationService.RegisterPage("ModeratorChannelDetails", typeof(Views.ModeratorViews.ChannelDetails.ModeratorChannelDetails));
            NavigationService.RegisterPage("AddAnnouncement", typeof(Views.ModeratorViews.AddAnnouncementDialog.AddAnnouncement));
            NavigationService.RegisterPage("AddAndEditChannel", typeof(Views.ModeratorViews.AddAndEditChannelDialog.AddAndEditChannel));
            NavigationService.RegisterPage("AddAndEditReminder", typeof(Views.ModeratorViews.AddAndEditReminderDialog.AddAndEditReminder));
            NavigationService.RegisterPage("ReminderDetails", typeof(Views.ModeratorViews.ReminderDetails.ReminderDetails));
            NavigationService.RegisterPage("AboutUniversityNews", typeof(Views.About.AboutUniversityNews));
            NavigationService.RegisterPage("AddAndEditGroup", typeof(Views.Group.AddAndEditGroupDialog.AddAndEditGroup));
            NavigationService.RegisterPage("SearchGroups", typeof(Views.Group.SearchGroups));
            NavigationService.RegisterPage("GroupDetails", typeof(Views.Group.GroupDetails));
            NavigationService.RegisterPage("ConversationDetails", typeof(Views.Group.ConversationDetails));
            NavigationService.RegisterPage("GroupSettings", typeof(Views.Group.GroupSettings));
            NavigationService.RegisterPage("AddAndEditConversation", typeof(Views.Group.AddAndEditConversationDialog.AddAndEditConversation));
            NavigationService.RegisterPage("BallotDetails", typeof(Views.Group.BallotDetails));
            NavigationService.RegisterPage("AddAndEditBallot", typeof(Views.Group.AddAndEditBallotDialog.AddAndEditBallot));
        }

        /// <summary>
        /// Diese Methode prüft, ob der PushNotificationManager bereits initialisiert und somit für den Empfang von Push Nachrichten
        /// bereit ist. Falls dies nicht der Fall ist, wird die Initialisierung angestoßen und das PushAccessToken des lokalen 
        /// Nutzers aktualisiert, falls notwendig.
        /// </summary>
        private async Task initializePushNotificationManagerAsync()
        {
            // Prüfe, ob PushNotificationManager initialisiert ist.
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            if (pushManager.IsInitialized() == false)
            {
                Debug.WriteLine("Need to initialize the push manager.");
                await pushManager.InitializeAsync();
                String channelURI = pushManager.GetChannelURIAsString();
                if(channelURI != null){
                    try
                    {
                        if(checkLocalUserExistence() == true)
                        {
                            // Aktualisiere das PushAccessToken falls notwendig.
                            await localUserController.UpdateLocalUserAsync(string.Empty, channelURI);
                        }
                    }
                    catch (ClientException ex)
                    {
                        Debug.WriteLine("Updating the push token of the local user has failed. Exception is with error code: " + ex.ErrorCode);
                        //  TODO Wie mit diesem Fall umgehen?
                    }
                }
            }
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

            //// Deregistrierung des PushNotificationManagers der App.
            //PushNotifications.PushNotificationManager.GetInstance().SuspendAppPushNotificationManager();

            await SuspensionManager.SaveAsync();
            deferral.Complete();
        }

        /// <summary>
        /// Wird aufgerufen, wenn innerhalb der Anwendung eine Exception aufgetreten ist,
        /// die nicht durch den Anwendungscode behandelt wurde.
        /// </summary>
        /// <param name="sender">Der Sender des Events.</param>
        /// <param name="e">Die Event Parameter.</param>
        void App_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Debug.WriteLine("Unhandled Exception in Application.");
            Debug.WriteLine("The exception message was {0}.", e.Message);
            Debug.WriteLine("The stack trace is: {0}.", e.Exception.StackTrace);
        }
    }
}
