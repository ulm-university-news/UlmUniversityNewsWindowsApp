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
using UlmUniversityNews.PushNotifications;
using Windows.ApplicationModel.Core;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.Group
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class GroupDetails : Page
    {
        private NavigationHelper navigationHelper;

        /// <summary>
        /// Referenz auf die ViewModel Instanz, die dieser Seite zugeordnet ist.
        /// </summary>
        private GroupDetailsViewModel groupDetailsViewModel;

        public GroupDetails()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            groupDetailsViewModel = new GroupDetailsViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = groupDetailsViewModel;

            // Initialiisierung des Drawer Menüs.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = groupDetailsViewModel.LoadDrawerMenuEntries();

            // Registriere Property-Changed Listener.
            groupDetailsViewModel.PropertyChanged += GroupDetailsViewModel_PropertyChanged;
            this.Loaded += GroupDetails_Loaded;
        }

        /// <summary>
        /// Workaround: Evaluiere Visibility von PivotItems zum Zeitpunkt, an dem 
        /// die Seite geladen ist.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupDetails_Loaded(object sender, RoutedEventArgs e)
        {
            // Stoße Evaluierung der PivotItem-Sichtbarkeiten an.
            forceBehaviorEvaluation();
        }

        /// <summary>
        /// Event-Handler, der aufgerufen wird, wenn das PropertyChanged Event vom ViewModel gefeuert wird.
        /// Wird hier für Workaround bezüglich HideablePivotItemBehavior verwendet.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GroupDetailsViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsGroupParticipant")
            {
                forceBehaviorEvaluation();
            }
        }

        /// <summary>
        /// Hilfsmethode, die einen Workaround bezüglich des HideablePivotItems ausführt. Das Problem ist,
        /// dass die Prüfung des Visible Attributs nicht zuverlässig bei PropertyChanged Events oder beim
        /// Laden der Seite angestoßen wird. Mittels dieser Methode lässt sich die Prüfung manuell anstoßen.
        /// </summary>
        private void forceBehaviorEvaluation()
        {
            Debug.WriteLine("Called forceBehaviorEvaluation.");

            // Workaround to call an update of the Visible attribute and thus force the evaluation of the
            // visibility status of the pivot item once againg when the pivot element is actually loaded.
            HidablePivotItemBehaviorElementConversations.ClearValue(HideablePivotItemBehavior.VisibleProperty);
            HidablePivotItemBehaviorElementConversations.Visible = groupDetailsViewModel.IsGroupParticipant;

            HidablePivotItemBehaviorElementEvents.ClearValue(HideablePivotItemBehavior.VisibleProperty);
            HidablePivotItemBehaviorElementEvents.Visible = groupDetailsViewModel.IsGroupParticipant;

            HidablePivotItemBehaviorElementBallots.ClearValue(HideablePivotItemBehavior.VisibleProperty);
            HidablePivotItemBehaviorElementBallots.Visible = groupDetailsViewModel.IsGroupParticipant;
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
                if (successful && GroupDetailsPivot != null)
                    GroupDetailsPivot.SelectedIndex = selectedIndex;
            }

            // Für den Typvergleich, siehe hier: http://stackoverflow.com/questions/983030/type-checking-typeof-gettype-or-is
            if (e.NavigationParameter != null && e.NavigationParameter.GetType() == typeof(string))
            {
               await groupDetailsViewModel.LoadGroupFromTemporaryCacheAsync(e.NavigationParameter as string);
            }
            else if (e.NavigationParameter != null && e.NavigationParameter.GetType() == typeof(int))
            {
                int groupId = Convert.ToInt32(e.NavigationParameter);
                await groupDetailsViewModel.LoadGroupFromLocalStorageAsync(groupId);

                // Lade Konversationen.
                await groupDetailsViewModel.LoadConversationsAsync(groupId);

                // Lade Abstimmungen.
                await groupDetailsViewModel.LoadBallotsAsync(groupId);
            }

            // Registriere Seite für relevante Push Notification Events.
            subscribeToPushManagerEvents();
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
            if (e.PageState != null && GroupDetailsPivot != null)
                e.PageState["PivotIndex"] = GroupDetailsPivot.SelectedIndex;

            // Deregistriere alle Push Notification Events.
            unsubscribeFromPushManagerEvents();

            // Setzte das HasNewEvent Flag zurück.
            if (groupDetailsViewModel != null)
            {
                groupDetailsViewModel.ResetHasNewEventFlag();
            }
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
        /// Abonniere für die GroupDetails relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die GroupDetails View ihren Zustand aktualisieren.
        /// </summary>
        private void subscribeToPushManagerEvents()
        {
            // Registriere PushNotification Events, die für die GroupDetails View von Interesse sind.
            PushNotificationManager pushManager = PushNotificationManager.GetInstance();
            pushManager.ReceivedConversationMessage += PushManager_ReceivedConversationMessage;
        }

        /// <summary>
        /// Deabonniere alle Events des PushNotificationManager, für die sich die View registriert hat.
        /// </summary>
        private void unsubscribeFromPushManagerEvents()
        {
            // Deregistriere PushNotification Events, die für die GroupDetails View von Interesse sind.
            PushNotificationManager pushManager = PushNotificationManager.GetInstance();
            pushManager.ReceivedConversationMessage -= PushManager_ReceivedConversationMessage;
        }

        /// <summary>
        /// Event-Handler, der ausgeführt wird, wenn vom PushNotificationManager ein
        /// ReceivedAnnouncement-Event verschickt wird.
        /// </summary>
        /// <param name="sender">Der Sender des Events, d.h. hier der PushNotificationManager.</param>
        /// <param name="e">Eventparameter.</param>
        private async void PushManager_ReceivedConversationMessage(object sender, PushNotifications.EventArgClasses.ConversationMessageNewEventArgs e)
        {
            if (groupDetailsViewModel != null)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, async () =>
                    {
                        await groupDetailsViewModel.UpdateNumberOfUnreadConversationMessagesAsync();
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

        /// <summary>
        /// Behandelt den Click auf den Beitreten-Button. Stößt die Anzeige
        /// des Passwort Feldes an. Mit der Eingabe des Passworts kann man 
        /// den Request zum Beitreten zur Gruppe absetzen.
        /// </summary>
        /// <param name="sender">Die Eventquelle.</param>
        /// <param name="e">Die Eventparameter.</param>
        private async void GroupDetailsJoinGroupButton_Click(object sender, RoutedEventArgs e)
        {
            await GroupDetailsPasswordEntryDialog.ShowAsync();
        }

        /// <summary>
        /// Behandelt Änderung des aktiven Pivotelements.
        /// </summary>
        /// <param name="sender">Die Ereignisquelle.</param>
        /// <param name="e">Die Ereignisparameter.</param>
        private void GroupDetailsPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Pivot pivotElement = this.GroupDetailsPivot;
            PivotItem selectedItem = pivotElement.SelectedItem as PivotItem;

            if (selectedItem != null)
            {
                groupDetailsViewModel.SelectedPivotItemName = selectedItem.Name;
            }
        }
    }
}
