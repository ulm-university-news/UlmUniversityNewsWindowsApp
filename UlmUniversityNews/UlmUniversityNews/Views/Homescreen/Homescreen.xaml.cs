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
using System.Threading.Tasks;

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

            HomescreenPivot.Loaded += HomescreenPivot_Loaded;

            // Initialisiere das Drawer Layout.
            DrawerLayout.InitializeDrawerLayout();
            string[] menuItems = new string[5] { "Test Item 1", "Test Item 2", "Test Item 3", "Test Item 4", "Test Item 5" };
            ListMenuItems.ItemsSource = menuItems.ToList();

            // Füge CommandBar hinzu.
            initializeAppBar();

            Debug.WriteLine("Finished constructor of Homescreen.");
        }

        /// <summary>
        /// Event-Handler für das Loaded Event des Pivot-Elements. Wird aufgerufen wenn das Pivot UI Element geladen wurde.
        /// Wird hier genutzt um zu prüfen, ob der Zugriff auf den LockScreen gewährt wurde.
        /// </summary>
        /// <param name="sender">Quelle des Events.</param>
        /// <param name="e">Eventparameter.</param>
        async void HomescreenPivot_Loaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Homescreen_Pivot Loaded.");
            // Prüfe, ob Zugriff auf LockScreen gewährt ist, um Hintergrundaufgaben ausführen zu dürfen.
            await checkLockScreenAccessPermissionAsync();

            Debug.WriteLine("Finished Homescreen_Pivot Loaded Event Handler.");
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
            Debug.WriteLine("LoadState Homescreen");
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

        # region commandBar
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
        # endregion commandBar

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
            string accessToLockScreenValue = localSettings.Values[Constants.Constants.AccessToLockScreenKey] as string;
            string accessDenied = Constants.Constants.AccessToLockScreenDenied;
            if (String.Compare(accessToLockScreenValue, accessDenied) == 0)
            {
                // Prüfe, ob der Nachrichtendialog noch angezeigt werden soll.
                string showLockScreenValue = localSettings.Values[Constants.Constants.ShowLockScreenMessageKey] as string;
                string showMessage = Constants.Constants.ShowLockScreenMessageYes;
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
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
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
            localSettings.Values[Constants.Constants.ShowLockScreenMessageKey] = Constants.Constants.ShowLockScreenMessageNo;
        }
        # endregion checkLockScreenAccessContentDialog

    }
}
