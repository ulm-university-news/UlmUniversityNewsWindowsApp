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
    public abstract class ViewModel : INotifyPropertyChanged
    {
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

        /// <summary>
        /// Konstruktor zur Initialisierung der ViewModel Klasse.
        /// </summary>
        protected ViewModel(INavigationService navService, IErrorMapper errorMapper)
        {
            _navService = navService;
            _errorMapper = errorMapper;
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

        protected async void displayProgressBar()
        {
            Windows.UI.ViewManagement.StatusBarProgressIndicator progressbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ProgressIndicator;

            await progressbar.ShowAsync();
        }

        protected async void hideProgressBar()
        {
            Windows.UI.ViewManagement.StatusBarProgressIndicator progressbar = Windows.UI.ViewManagement.StatusBar.GetForCurrentView().ProgressIndicator;

            await progressbar.HideAsync();
        }

    }
}
