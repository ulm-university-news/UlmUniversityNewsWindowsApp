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

namespace UlmUniversityNews.Views.Group.AddAndEditBallotDialog
{
    /// <summary>
    /// Eine leere Seite, die eigenständig verwendet werden kann oder auf die innerhalb eines Frames navigiert werden kann.
    /// </summary>
    public sealed partial class AddAndEditBallot : Page
    {
        private NavigationHelper navigationHelper;

        /// <summary>
        /// Eine Referenz auf die ViewModel Instanz, die der View zugeordnet ist.
        /// </summary>
        private AddAndEditBallotViewModel addAndEditBallotViewModel;

        /// <summary>
        /// Gibt an, ob es sich um einen Dialog zur Erstellung einer Abstimmung handelt.
        /// </summary>
        private bool isAddBallotDialog = false;

        public AddAndEditBallot()
        {
            this.InitializeComponent();

            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
            this.Unloaded += AddAndEditBallot_Unloaded;

            this.navigationHelper = new NavigationHelper(this);
            this.navigationHelper.LoadState += this.NavigationHelper_LoadState;
            this.navigationHelper.SaveState += this.NavigationHelper_SaveState;

            addAndEditBallotViewModel = new AddAndEditBallotViewModel(App.NavigationService, App.ErrorMapper);
            this.DataContext = addAndEditBallotViewModel;

            // Initialisiere das DrawerLayout.
            DrawerLayout.InitializeDrawerLayout();
            ListMenuItems.ItemsSource = addAndEditBallotViewModel.LoadDrawerMenuEntries();
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

            if (groupId != -1 && ballotId == -1)
            {
                isAddBallotDialog = true;
                await addAndEditBallotViewModel.LoadCreateBallotDialogAsync(groupId);
            }
            else if (groupId != -1 && ballotId != -1)
            {
                isAddBallotDialog = false;
                await addAndEditBallotViewModel.LoadEditBallotDialogAsync(groupId, ballotId);
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

            addAndEditBallotViewModel.ShowWarningFlyout.Execute(drawerMenuEntry);
        }

        /// <summary>
        /// Wird aufgerufen, wenn die Seite verlassen wird. Deregistriere den Handler für die Back-Taste,
        /// so dass die Behandlung der Back-Taste wieder vom NavigationHelper übernommen wird.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void AddAndEditBallot_Unloaded(object sender, RoutedEventArgs e)
        {
            Windows.Phone.UI.Input.HardwareButtons.BackPressed -= HardwareButtons_BackPressed;
        }

        /// <summary>
        /// Behandelt Klick auf "continue" Button. Wechsel auf das nächste PivotItem.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void AddAndEditBallotNextPivotItemButton_Click(object sender, RoutedEventArgs e)
        {
            // Wechsel auf Optionen-Tab.
            AddAndEditBallotPivot.SelectedIndex = 1;
        }

        /// <summary>
        /// Wird gerufen, wenn es eine Änderung beim aktiven PivotItem gibt.
        /// Wird hier genutzt um die Anzeige der AppBarButtons zu steuern.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void AddAndEditBallotPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            determineVisibilityOfAppBarButtons();
        }
        
        /// <summary>
        /// Event-Handler, der das Klick-Event des Toggle-Buttons behandelt. 
        /// Ändert den SelectionMode der Liste von Abstimmungsoptionen.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void AddAndEditSelectOptionsToggleButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddAndEditBallotOptionList != null && 
                AddAndEditBallotOptionList.SelectionMode == ListViewSelectionMode.Single)
            {
                AddAndEditBallotOptionList.SelectionMode = ListViewSelectionMode.Multiple;

                // Blende Buttons aus.
                AddAndEditBallotSaveChangesButton.Visibility = Visibility.Collapsed;
                AddAndEditBallotCreateBallotButton.Visibility = Visibility.Collapsed;
                
                // Blende "Entferne Option" Button ein.
                AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Visible;

            }
            else if (AddAndEditBallotOptionList != null && 
                AddAndEditBallotOptionList.SelectionMode == ListViewSelectionMode.Multiple)
            {
                AddAndEditBallotOptionList.SelectionMode = ListViewSelectionMode.Single;

                // Blende "Entferne Option" Button aus.
                AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
                
                // Blende Button abhängig vom gewählten Dialog wieder ein.
                if (isAddBallotDialog)
                    AddAndEditBallotCreateBallotButton.Visibility = Visibility.Visible;
                else
                    AddAndEditBallotSaveChangesButton.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// Bestimmt und setzt die Sichtbarkeit des ToggleButtons zur Auswahl von definierten Abstimmungsoptionen.
        /// </summary>
        private void determineVisibilityOfSelectOptionsToggleButton()
        {
            // Richte Sichtbarkeit nach Ausführbarkeit des Befehls zum Entfernen der Option aus.
            if (addAndEditBallotViewModel != null && 
                addAndEditBallotViewModel.RemoveBallotOptionCommand != null &&
                addAndEditBallotViewModel.RemoveBallotOptionCommand.CanExecute(null))
            {
                AddAndEditBallotSelectOptionsToggleButton.Visibility = Visibility.Visible;
            }
            else
            {
                AddAndEditBallotSelectOptionsToggleButton.Visibility = Visibility.Collapsed;
            }

            if (AddAndEditBallotPivot != null && AddAndEditBallotPivot.SelectedIndex == 0)
                AddAndEditBallotSelectOptionsToggleButton.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Prüft, welche Buttons abhängig vom aktuellen ViewZustand aktiv sind
        /// und deshalb angezeigt werden müssen. Inaktive Buttons werden ausgeblendet.
        /// </summary>
        private void determineVisibilityOfAppBarButtons()
        {
            if (AddAndEditBallotPivot.SelectedIndex == 0)
            {
                AddAndEditBallotNextPivotItemButton.Visibility = Visibility.Visible;
            }
            else if (AddAndEditBallotPivot.SelectedIndex == 1)
            {
                AddAndEditBallotNextPivotItemButton.Visibility = Visibility.Collapsed;
            }

            if (isAddBallotDialog)
            {
                // Erstellungsdialog.
                AddAndEditBallotSaveChangesButton.Visibility = Visibility.Collapsed;
                if (AddAndEditBallotPivot.SelectedIndex == 0)
                {
                    AddAndEditBallotCreateBallotButton.Visibility = Visibility.Collapsed;
                    AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
                }
                else if (AddAndEditBallotPivot.SelectedIndex == 1)
                {
                    if (AddAndEditBallotSelectOptionsToggleButton.IsChecked.GetValueOrDefault() == true)
                    {
                        AddAndEditBallotCreateBallotButton.Visibility = Visibility.Collapsed;
                        AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AddAndEditBallotCreateBallotButton.Visibility = Visibility.Visible;
                        AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
                    }
                }
            }
            else
            {
                // Änderungsdialog.
                AddAndEditBallotCreateBallotButton.Visibility = Visibility.Collapsed;
                if (AddAndEditBallotPivot.SelectedIndex == 0)
                {
                    AddAndEditBallotSaveChangesButton.Visibility = Visibility.Collapsed;
                    AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
                }
                else if (AddAndEditBallotPivot.SelectedIndex == 1)
                {
                    if (AddAndEditBallotSelectOptionsToggleButton.IsChecked.GetValueOrDefault() == true)
                    {
                        AddAndEditBallotSaveChangesButton.Visibility = Visibility.Collapsed;
                        AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        AddAndEditBallotSaveChangesButton.Visibility = Visibility.Visible;
                        AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
                    }
                }
            }

            // Prüfe die Sichtbarkeit des ToggleButtons für die Auswahl von Abstimmungsoptionen.
            determineVisibilityOfSelectOptionsToggleButton();
        }

        /// <summary>
        /// Event-Handler, der gerufen wird, wenn der Inhalt der ListView sich geändert hat.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void AddAndEditBallotOptionList_ContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
        {
            // Prüfe, ob Sichtbarkeitsstatus sich geändert hat.
            determineVisibilityOfSelectOptionsToggleButton();
        }

        /// <summary>
        /// Wird gerufen, wenn der Fokus auf das Eingabefeld für neue Optionen fällt.
        /// Blende "Option hinzufügen" Button ein und die anderen Buttons aus.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparamter.</param>
        private void AddAndEditBallotNewOptionTextField_GotFocus(object sender, RoutedEventArgs e)
        {
            // Blende "Option hinzufügen" Button ein.
            AddAndEditBallotAddBallotOptionButton.Visibility = Visibility.Visible;

            //Blende andere Buttons aus, abhängig von Dialogart.
            if (isAddBallotDialog)
            {
                AddAndEditBallotSelectOptionsToggleButton.Visibility = Visibility.Collapsed;
                AddAndEditBallotCreateBallotButton.Visibility = Visibility.Collapsed;
                AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                AddAndEditBallotSelectOptionsToggleButton.Visibility = Visibility.Collapsed;
                AddAndEditBallotSaveChangesButton.Visibility = Visibility.Collapsed;
                AddAndEditBallotDeleteOptionButton.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Wird gerufen, wenn der Fokus dem Eingabefeld für neue Optionen entzogen wird.
        /// Blende AddOption Button wieder aus und die ursprünglich dastehenden Buttons wieder ein.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparamter.</param>
        private void AddAndEditBallotNewOptionTextField_LostFocus(object sender, RoutedEventArgs e)
        {
            // Blende Button wieder aus.
            AddAndEditBallotAddBallotOptionButton.Visibility = Visibility.Collapsed;

            // Blende Buttons wieder ein, abhängig von Dialogart.
            determineVisibilityOfAppBarButtons();
        }

        /// <summary>
        /// Wird gerufen, wenn der Button zum Löschen von Abstimmungsoptionen betätigt wird.
        /// Führt den Befehl zum Löschen der gewählten Abstimmungsoptionen aus.
        /// Muss manuell im Code-Behind ausgeführt werden, da SelectedItems Property bei
        /// Multi-Selection sich nicht per Binding an eine ViewModel Variable binden lässt.
        /// </summary>
        /// <param name="sender">Ereignisquelle.</param>
        /// <param name="e">Ereignisparameter.</param>
        private void AddAndEditBallotDeleteOptionButton_Click(object sender, RoutedEventArgs e)
        {
            if (AddAndEditBallotOptionList != null && 
                addAndEditBallotViewModel.RemoveBallotOptionCommand != null && 
                addAndEditBallotViewModel.RemoveBallotOptionCommand.CanExecute(null))
            {
                var listItems = AddAndEditBallotOptionList.SelectedItems;
                List<object> selectedItems = listItems.ToList<object>();

                foreach (object item in selectedItems)
                {
                    System.Diagnostics.Debug.WriteLine("Test: Selected item: {0}.", item);
                    addAndEditBallotViewModel.RemoveBallotOptionCommand.Execute(item);
                }
            }
        }
    }
}
