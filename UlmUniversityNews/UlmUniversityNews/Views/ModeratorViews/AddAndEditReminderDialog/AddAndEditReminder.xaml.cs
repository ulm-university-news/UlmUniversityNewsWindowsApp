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
using DataHandlingLayer.DataModel;

// Die Elementvorlage "Standardseite" ist unter "http://go.microsoft.com/fwlink/?LinkID=390556" dokumentiert.

namespace UlmUniversityNews.Views.ModeratorViews.AddAndEditReminderDialog
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class AddAndEditReminder : Page
    {
        private NavigationHelper navigationHelper;
        private AddAndEditReminderViewModel addAndEditReminderViewModel;
        
        public AddAndEditReminder()
        {
            this.InitializeComponent();

            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            this.Unloaded += AddAndEditReminder_Unloaded;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            addAndEditReminderViewModel = new AddAndEditReminderViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = addAndEditReminderViewModel;

            // Initialisiere das Drawer Menü.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = addAndEditReminderViewModel.LoadDrawerMenuEntries();
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
            int channelId = -1;
            int reminderId = -1;
            if (e.NavigationParameter != null)
            {
                string navParam = e.NavigationParameter as string;
                string[] navParams = navParam.Split(new string [] {"?", "="}, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < navParams.Length; i++)
                {
                    if (navParams[i] == "channelId" && i+1 < navParams.Length) {
                        int.TryParse(navParams[i+1], out channelId);
                    }
                    else if (navParams[i] == "reminderId" && i + 1 < navParams.Length)
                    {
                        int.TryParse(navParams[i + 1], out reminderId);
                    }
                }
            }
            
            if (channelId != -1)
            {
                // Lade Channel
                await addAndEditReminderViewModel.LoadSelectedChannel(channelId);

                if (reminderId != -1)
                {
                    // TODO Load Edit Dialog
                }
                else
                {
                    // Lade Dialog "Reminder hinzufügen."
                    addAndEditReminderViewModel.LoadAddReminderDialog();
                }
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
        /// Event-Handler, der das Verhalten des Back-Button für diese Seite festlegt.
        /// </summary>
        /// <param name="sender">Die Event-Quelle.</param>
        /// <param name="e">Eventparameter.</param>
        void HardwareButtons_BackPressed(object sender, Windows.Phone.UI.Input.BackPressedEventArgs e)
        {
            e.Handled = true;   // Ereignis behandelt.

            // Zeige das Flyout mit der Warnung an.
            // Gebe ein spezielles DrawerMenuEntry Objekt mit, welches die Aktion "Go Back repräsentiert".
            DrawerMenuEntry drawerMenuEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "GoBack",
                DisplayableNameResourceKey = null,
                IconPath = null,
                ReferencedPageKey = null
            };

            addAndEditReminderViewModel.ShowWarningFlyout.Execute(drawerMenuEntry);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Seite verlassen wird. Deregistriere den Handler für die Back-Taste,
        /// so dass die Behandlung der Back-Taste wieder vom NavigationHelper übernommen wird.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AddAndEditReminder_Unloaded(object sender, RoutedEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }
    }
}
