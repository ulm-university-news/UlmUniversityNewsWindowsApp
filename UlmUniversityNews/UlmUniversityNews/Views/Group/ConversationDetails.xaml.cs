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
using DataHandlingLayer.ViewModel;
using UlmUniversityNews.PushNotifications;
using Windows.ApplicationModel.Core;
using System.Diagnostics;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.Group
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class ConversationDetails : Page
    {
        private NavigationHelper navigationHelper;
        private ConversationDetailsViewModel conversationDetailsViewModel;

        public ConversationDetails()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            conversationDetailsViewModel = new ConversationDetailsViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = conversationDetailsViewModel;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = conversationDetailsViewModel.LoadDrawerMenuEntries();

            // Registriere Seite für Loaded und Unloaded Events.
            this.Loaded += ConversationDetails_Loaded;
            this.Unloaded += ConversationDetails_Unloaded;
        }

        /// <summary>
        /// Event-Handler für die Behandlung des Unloaded-Events. Wird gerufen, wenn Seite
        /// erfolgreich aus Speicher genommen wurde.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void ConversationDetails_Unloaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ConversationDetails: Unloaded.");

            // Deregistrierung, wenn Seite verlassen wird.
            Application.Current.Resuming -= Current_Resuming;
        }

        /// <summary>
        /// Event-Handler für die Behandlung des Loaded-Events. Wird gerufen, wenn Seite
        /// erfolgreich geladen wurde.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void ConversationDetails_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("ConversationDetails: Loaded.");

            // Registrierung für Behandlung von Resuming Events, um View nach Fortsetzung zu aktualisieren.
            Application.Current.Resuming += Current_Resuming;
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
                int conversationId = Convert.ToInt32(e.NavigationParameter);
                await conversationDetailsViewModel.LoadSelectedConversationAsync(conversationId);
            }

            // Registrierung für View relevante Push Notification Events.
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
            // Markiere ConversationMessages als gelesen.
            conversationDetailsViewModel.MarkConversationMessagesAsRead();

            // Deregistriere alle PushNotification Events.
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
        /// Abonniere für die ConverstionDetails relevante Events, die vom PushNotificationManager bereitgestellt werden.
        /// Beim Empfangen dieser Events wird die ConversationDetails View ihren Zustand aktualisieren.
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
        private async void PushManager_ReceivedConversationMessage(object sender, PushNotifications.EventArgClasses.ConversationRelatedEventArgs e)
        {
            if (conversationDetailsViewModel != null && 
                conversationDetailsViewModel.SelectedConversation != null && 
                conversationDetailsViewModel.SelectedConversation.Id == e.ConversationId)
            {
                // Ausführung auf UI-Thread abbilden.
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                    {
                        conversationDetailsViewModel.UpdateConversationMessagesCollection();
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
        /// Wird gerufen, wenn App aus Suspension-Zustand zurückkommt.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void Current_Resuming(object sender, object e)
        {
            Debug.WriteLine("ConversationDetails: App resuming.");

            if (conversationDetailsViewModel != null &&
                conversationDetailsViewModel.SelectedConversation != null)
            {
                conversationDetailsViewModel.UpdateConversationMessagesCollection();
            }
        }

        /// <summary>
        /// Event-Handler, der gerufen wird, wenn die TextBox ConversationDetailsMessageInputBox vom Nutzer selektiert wird.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void ConversationDetailsMessageInputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ConversationDetailsSynchronizeButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Event-Handler, der gerufen wird, wenn die TextBox ConversationDetailsMessageInputBox den
        /// Fokus wieder verliert, d.h. nicht mehr selektiert ist.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void ConversationDetailsMessageInputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConversationDetailsSynchronizeButton.Visibility = Visibility.Visible;
        }
    }
}
