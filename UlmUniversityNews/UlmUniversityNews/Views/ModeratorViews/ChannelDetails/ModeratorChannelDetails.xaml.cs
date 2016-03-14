using UlmUniversityNews.Common;
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
using DataHandlingLayer.ViewModel;
using UlmUniversityNews.PushNotifications.EventArgClasses;
using Windows.ApplicationModel.Core;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.ModeratorViews.ChannelDetails
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class ModeratorChannelDetails : Page
    {
        private NavigationHelper navigationHelper;
        private ModeratorChannelDetailsViewModel moderatorChannelDetailsViewModel;

        public ModeratorChannelDetails()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            moderatorChannelDetailsViewModel = new ModeratorChannelDetailsViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = moderatorChannelDetailsViewModel;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = moderatorChannelDetailsViewModel.DrawerMenuEntriesStatusLoggedIn;
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
            if (e.NavigationParameter != null)
            {
                int channelId = Convert.ToInt32(e.NavigationParameter);
                moderatorChannelDetailsViewModel.LoadSelectedChannel(channelId);
                await moderatorChannelDetailsViewModel.PerformAnnouncementUpdate();
                await moderatorChannelDetailsViewModel.LoadRemindersOfChannel();
                await moderatorChannelDetailsViewModel.CheckForMissingReminders();
                await moderatorChannelDetailsViewModel.LoadModeratorsOfChannel();
            }

            subscribeToPushManagerEvents();
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
            unsubscribeFromPushManagerEvents();
        }

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

        #region PushNotificationManagerEvents
        /// <summary>
        /// Abonniere für die ModeratorChannelDetails View relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die ChannelDetails View ihren Zustand aktualisieren.
        /// </summary>
        private void subscribeToPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement += pushManager_ReceivedAnnouncement;
        }

        /// <summary>
        /// Deabonniere alle Events des PushNotificationManager, für die sich die View registriert hat.
        /// </summary>
        private void unsubscribeFromPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement -= pushManager_ReceivedAnnouncement;
        }

        /// <summary>
        /// EventHandler für das ReceivedAnnouncement Event. Dieser EventHandler wird aufgerufen, wenn eine
        /// neue Announcement per PushNachricht empfangen wurde.
        /// </summary>
        /// <param name="sender">Der Sender des Events, hier der PushNotificationManager.</param>
        /// <param name="e">Die Eventparameter.</param>
        async void pushManager_ReceivedAnnouncement(object sender, AnnouncementReceivedEventArgs e)
        {
            if (moderatorChannelDetailsViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        // Aktualisiere View, wenn eigener Kanal betroffen.
                        if (moderatorChannelDetailsViewModel.Channel != null
                            && moderatorChannelDetailsViewModel.Channel.Id == e.ChannelId)
                        {
                            await moderatorChannelDetailsViewModel.UpdateAnnouncementsOnAnnouncementReceived();
                        }
                    });
            }
        }
        #endregion PushNotificationManagerEvents

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
    }
}
