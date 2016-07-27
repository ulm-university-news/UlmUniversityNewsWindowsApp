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
using Windows.ApplicationModel.Core;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.Group
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class BallotDetails : Page
    {
        private NavigationHelper navigationHelper;

        /// <summary>
        /// Referenz auf die ViewModel Instanz, die dieser View zugeordnet ist.
        /// </summary>
        private BallotDetailsViewModel ballotDetailsViewModel;

        public BallotDetails()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            ballotDetailsViewModel = new BallotDetailsViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = ballotDetailsViewModel;

            // Initialisiere Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = ballotDetailsViewModel.LoadDrawerMenuEntries();
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
            // Prüfe, ob ein Index gespeichert ist, der angibt, auf welchem PivotItem der Nutzer zuletzt war.
            if (e.PageState != null &&
                e.PageState.Keys.Contains("PivotIndex") &&
                e.PageState["PivotIndex"] != null)
            {
                int selectedIndex = 0;
                bool successful = int.TryParse(e.PageState["PivotIndex"].ToString(), out selectedIndex);

                // Falls es einen gespeicherten PivotIndex gibt, setze ihn wieder aktiv.
                if (successful && BallotDetailsPivot != null)
                    BallotDetailsPivot.SelectedIndex = selectedIndex;
            }

            int groupId = -1;
            int ballotId = -1;
            if (e.NavigationParameter != null)
            {
                string navParam = e.NavigationParameter as string;
                string[] navParams = navParam.Split(new string[] { "?", "=" }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < navParams.Length; i++)
                {
                    if (navParams[i] == "groupId" && i + 1 < navParams.Length)
                    {
                        int.TryParse(navParams[i + 1], out groupId);
                    }
                    else if (navParams[i] == "ballotId" && i + 1 < navParams.Length)
                    {
                        int.TryParse(navParams[i + 1], out ballotId);
                    }
                }
            }

            if (ballotId != -1 && groupId != -1)
            {
                await ballotDetailsViewModel.LoadBallotAsync(groupId, ballotId);

                subscribeToPushManagerEvents();
            }
        }

        /// <summary>
        /// Behält den dieser Seite zugeordneten Zustand bei, wenn die Anwendung angehalten oder
        /// die Seite im Navigationscache verworfen wird. Die Werte müssen den Serialisierungsanforderungen
        /// von <see cref="SuspensionManager.SessionState"/> entsprechen.
        /// </summary>
        /// <param name="sender">Die Quelle des Ereignisses, normalerweise <see cref="NavigationHelper"/></param>
        /// <param name="e">Ereignisdaten, die ein leeres Wörterbuch zum Auffüllen bereitstellen
        /// serialisierbarer Zustand.</param>
        private void NavigationHelper_SaveState(object sender, SaveStateEventArgs e)
        {
            // Speichere Pivot-Index zwischen, so dass man ihn beim nächsten Aufruf der Seite wieder aktiv setzen kann.
            if (e.PageState != null && BallotDetailsPivot != null)
                e.PageState["PivotIndex"] = BallotDetailsPivot.SelectedIndex;

            unsubscribeFromPushManagerEvents();
        }

        #region NavigationHelper-Registrierung

        /// <summary>
        /// Die in diesem Abschnitt bereitgestellten Methoden werden einfach verwendet,
        /// damit NavigationHelper auf die Navigationsmethoden der Seite reagieren kann.
        /// <para>
        /// Platzieren Sie seitenspezifische Logik in Ereignishandlern für  
        /// <see cref="NavigationHelper.LoadState"/>
        /// und <see cref="NavigationHelper.SaveState"/>.
        /// Der Navigationsparameter ist in der LoadState-Methode zusätzlich 
        /// zum Seitenzustand verfügbar, der während einer früheren Sitzung gesichert wurde.
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
        /// Abonniere für die BallotDetails View relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die BallotDetails View ihren Zustand aktualisieren.
        /// </summary>
        private void subscribeToPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.BallotOptionNew += pushManager_ReloadBallotDetailsView;
            pushManager.BallotOptionVote += pushManager_ReloadBallotDetailsView;
        }

        /// <summary>
        /// Deabonniere alle Events des PushNotificationManager, für die sich die View registriert hat.
        /// </summary>
        private void unsubscribeFromPushManagerEvents()
        {
            PushNotifications.PushNotificationManager pushManager = PushNotifications.PushNotificationManager.GetInstance();
            pushManager.BallotOptionNew -= pushManager_ReloadBallotDetailsView;
            pushManager.BallotOptionVote -= pushManager_ReloadBallotDetailsView;
        }

        /// <summary>
        /// Behandelt abstimmungsbezogene Events, die eine Aktualisierung der Detailsseite erfordern.
        /// Wenn es sich bei der betroffenen Gruppe um die aktuell angezeigte Gruppe handelt und 
        /// die geladene Abstimmung der betroffenen Abstimmung entspricht, so wird diese Aktualisierung angestoßen.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        private async void pushManager_ReloadBallotDetailsView(object sender, PushNotifications.EventArgClasses.BallotRelatedEventArgs e)
        {
            if (ballotDetailsViewModel != null &&
                ballotDetailsViewModel.AffectedGroup != null && ballotDetailsViewModel.SelectedBallot != null &&
                ballotDetailsViewModel.AffectedGroup.Id == e.GroupId && 
                ballotDetailsViewModel.SelectedBallot.Id == e.BallotId)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await ballotDetailsViewModel.LoadBallotAsync(e.GroupId, e.BallotId);
                    });
            }
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
