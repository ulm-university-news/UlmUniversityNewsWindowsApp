using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System.Diagnostics;

namespace DataHandlingLayer.ViewModel
{
    /// <summary>
    /// Die Klasse DialogBaseViewModel repräsentiert eine Basisklasse eines ViewModels,
    /// welche Funktionalität bereitstellt, die für einen Dialog zur Erstellung von Ressourcen relevant
    /// ist. Dazu gehört das einblenden einer Warnung, wenn ein Nutzer über das Drawer Menü
    /// oder die Zurück Taste des Smartphones den Dialog verlässt.
    /// </summary>
    public class DialogBaseViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Zwischenspeicher, um sich das eigentlich angeklickte Element zu merken, wenn 
        /// statt der Aktion das Fylout mit der Warnung bezüglich der Schließung des
        /// Dialogs angezeigt wird.
        /// </summary>
        private object originalClickedElement;
        #endregion Fields

        #region Properties
        private bool isFlyoutOpen;
        /// <summary>
        /// Gibt an, ob das Flyout mit der Warnung bezüglich der Schließung
        /// des Dialogs gerade angezeigt wird.
        /// </summary>
        public bool IsFlyoutOpen
        {
            get { return isFlyoutOpen; }
            set { this.setProperty(ref this.isFlyoutOpen, value); }
        }
        #endregion Properties

        #region Commands
        private RelayCommand showWarningFlyout;
        /// <summary>
        /// Befehl, der zum Öffnen des Fylouts mit der Warnung zur Schließung des
        /// Dialogs verwendet wird.
        /// </summary>
        public RelayCommand ShowWarningFlyout
        {
            get { return showWarningFlyout; }
            set { showWarningFlyout = value; }
        }

        private RelayCommand performOriginalActionCommand;
        /// <summary>
        /// Befehl, der zum Schließen des Fylouts mit der Warnung zur Schließung des
        /// Dialogs verwendet wird.
        /// </summary>
        public RelayCommand PerformOriginalDrawerMenuActionCommand
        {
            get { return performOriginalActionCommand; }
            set { performOriginalActionCommand = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse DialogBaseViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public DialogBaseViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            IsFlyoutOpen = false;

            ShowWarningFlyout = new RelayCommand(param => executeShowWarningFlyoutCommand(param));
            PerformOriginalDrawerMenuActionCommand = new RelayCommand(param => executePerformOriginalDrawerMenuActionCommand());
        }

        /// <summary>
        /// Führt den Befehl zum Öffnen des Fylouts mit der Warnung bezüglich 
        /// der Schließung des Dialogs aus.
        /// </summary>
        /// <param name="originalClickedElement">Das Element, das eigentlich angeklickt wurde.
        ///     Anstelle der Aktion, die durch das angeklickte Element ausgelöst wird, wird nun jedoch
        ///     das Flyout angezeigt.</param>
        private void executeShowWarningFlyoutCommand(object originalClickedElement)
        {
            this.originalClickedElement = originalClickedElement;

            Debug.WriteLine("Open flyout.");
            IsFlyoutOpen = true;
        }

        /// <summary>
        /// Führt den Befehl zum Schließen des Fylouts mit der Warnung bezüglich 
        /// der Schließung des Dialogs aus. Es wird die Aktion ausgeführt, die normal
        /// ausgeführt wird, wenn das ursprünglich geklickte Element gewählt wurde.
        /// </summary>
        private void executePerformOriginalDrawerMenuActionCommand()
        {
            Debug.WriteLine("Close flyout.");
            IsFlyoutOpen = false;

            // Exceute the DrawerButtonCommand with the orginal clicked element.
            DrawerButtonCommand.Execute(originalClickedElement);
            this.originalClickedElement = null;
        }
    }
}
