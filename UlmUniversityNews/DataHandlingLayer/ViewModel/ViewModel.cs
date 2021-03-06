﻿using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.Controller;

namespace DataHandlingLayer.ViewModel
{
    /// <summary>
    /// Abstrakte Klasse ViewModel stellt Funktionalitäten bereit, die von allen ViewModel Klassen benötigt werden.
    /// Dazu gehört die Implementierung von INotifyPropertyChanged und Zugriff auf häufig benötigte Objekte, wie das lokale Nutzerobjekt.
    /// Zudem bietet die Klasse Zugriff auf den Navigationsdienst und den ErrorMapper.
    /// </summary>
    public abstract class ViewModel : INotifyPropertyChanged, IValidationErrorReport
    {
        #region Fields
        /// <summary>
        ///  Eine Referenz auf den Navigationsdienst, über den die Seitennavigation erfolgt.
        /// </summary>
        protected INavigationService _navService;

        /// <summary>
        /// Eine Referenz auf den ErrorMapper, über den Fehlercodes auf Fehlernachrichten abgebildet und angezeigt werden können.
        /// </summary>
        private IErrorMapper _errorMapper;

        /// <summary>
        /// PropertyChanged Event wird gefeuert, wenn sich Werte von bestimmten Properties geändert haben.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion Fields

        # region Properties

        #region ProgressIndicator
        private bool isIndeterminateProgressIndicator;
        /// <summary>
        /// Gibt an, ob es sich bei der aktuellen Fortschrittsanzeige um eine mit unbekannter
        /// Dauer handelt.
        /// </summary>
        public bool IsIndeterminateProgressIndicator
        {
            get { return isIndeterminateProgressIndicator; }
            set { this.setProperty(ref this.isIndeterminateProgressIndicator, value); }
        }

        private bool isProgressIndicatorVisible;
        /// <summary>
        /// Gibt an, ob die Fortschrittsanzeige aktuell angezeigt werden soll.
        /// </summary>
        public bool IsProgressIndicatorVisible
        {
            get { return isProgressIndicatorVisible; }
            set { this.setProperty(ref this.isProgressIndicatorVisible, value); }
        }

        private string progressIndicatorText;
        /// <summary>
        /// Der Text, der in der Fortschrittsanzeige angezeigt werden soll. 
        /// </summary>
        public string ProgressIndicatorText
        {
            get { return progressIndicatorText; }
            set { this.setProperty(ref this.progressIndicatorText, value); }
        }

        private double progressIndicatorProgressValue;
        /// <summary>
        /// Der aktuelle Fortschrittswert bei einer Fortschrittsanzeige mit bekannter Dauer.
        /// </summary>
        public double ProgressIndicatorProgressValue
        {
            get { return progressIndicatorProgressValue; }
            set { this.setProperty(ref this.progressIndicatorProgressValue, value); }
        }
        #endregion ProgressIndicator

        private DataHandlingLayer.Common.ObservableDictionary validationMessages;
        /// <summary>
        /// Ein Verzeichnis, welches aufgetretene Valdierungsfehler auf die entsprechenden Properties abbildet.
        /// </summary>
        public DataHandlingLayer.Common.ObservableDictionary ValidationMessages
        {
            get { return validationMessages; }
            set
            {
                validationMessages = value;
                onPropertyChanged("ValidationMessages");
            }
        }

        private List<DrawerMenuEntry> drawerMenuEntriesStatusNoLogin;
        /// <summary>
        /// Eine Liste aller Drawer Menüeinträge, die angezeigt werden sollen,
        /// wenn der aktuelle Nutzer nicht eingeloggt ist.
        /// </summary>
        public List<DrawerMenuEntry> DrawerMenuEntriesStatusNoLogin
        {
            get { return drawerMenuEntriesStatusNoLogin; }
            set { drawerMenuEntriesStatusNoLogin = value; }
        }

        private List<DrawerMenuEntry> drawerMenuEntriesStatusLoggedIn;
        /// <summary>
        /// Eine Liste aller Drawer Menüeinträge, die angezeigt werden sollen,
        /// wenn der aktuelle Nutzer eingeloggt ist.
        /// </summary>
        public List<DrawerMenuEntry> DrawerMenuEntriesStatusLoggedIn
        {
            get { return drawerMenuEntriesStatusLoggedIn; }
            set { drawerMenuEntriesStatusLoggedIn = value; }
        }  
        #endregion Properties

        #region Commands
        private RelayCommand drawerButtonCommand;
        /// <summary>
        /// Der Befehl zur Behandlung eines Klicks auf einen Button im Drawer Menü.
        /// </summary>
        public RelayCommand DrawerButtonCommand
        {
            get { return drawerButtonCommand; }
            set { drawerButtonCommand = value; }
        }      
        #endregion Commands

        /// <summary>
        /// Konstruktor zur Initialisierung der ViewModel Klasse.
        /// </summary>
        protected ViewModel(INavigationService navService, IErrorMapper errorMapper)
        {
            _navService = navService;
            _errorMapper = errorMapper;

            ValidationMessages = new DataHandlingLayer.Common.ObservableDictionary();
            drawerMenuEntriesStatusNoLogin = new List<DrawerMenuEntry>();
            drawerMenuEntriesStatusLoggedIn = new List<DrawerMenuEntry>();

            // Erzeuge die Commands.
            DrawerButtonCommand = new RelayCommand(param => executeDrawerButtonCommand(param));

            // Erzeuge Drawer Menüeinträge.
            createDrawerMenuEntries();
        }

        // Property Change Logik:
        protected bool setProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if(object.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            this.onPropertyChanged(propertyName);
            return true;
        }

        protected void onPropertyChanged([CallerMemberName] String propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if(eventHandler != null){
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        /// <summary>
        /// Informiert den Nutzer über einen aufgetretenen Fehler, indem der Fehler auf dem Display angezeigt wird.
        /// </summary>
        /// <param name="errorCode">Der Fehlercode des aufgetretenen Fehlers.</param>
        protected void displayError(int errorCode)
        {
            _errorMapper.DisplayErrorMessage(errorCode);
        }

        /// <summary>
        /// Blende eine Fortschrittsanzeige ein, die für eine nicht vorher definierte Zeit sichtbar bleibt.
        /// </summary>
        protected void displayIndeterminateProgressIndicator()
        {
            IsProgressIndicatorVisible = true;
            IsIndeterminateProgressIndicator = true;
        }

        /// <summary>
        /// Blende eine Fortschrittsanzeige ein, die für eine nicht vorher definierte Zeit sichtbar bleibt.
        /// </summary>
        /// <param name="text">Der Text, der in der StatusBar während der Dauer der Fortschrittsanzeige angezeigt werden soll.</param>
        protected void displayIndeterminateProgressIndicator(string text)
        {
            IsProgressIndicatorVisible = true;
            IsIndeterminateProgressIndicator = true;
            ProgressIndicatorText = text;
        }

        /// <summary>
        /// Blende eine angezeigte Fortschrittsanzeige mit unbekannter Dauer wieder aus.
        /// </summary>
        protected void hideIndeterminateProgressIndicator()
        {
            IsIndeterminateProgressIndicator = false;
            ProgressIndicatorText = string.Empty;
            IsProgressIndicatorVisible = false;
        }

        /// <summary>
        /// Zeigt einen Text in der StatusBar für eine angegebene Zeitspanne an.
        /// </summary>
        /// <param name="text">Der Text, der in der StatusBar angezeigt werden soll.</param>
        /// <param name="displayDuration">Die Dauer, die der Text angezeigt werden soll in Sekunden.</param>
        protected void displayStatusBarText(string text, double displayDuration)
        {
            Debug.WriteLine("Hey, I just started the displayStatusBarText method.");

            IsProgressIndicatorVisible = true;
            IsIndeterminateProgressIndicator = false;
            ProgressIndicatorText = text;
            ProgressIndicatorProgressValue = 0.0f;

            var task = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(displayDuration));
                hideStatusBarText();
            });
            Debug.WriteLine("Hey, I have finished with the displayStatusBarText method.");
        }

        /// <summary>
        /// Hilfsmethode, die einen in der StatusBar angezeigten Text wieder ausblendet.
        /// </summary>
        private async void hideStatusBarText()
        {
            Debug.WriteLine("Hey, the hideStatusBarText method is called.");
            await Windows.ApplicationModel.Core.CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal,
                () =>
                {
                    IsProgressIndicatorVisible = false;
                    ProgressIndicatorText = string.Empty;
                });           
        }

        /// <summary>
        /// Meldet einen Validierungsfehler für eine gegebene Property an die View.
        /// Der Validierungsfehler wird mit seiner Nachricht bzw. einem Ressourcenschlüssel zu einer Nachricht
        /// in einem Verzeichnis abgelegt und die View wird informiert.
        /// </summary>
        /// <param name="property">Die Property, für die der Validierungsfehler aufgetreten ist.</param>
        /// <param name="validationMessage">Die Fehlernachricht, bzw. ein Ressourcenschlüssel auf eine Fehlernachricht in den sprachenabhängigen Ressourcendateien.</param>
        public void ReportValidationError(string property, string validationMessage)
        {
            if(validationMessages.ContainsKey(property)){
                validationMessages[property] = validationMessage;
            }
            else
            {
                ValidationMessages.Add(property, validationMessage);
            }
        }

        /// <summary>
        /// Entfernt einen Validierungsfehler zu dem gegebenen Property aus dem Verzeichnis.
        /// </summary>
        /// <param name="property">Die Property, für die der Fehler aufgetreten ist.</param>
        public void RemoveFailureMessagesForProperty(string property)
        {
            if(validationMessages.ContainsKey(property)){
                ValidationMessages.Remove(property);
            }
        }

        /// <summary>
        /// Entfernt alle Validierungsfehler, die im Verzeichnis gespeichert sind.
        /// </summary>
        public void RemoveAllFailureMessages()
        {
            ValidationMessages.Clear();
        }

        /// <summary>
        /// Erzeugt die Menüeinträge für das Drawer Menü.
        /// </summary>
        private void createDrawerMenuEntries()
        {
            // Homescreen:
            DrawerMenuEntry homescreenEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Homescreen",
                DisplayableNameResourceKey = "DrawerMenuEntryHomescreen",
                ReferencedPageKey = "Homescreen",
                IconPath = "/Assets/extIcons/appbar.home.empty.png"
            };

            // Moderator Homescreen:
            DrawerMenuEntry moderatorHomescreenEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Homescreen",
                DisplayableNameResourceKey = "DrawerMenuEntryHomescreen",
                ReferencedPageKey = "HomescreenModerator",
                IconPath = "/Assets/extIcons/appbar.home.empty.png"
            };

            // Login Seite:
            DrawerMenuEntry loginEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Login",
                DisplayableNameResourceKey = "DrawerMenuEntryLogin",
                ReferencedPageKey = "LoginPage",
                IconPath = "/Assets/extIcons/appbar.door.enter.png"
            };

            // Logout Seite:
            DrawerMenuEntry logoutEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Logout",
                DisplayableNameResourceKey = "DrawerMenuEntryLogout",
                ReferencedPageKey = "Homescreen",
                IconPath = "/Assets/extIcons/appbar.door.leave.png"
            };

            // Anwendungseinstellung:
            DrawerMenuEntry applicationSettingsEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "ApplicationSettings",
                DisplayableNameResourceKey = "DrawerMenuEntryApplicationSettings",
                ReferencedPageKey = "ApplicationSettings",
                IconPath = "/Assets/Drawer/feature.settings.png"
            };

            // About University News:
            DrawerMenuEntry aboutUniversityNews = new DrawerMenuEntry()
            {
                MenuEntryName = "About",
                DisplayableNameResourceKey = "DrawerMenuEntryAbout",
                ReferencedPageKey = "AboutUniversityNews",
                IconPath = "/Assets/extIcons/appbar.information.png"
            };

            // Füge Einträge der Liste hinzu, die den nicht eingeloggten Zustand repräsentiert.
            drawerMenuEntriesStatusNoLogin.Add(homescreenEntry);
            drawerMenuEntriesStatusNoLogin.Add(applicationSettingsEntry);
            drawerMenuEntriesStatusNoLogin.Add(aboutUniversityNews);
            drawerMenuEntriesStatusNoLogin.Add(loginEntry);

            // Füge Einträge der Liste hinzu, die den eingeloggten Zustand repräsentiert.            
            drawerMenuEntriesStatusLoggedIn.Add(moderatorHomescreenEntry);
            drawerMenuEntriesStatusLoggedIn.Add(applicationSettingsEntry);
            drawerMenuEntriesStatusLoggedIn.Add(aboutUniversityNews);
            drawerMenuEntriesStatusLoggedIn.Add(logoutEntry);
        }

        /// <summary>
        /// Lädt die Menüeinträge des DrawerMenü, abhängig davon, ob ein Moderator
        /// gerade eingeloggt ist, man sich also in der Moderatorenansicht befindet,
        /// oder ob man sich in der Nutzeransicht befindet.
        /// </summary>
        /// <returns>Eine Liste an Menüeinträgen des Typs DrawerMenuEntry.</returns>
        public List<DrawerMenuEntry> LoadDrawerMenuEntries()
        {
            Moderator activeModerator = LocalModerator.GetInstance().GetCachedModerator();

            if (activeModerator != null)
            {
                return DrawerMenuEntriesStatusLoggedIn;
            }
            else
            {
                return DrawerMenuEntriesStatusNoLogin;
            }
        }

        /// <summary>
        /// Behandle Klick auf einen Button im Drawer Menü.
        /// </summary>
        private void executeDrawerButtonCommand(object clickedButton)
        {
            DrawerMenuEntry menuEntry = clickedButton as DrawerMenuEntry;
            if(menuEntry != null)
            {
                Debug.WriteLine("Drawer Button is clicked. Button with name {0}.", menuEntry.MenuEntryName);
                
                // Spezialfall. Ein manuell erzeugter DrawerMenuEntry, der beim
                // manuellen Behandeln des Back-Hardware Buttons gefeuert wird.
                if (menuEntry.MenuEntryName == "GoBack")
                {
                    if (_navService.CanGoBack())
                    {
                        _navService.GoBack();
                    }
                    return;
                }

                // Spezialfall. Führe Logout durch.
                if (menuEntry.MenuEntryName == "Logout")
                {
                    // Führe Logout durch.
                    LoginController loginController = new LoginController();
                    loginController.PerformLogout();
                    // Navigiere zurück auf den Homescreen des Nutzers.
                    _navService.Navigate(menuEntry.ReferencedPageKey);

                    // Lösche noch den Backstack, so dass nicht per Back-Key auf die Moderatorensicht
                    // zurück navigiert werden kann.
                    _navService.ClearBackStack();

                    return;
                }

                // Normaler Fall, navgiere auf die verlinkte Seite.
                _navService.Navigate(menuEntry.ReferencedPageKey);
            }
        }
    }
}
