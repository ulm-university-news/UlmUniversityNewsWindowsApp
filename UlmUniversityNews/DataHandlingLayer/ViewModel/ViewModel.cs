using DataHandlingLayer.Controller.ValidationErrorReportInterface;
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

        # region properties
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
        #endregion properties

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
        protected async void displayProgressBar()
        {
            Windows.UI.ViewManagement.StatusBarProgressIndicator progressbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ProgressIndicator;

            await progressbar.ShowAsync();
        }

        /// <summary>
        /// Blende eine angezeigte Fortschrittsanzeige wieder aus.
        /// </summary>
        protected async void hideProgressBar()
        {
            Windows.UI.ViewManagement.StatusBarProgressIndicator progressbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ProgressIndicator;

            await progressbar.HideAsync();
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
            // Homescreen
            DrawerMenuEntry homescreenEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Homescreen",
                DisplayableNameResourceKey = "DrawerMenuEntryHomescreen",
                ReferencedPageKey = "Homescreen",
                IconPath = "/Assets/extIcons/appbar.home.empty.png"
            };

            // Login Seite
            DrawerMenuEntry loginEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Login",
                DisplayableNameResourceKey = "DrawerMenuEntryLogin",
                ReferencedPageKey = "Login",
                IconPath = "/Assets/extIcons/appbar.door.enter.png"
            };

            // Anwendungseinstellung
            DrawerMenuEntry applicationSettingsEntry = new DrawerMenuEntry()
            {
                MenuEntryName = "Anwendungseinstellungen",
                DisplayableNameResourceKey = "DrawerMenuEntryApplicationSettings",
                ReferencedPageKey = "ApplicationSettings",
                IconPath = "/Assets/Drawer/feature.settings.png"
            };

            // Füge Einträge der Liste hinzu, die den nicht eingeloggten Zustand repräsentiert.
            drawerMenuEntriesStatusNoLogin.Add(homescreenEntry);
            drawerMenuEntriesStatusNoLogin.Add(applicationSettingsEntry);
            drawerMenuEntriesStatusNoLogin.Add(loginEntry);
        }
    }
}
