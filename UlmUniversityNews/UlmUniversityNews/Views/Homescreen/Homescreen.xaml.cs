﻿using UlmUniversityNews.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Graphics.Display;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using DataHandlingLayer.ViewModel;
using UlmUniversityNews.PushNotifications;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Constants;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.Homescreen
{
    /// <summary>
    /// Der Homescreen der App. Es werden die Kanäle, Gruppen und Anwendungseinstellungen des Nutzers über den Homescreen verfügbar gemacht werden.
    /// </summary>
    public sealed partial class Homescreen : Page
    {
        private NavigationHelper navigationHelper;

        private HomescreenViewModel homescreenViewModel;

        public Homescreen()
        {
            Debug.WriteLine("Starting constructor of Homescreen.");
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            HomescreenPivot.Loaded += HomescreenPivot_Loaded;

            // Diese Seite soll wenn möglich im Cache gehalten werden.
            NavigationCacheMode = NavigationCacheMode.Enabled;

            // Initialisiere Homescreen ViewModel.
            homescreenViewModel = new HomescreenViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = homescreenViewModel;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            List<DrawerMenuEntry> test = homescreenViewModel.DrawerMenuEntriesStatusNoLogin;
            ListMenuItems.ItemsSource = test;

            // Registriere Loaded und Unloaded Events.
            this.Loaded += Homescreen_Loaded;
            this.Unloaded += Homescreen_Unloaded;

            Debug.WriteLine("Finished constructor of Homescreen.");
        }

        /// <summary>
        /// Wird gerufen, wenn die Seite erfolgreich aus dem Speicher genommen wurde.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void Homescreen_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Homescreen: Unloaded.");

            // Deregistrierung, wenn Seite verlassen wird.
            Application.Current.Resuming -= Current_Resuming;
        }

        /// <summary>
        /// Wird gerufen, wenn die Seite erfolgreich geladen wurde.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void Homescreen_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Homescreen: Loaded.");

            // Registrierung für Behandlung von Resuming Events, um View nach Fortsetzung zu aktualisieren.
            Application.Current.Resuming += Current_Resuming;
        }

        /// <summary>
        /// Wird gerufen, wenn App aus Suspension-Zustand zurückkommt.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private async void Current_Resuming(object sender, object e)
        {
            Debug.WriteLine("Homescreen: App resuming ... ");
            if (HomescreenPivot != null && homescreenViewModel != null)
            {
                if (HomescreenPivot.SelectedIndex == 0)
                {
                    Debug.WriteLine("Homescreen: Case 'My channels': ");
                    await homescreenViewModel.UpdateNumberOfUnreadAnnouncementsAsync();
                    Debug.WriteLine("Homescreen: Performed update.");
                }
                else if (HomescreenPivot.SelectedIndex == 1)
                {
                    Debug.WriteLine("Homescreen: Case 'My groups': ");
                    await homescreenViewModel.GroupCollectionUpdateOnAppResumingAsync();
                    Debug.WriteLine("Homescreen: Performed update.");
                }
            }
        }

        /// <summary>
        /// Event-Handler für das Loaded Event des Pivot-Elements. Wird aufgerufen wenn das Pivot UI Element geladen wurde.
        /// Wird hier genutzt um zu prüfen, ob der Zugriff auf den LockScreen gewährt wurde.
        /// </summary>
        /// <param name="sender">Quelle des Events.</param>
        /// <param name="e">Eventparameter.</param>
        async void HomescreenPivot_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Homescreen_Pivot: Loaded.");
            // Prüfe, ob Zugriff auf LockScreen gewährt ist, um Hintergrundaufgaben ausführen zu dürfen.
            await checkLockScreenAccessPermissionAsync();
        }

        /// <summary>
        /// Ruft den <see cref="NavigationHelper"/> ab, der mit dieser <see cref="Page"/> verknüpft ist.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Füllt die Seite mit Inhalt auf, der bei der Navigation übergeben wird.  Gespeicherte Zustände werden ebenfalls
        /// bereitgestellt, wenn eine Seite aus einer vorherigen Sitzung neu erstellt wird.
        /// </summary>
        /// <param name="sender">
        /// Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/>
        /// </param>
        /// <param name="e">Ereignisdaten, die die Navigationsparameter bereitstellen, die an
        /// <see cref="Frame.Navigate(Type, Object)"/> als diese Seite ursprünglich angefordert wurde und
        /// ein Wörterbuch des Zustands, der von dieser Seite während einer früheren
        /// beibehalten wurde.  Der Zustand ist beim ersten Aufrufen einer Seite NULL.</param>
        private async void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            Debug.WriteLine("Homescreen: In LoadState.");

            // Erforderlich wegen Caching. Falls Seite aus Cache geladen wird und Drawer war offen
            // bleibt er sonst offen.
            if (DrawerLayout.IsDrawerOpen)
            {
                DrawerLayout.CloseDrawer();
            }

            // Registriere für Homescreen View relevante Events des PushNotificationManagers.
            subscribeToPushManagerEvents();

            // Lade "Meine Kanäle"
            await homescreenViewModel.LoadMyChannelsAsync();

            // Lade "Meine Gruppen"
            await homescreenViewModel.LoadMyGroupsAsync();
        }

        /// <summary>
        /// Behält den dieser Seite zugeordneten Zustand bei, wenn die Anwendung angehalten oder
        /// die Seite im Navigationscache verworfen wird.  Die Werte müssen den Serialisierungsanforderungen
        /// von <see cref="SuspensionManager.SessionState"/> entsprechen.
        /// </summary>
        /// <param name="sender">Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/></param>
        /// <param name="e">Ereignisdaten, die ein leeres Wörterbuch zum Auffüllen bereitstellen
        /// serialisierbarer Zustand.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            Debug.WriteLine("Homescreen: In SaveState.");

            // Deregistriere von PushNotificationEvents beim Verlassen der Seite.
            unsubscribeFromPushManagerEvents();
        }

        #region PushNotificationManagerEvents
        /// <summary>
        /// Abonniere für die HomescreenView relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die Homescreen View ihren Zustand aktualisieren.
        /// </summary>
        private void subscribeToPushManagerEvents()
        {
            // Registriere PushNotification Events, die für die Homescreen View von Interesse sind.
            PushNotificationManager pushManager = PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement += pushManager_ReceivedAnnouncement;
            pushManager.ChannelDeleted += pushManager_ChannelDeleted;
            pushManager.ChannelChanged += pushManager_ChannelChanged;
            pushManager.GroupDeleted += pushManager_GroupUpdateRequired;
            pushManager.ParticipantNew += pushManager_GroupUpdateRequired;
            pushManager.ParticipantLeft += pushManager_GroupUpdateRequired;
            pushManager.ConversationNew += pushManager_GroupUpdateRequired;
            pushManager.ConversationDeleted += pushManager_GroupUpdateRequired;
            pushManager.ReceivedConversationMessage += pushManager_GroupUpdateRequired;
            pushManager.BallotNew += pushManager_GroupUpdateRequired;
            pushManager.BallotOptionNew += pushManager_GroupUpdateRequired;
            pushManager.BallotOptionVote += pushManager_GroupUpdateRequired;
            pushManager.BallotDeleted += pushManager_GroupUpdateRequired;
        }

        /// <summary>
        /// Deabonniere alle Events des PushNotificationManager, für die sich die View registriert hat.
        /// </summary>
        private void unsubscribeFromPushManagerEvents()
        {
            // Deregistriere PushNotification Events, die für die Homescreen View von Interesse sind.
            PushNotificationManager pushManager = PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement -= pushManager_ReceivedAnnouncement;
            pushManager.ChannelDeleted -= pushManager_ChannelDeleted;
            pushManager.ChannelChanged -= pushManager_ChannelChanged;
            pushManager.GroupDeleted -= pushManager_GroupUpdateRequired;
            pushManager.ParticipantNew -= pushManager_GroupUpdateRequired;
            pushManager.ParticipantLeft -= pushManager_GroupUpdateRequired;
            pushManager.ConversationNew -= pushManager_GroupUpdateRequired;
            pushManager.ConversationDeleted -= pushManager_GroupUpdateRequired;
            pushManager.ReceivedConversationMessage -= pushManager_GroupUpdateRequired;
            pushManager.BallotNew -= pushManager_GroupUpdateRequired;
            pushManager.BallotOptionNew -= pushManager_GroupUpdateRequired;
            pushManager.BallotOptionVote -= pushManager_GroupUpdateRequired;
            pushManager.BallotDeleted -= pushManager_GroupUpdateRequired;
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein
        /// ReceivedAnnouncement-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_ReceivedAnnouncement(object sender, EventArgs e)
        {
            if (homescreenViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await homescreenViewModel.UpdateNumberOfUnreadAnnouncementsAsync();
                    });
            }
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein 
        /// ChannelDeleted-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_ChannelDeleted(object sender, PushNotifications.EventArgClasses.ChannelDeletedEventArgs e)
        {
            if (homescreenViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        homescreenViewModel.PerformViewUpdateOnChannelDeletedEvent(e.ChannelId);
                    });
            }
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein 
        /// ChannelChanged-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_ChannelChanged(object sender, PushNotifications.EventArgClasses.ChannelChangedEventArgs e)
        {
            if (homescreenViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await homescreenViewModel.PerformViewUpdateOnChannelChangedEventAsync(e.ChannelId);
                    });
            }
        }

        /// <summary>
        /// Event-Handler, der von verschiedenen gruppen-basierten Events genutzt werden kann. Der Handler
        /// stößt die Aktualisierung der vom Event betroffenen Gruppe an.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_GroupUpdateRequired(object sender, PushNotifications.EventArgClasses.GroupRelatedEventArgs e)
        {
            if (homescreenViewModel != null)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        homescreenViewModel.UpdateIndividualGroupInCollection(e.GroupId);
                    });
            }
        }
        #endregion PushNotificationManagerEvents

        #region NavigationHelper-Registrierung

        /// <summary>
        /// Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet, um
        /// damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
        /// <para>
        /// Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
        /// <see cref="NavigationHelper.LoadState"/>
        /// und <see cref="NavigationHelper.SaveState"/>.
        /// Der Navigationsparameter ist in der LoadState-Methode verfügbar 
        /// zusätzlich zum Seitenzustand, der während einer früheren Sitzung beibehalten wurde.
        /// </para>
        /// </summary>
        /// <param name="e">Stellt Daten für Navigationsmethoden und -ereignisse bereit.
        /// Handler, bei denen die Navigationsanforderung nicht abgebrochen werden kann.</param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedTo(e);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            this.navigationHelper.OnNavigatedFrom(e);
        }

        #endregion

        /// <summary>
        /// Behandelt Klick Events für das Drawer-Layout. Das Menü wird mittels eines Klicks
        /// auf das Drawer Icon abhängig vom aktuellen Zustand ein oder ausgeklappt.
        /// </summary>
        /// <param name="sender">Die Quelle des Ereignisses, d.h. ein Klick auf das Drawer Icon.</param>
        /// <param name="e">Ereignisdaten</param>
        private void DrawerIcon_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (DrawerLayout.IsDrawerOpen)
            {
                DrawerLayout.CloseDrawer();
            }
            else
            {
                DrawerLayout.OpenDrawer();
            }
        }

        # region checkLockScreenAccessContentDialog
        /// <summary>
        /// Prüft, ob der Zugriff auf den LockScreen durch das System gewährt wurde. Falls das nicht der Fall ist wird
        /// ein Dialog mit einem Warnhinweis angezeigt. Der Nutzer kann jedoch bestimmen, dass der Dialog nicht mehr angezeigt 
        /// werden soll. In diesem Fall wird der Dialog auch nicht mehr angezeigt.
        /// </summary>
        private async Task checkLockScreenAccessPermissionAsync(){
            Debug.WriteLine("Start checking LockScreen access permission in local settings.");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Prüfe, ob der Zugriff abgelehnt wurde.
            string accessToLockScreenValue = localSettings.Values[Constants.AccessToLockScreenKey] as string;
            string accessDenied = Constants.AccessToLockScreenDenied;
            if (String.Compare(accessToLockScreenValue, accessDenied) == 0)
            {
                // Prüfe, ob der Nachrichtendialog noch angezeigt werden soll.
                string showLockScreenValue = localSettings.Values[Constants.ShowLockScreenMessageKey] as string;
                string showMessage = Constants.ShowLockScreenMessageYes;
                if (String.Compare(showLockScreenValue, showMessage) == 0){
                    // Zeige die Warnung an.
                    await showLockScreenAccessDeniedWarningAsync();
                }
            }
        }

        /// <summary>
        /// Zeigt einen ContentDialog mit einer Warnung bezüglich der Ablehnung des LockScreen Zugriffs an.
        /// Der Nutzer hat die Möglichkeit das zukünftige Anzeigen dieses Dialogs zu unterbinden.
        /// </summary>
        private async Task showLockScreenAccessDeniedWarningAsync()
        {
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForCurrentView("Resources");
            try
            {
                // Generiere den Inhalt des ContentDialog.
                StackPanel sp = new StackPanel();
                TextBlock contentDialogTxt = new TextBlock()
                {
                    Text = loader.GetString("lockScreenAccessDeniedContentDialogText"),
                    Style = App.Current.Resources["ContentDialogTextStyle"] as Style
                };
                CheckBox doNotShowAgainCheckBox = new CheckBox() 
                {
                    Content = loader.GetString("doNotShowAgainCheckBoxText")
                };
                sp.Children.Add(contentDialogTxt);
                sp.Children.Add(doNotShowAgainCheckBox);

                // Erzeuge Dialog und zeige ihn mit Inhalt an.
                ContentDialog lockAccessDeniedCD = new ContentDialog()
                {
                    Title = loader.GetString("lockScreenAccessDeniedContentDialogTitle"),
                    PrimaryButtonText = "OK",
                    FullSizeDesired = false,
                    Content = sp
                };

                var result = await lockAccessDeniedCD.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Prüfe, ob der Dialog in Zukunft nicht mehr angezeigt werden soll.
                    if(doNotShowAgainCheckBox.IsChecked == true){
                        Debug.WriteLine("Disable the lockAccessDeniedCD so that it is not shown in the future.");
                        disableLockScreenAccessDeniedWarning();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception occured: Message is: {0}, Hresult is: {1} and stack-trace is: {2}.", ex.Message, ex.HResult, ex.StackTrace);
            }
        }

        /// <summary>
        /// Aktualisiert die lokalen Einstellungen, so dass der Warnhinweis bezüglich der Ablehnung des Zugriffs
        /// auf den LockScreen nicht mehr angezeigt wird. 
        /// </summary>
        private void disableLockScreenAccessDeniedWarning()
        {
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            localSettings.Values[Constants.ShowLockScreenMessageKey] = Constants.ShowLockScreenMessageNo;
        }
        # endregion checkLockScreenAccessContentDialog

    }
}
