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
using System.Diagnostics;
using Windows.ApplicationModel.Core;
using UlmUniversityNews.PushNotifications.EventArgClasses;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.ChannelDetails
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class ChannelDetails : Page
    {
        private NavigationHelper navigationHelper;

        /// <summary>
        /// Eine Referenz auf die ViewModel Klasse ChannelDetailsViewModel.
        /// </summary>
        private ChannelDetailsViewModel channelDetailsViewModel;

        public ChannelDetails()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            ChannelDetailsPivot.Loaded += ChannelDetailsPivot_Loaded;
            this.Loaded += ChannelDetails_Loaded;
            
            channelDetailsViewModel = new ChannelDetailsViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = channelDetailsViewModel;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = channelDetailsViewModel.DrawerMenuEntriesStatusNoLogin;

            channelDetailsViewModel.PropertyChanged += channelDetailsViewModel_PropertyChanged;
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Seite ChannelDetails geladen wurde. 
        /// Zeigt die Seite aktuell einen abonnierten Kanal an, so wird ein
        /// Aktualisierungsrequest für die Announcements des Kanals abgesetzt.
        /// </summary>
        /// <param name="sender">Der Sender des Loaded Events.</param>
        /// <param name="e">Die Eventparameter.</param>
        async void ChannelDetails_Loaded(object sender, RoutedEventArgs e)
        {
            if(channelDetailsViewModel != null && channelDetailsViewModel.ChannelSubscribedStatus == true)
            {
                await channelDetailsViewModel.PerformAnnouncementUpdateAsync();

                // Prüfe, ob der Kanal gelöscht wurde und zeige falls notwendig eine Benachrichtigung an.
                channelDetailsViewModel.CheckWhetherChannelIsDeleted();
            }
            await channelDetailsViewModel.LoadModeratorsOfChannelAsync();
        }

        /// <summary>
        /// Event-Handler, der aufgerufen wird, wenn das PropertyChanged Event vom ViewModel gefeuert wird.
        /// Wird hier für Workaround bezüglich HideablePivotItemBehavior verwendet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void channelDetailsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ChannelSubscribedStatus")
            {
                // Workaround to call an update of the Visible attribute and thus force the evaluation of the
                // visibility status of the pivot item once againg when the pivot element is actually loaded.
                HidablePivotItemBehaviorElement.ClearValue(HideablePivotItemBehavior.VisibleProperty);
                HidablePivotItemBehaviorElement.Visible = channelDetailsViewModel.ChannelSubscribedStatus;
            }
        }

        /// <summary>
        /// Event-Handler, der aufgerufen wird, wenn das PivotElement geladen wurde.
        /// Wird hier für Workaround benötigt bezüglich HidablePivotItemBehavior.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void ChannelDetailsPivot_Loaded(object sender, RoutedEventArgs e)
        {
            if(channelDetailsViewModel != null)
            {
                // Workaround to call an update of the Visible attribute and thus force the evaluation of the
                // visibility status of the pivot item once againg when the pivot element is actually loaded.
                HidablePivotItemBehaviorElement.ClearValue(HideablePivotItemBehavior.VisibleProperty);
                HidablePivotItemBehaviorElement.Visible = channelDetailsViewModel.ChannelSubscribedStatus;
            }
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
        private void NavigationHelper_LoadState(object sender, LoadStateEventArgs e)
        {
            //Debug.WriteLine("In LoadState of ChannelDetails page. NavigationParameter is: {0}.", e.NavigationParameter);

            // Prüfe, ob ein Index gespeichert ist, der angibt, auf welchem PivotItem der Nutzer zuletzt war.
            if (e.PageState != null && e.PageState["PivotIndex"] != null)
            {
                int selectedIndex = 0;
                bool successful = int.TryParse(e.PageState["PivotIndex"].ToString(), out selectedIndex);

                // Falls es einen gespeicherten PivotIndex gibt, setze ihn wieder aktiv.
                if (successful && ChannelDetailsPivot != null)
                    ChannelDetailsPivot.SelectedIndex = selectedIndex;
            }

            if(e.NavigationParameter != null)
            {
                // Lade den Zustand im ViewModel mit dem übergebenen Parameterwert.
                int selectedChannelId;
                bool successful = int.TryParse(e.NavigationParameter.ToString(), out selectedChannelId);
                if (successful)
                {
                    channelDetailsViewModel.LoadSelectedChannel(selectedChannelId);
                }
       
                // Registriere View für PushNotificationManager Events.
                subscribeToPushManagerEvents();
            }
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
            Debug.WriteLine("In SaveState of ChannelDetails page.");
            if(channelDetailsViewModel.ChannelSubscribedStatus)
            {
                // Markiere die Nachrichten dieses Kanals nun als gelsen.
                channelDetailsViewModel.MarkAnnouncementsAsRead();
            }

            // Speichere Pivot-Index zwischen, so dass man ihn beim nächsten Aufruf der Seite wieder aktiv setzen kann.
            if (e.PageState != null && ChannelDetailsPivot != null)
                e.PageState["PivotIndex"] = ChannelDetailsPivot.SelectedIndex;

            // Deregistriere View von PushNotificationManager Events.
            unsubscribeFromPushManagerEvents();
        }

        #region PushNotificationManagerEvents
        /// <summary>
        /// Abonniere für die ChannelDetails View relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die ChannelDetails View ihren Zustand aktualisieren.
        /// </summary>
        private void subscribeToPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement += pushManager_ReceivedAnnouncement;
            pushManager.ChannelDeleted += pushManager_ChannelDeleted;
            pushManager.ChannelChanged += pushManager_ChannelChanged;
        }

        /// <summary>
        /// Deabonniere alle Events des PushNotificationManager, für die sich die View registriert hat.
        /// </summary>
        private void unsubscribeFromPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.ReceivedAnnouncement -= pushManager_ReceivedAnnouncement;
            pushManager.ChannelDeleted -= pushManager_ChannelDeleted;
            pushManager.ChannelChanged -= pushManager_ChannelChanged;
        }

        /// <summary>
        /// EventHandler für das ReceivedAnnouncement Event. Dieser EventHandler wird aufgerufen, wenn eine
        /// neue Announcement per PushNachricht empfangen wurde.
        /// </summary>
        /// <param name="sender">Der Sender des Events, hier der PushNotificationManager.</param>
        /// <param name="e">Die Eventparameter.</param>
        async void pushManager_ReceivedAnnouncement(object sender, AnnouncementReceivedEventArgs e)
        {
            if(channelDetailsViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        // Aktualisiere View, wenn eigener Kanal betroffen.
                        if(channelDetailsViewModel.Channel != null 
                            && channelDetailsViewModel.Channel.Id == e.ChannelId)
                        {
                            await channelDetailsViewModel.UpdateAnnouncementsOnAnnouncementReceivedAsync();
                        }
                    });
            }
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein 
        /// ChannelDeleted-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_ChannelDeleted(object sender, ChannelDeletedEventArgs e)
        {
            if (channelDetailsViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        // Aktualisiere View, wenn eigener Kanal betroffen.
                        if (channelDetailsViewModel.Channel != null
                            && channelDetailsViewModel.Channel.Id == e.ChannelId)
                        {
                            channelDetailsViewModel.PerformViewUpdateOnChannelDeletedEvent();
                        }
                    });
            }
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein 
        /// ChannelChanged-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        async void pushManager_ChannelChanged(object sender, ChannelChangedEventArgs e)
        {
            if(channelDetailsViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        // Aktualisiere View, wenn eigener Kanal betroffen.
                        if(channelDetailsViewModel.Channel != null 
                            && channelDetailsViewModel.Channel.Id == e.ChannelId)
                        {
                            await channelDetailsViewModel.PerformViewUpdateOnChannelChangedEventAsync();
                        }
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

    }
}
