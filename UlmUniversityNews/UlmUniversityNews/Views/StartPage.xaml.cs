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
using Windows.UI;
using DataHandlingLayer.ViewModel;
using DataHandlingLayer.Exceptions;
using UlmUniversityNews.PushNotifications;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class StartPage : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        private LocalUserViewModel localUserViewModel;

        public StartPage()
        {
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            // Erzeuge Instanz auf das ViewModel für den lokalen Nutzer.
            localUserViewModel = new LocalUserViewModel();
        }

        /// <summary>
        /// Ruft den <see cref="NavigationHelper"/> ab, der mit dieser <see cref="Page"/> verknüpft ist.
        /// </summary>
        public NavigationHelper NavigationHelper
        {
            get { return this.navigationHelper; }
        }

        /// <summary>
        /// Ruft das Anzeigemodell für diese <see cref="Page"/> ab.
        /// Dies kann in ein stark typisiertes Anzeigemodell geändert werden.
        /// </summary>
        public ObservableDictionary DefaultViewModel
        {
            get { return this.defaultViewModel; }
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

        /// <summary>
        /// Event-Handler für die Behandlung des Klicks auf den Create Account Button.
        /// </summary>
        /// <param name="sender">Der Auslöser des Events.</param>
        /// <param name="e">Eventparameter.</param>
        private async void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            // Validiere die eingegebenen Daten.
            string name = UserName.Text;
            if(name.Length < 3 || name.Length > 35){
                UserName.SelectionHighlightColor = new SolidColorBrush(Colors.Red);
                ErrorText.Text = "Bitte geben Sie einen gültigen Nutzernamen ein. Der Nutzername muss mindestens 3 Zeichen " +
                    " umfassen und darf nicht mehr als 35 Zeichen enthalten. Der Name darf zudem keine Leerzeichen und Sonderzeichen enthalten.";
            }
            else
            {
                bool forwardToHomescreen = true;

                // Initialisiere den Push Notification Manager.
                PushNotificationManager pushManager = PushNotificationManager.GetInstance();
                await pushManager.InitializeAsync();

                // Frage die Kanal-URI des Kanals für Push Nachrichten ab. Dieser dient als push access token für den lokalen Nutzer.
                String pushAccessToken = pushManager.GetChannelURIAsString();
                if(pushAccessToken != null){
                    // Erstelle einen lokalen Nutzeraccount.
                    try
                    {
                        await localUserViewModel.CreateLocalUserAsync(name, pushAccessToken);
                    }
                    catch (ClientException ex)
                    {
                        forwardToHomescreen = false;

                        // Zeige Fehler in einem MessageDialog an.
                        string errorDescription = ErrorHandling.ErrorDescriptionMapper.GetInstance().GetErrorDescription(ex.ErrorCode); ;
                        string title = "Error";
                        showErrorMessageDialog(errorDescription, title);

                        //Flyout flyout = (Flyout) this.Resources["ErrorFlyout"];
                        //ErrorFlyoutMessage.Text = ErrorHandling.ErrorDescriptionMapper.GetInstance().GetErrorDescription(ex.ErrorCode);
                        //flyout.ShowAt(this.LayoutRoot);
                    }
                }
                else
                {
                    forwardToHomescreen = false;

                    string errorDescription = ErrorHandling.ErrorDescriptionMapper.GetInstance().GetErrorDescription(ErrorCodes.WnsChannelInitializationFailed); ;
                    string title = "Error";
                    showErrorMessageDialog(errorDescription, title);
                }

                if(forwardToHomescreen)
                {
                    // Navigiere auf den Homescreen.
                    Frame.Navigate(typeof(Views.Homescreen.Homescreen));
                }
            }
        }

        /// <summary>
        /// Zeigt eine Fehlernachricht innerhalb eines MessageDialog Elements an.
        /// </summary>
        /// <param name="content">Der Inhalt des MessageDialog Elements, d.h. die Beschreibung des Fehlers.</param>
        /// <param name="title">Der Titel des MessageDialog Elements.</param>
        private async void showErrorMessageDialog(string content, string title)
        {
            var dialog = new Windows.UI.Popups.MessageDialog(content, title);
            dialog.Commands.Add(new Windows.UI.Popups.UICommand("Ok") { Id = 0 });
            dialog.DefaultCommandIndex = 0;

            var result = await dialog.ShowAsync();
        }
    }
}
