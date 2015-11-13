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
using System.Diagnostics;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.Homescreen
{
    /// <summary>
    /// Der Homescreen der App. Es werden die Kanäle, Gruppen und Anwendungseinstellungen des Nutzers über den Homescreen verfügbar gemacht werden.
    /// </summary>
    public sealed partial class Homescreen : Page
    {
        private NavigationHelper navigationHelper;
        private ObservableDictionary defaultViewModel = new ObservableDictionary();

        public Homescreen()
        {
            Debug.WriteLine("Starting constructor of Homescreen.");
            this.InitializeComponent();

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            string[] menuItems = new string[5] { "Test Item 1", "Test Item 2", "Test Item 3", "Test Item 4", "Test Item 5" };
            ListMenuItems.ItemsSource = menuItems.ToList();

            // Füge CommandBar hinzu.
            initializeAppBar();

            Debug.WriteLine("Finished constructor of Homescreen.");
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
        /// Erstellt und initiiert eine CommandBar.
        /// </summary>
        private void initializeAppBar()
        {
            Debug.WriteLine("Creating AppBar.");
            CommandBar commandBar = HomescreenCommandBar;

            // Erstelle einen AppBarButton.
            AppBarButton testButton = new AppBarButton();
            testButton.Label = "hinzufügen";
            testButton.IsEnabled = true;
            testButton.Icon = new SymbolIcon(Symbol.Add);
            testButton.Click += testButton_Click;

            // Füge ihn als Primary Command hinzu.
            commandBar.PrimaryCommands.Add(testButton);

            // Erstelle noch einen AppBarButton.
            AppBarButton secondaryButtonTest = new AppBarButton();
            secondaryButtonTest.Label = "secondary Element";
            secondaryButtonTest.IsEnabled = true;
            secondaryButtonTest.Click += secondaryButtonTest_Click;

            // Füge den zweiten Button als Secondary Element hinzu.
            commandBar.SecondaryCommands.Add(secondaryButtonTest);
            Debug.WriteLine("AppBar created.");
        }

        void secondaryButtonTest_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Secondary Element clicked!");
        }

        void testButton_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Button clicked!");
        }

        private void checkLockScreenAccessPermission(){
            Debug.WriteLine("Start checking LockScreen access permission in local settings.");
            var localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;

            // Prüfe, ob der Zugriff abgelehnt wurde.
            string accessToLockScreenValue = localSettings.Values[Constants.Constants.AccessToLockScreenKey] as string;
            string accessDenied = Constants.Constants.AccessToLockScreenDenied;
            if (String.Compare(accessToLockScreenValue, accessDenied) == 0)
            {
                // Prüfe, ob der Nachrichtendialog noch angezeigt werden soll.
                string showLockScreenValue = localSettings.Values[Constants.Constants.ShowLockScreenMessageKey] as string;
                string showMessage = Constants.Constants.ShowLockScreenMessageYes;
                if (String.Compare(showLockScreenValue, showMessage) == 0){
                    // Zeige Dialog mit Hinweis an.
                    //CustomMessageBox messageBox = new CustomMessageBox();
                }
            }
        }
    }
}
