using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

namespace UlmUniversityNews.NavigationService
{
    public class NavigationService : DataHandlingLayer.NavigationService.INavigationService
    {
        /// <summary>
        /// Datenstruktur, die Seitentypen auf ihre Schlüssel abbildet.
        /// </summary>
        private Dictionary<string, Type> pageMap;

        /// <summary>
        /// Eine Referenz auf den Frame der Anwendung. Der Frame wird für die Realisierung der Navigation verwendet. 
        /// </summary>
        private Frame rootFrame;

        /// <summary>
        /// Erzeugt eine Instanz der Klasse NavigationService.
        /// </summary>
        /// <param name="rootFrame">Der aktuelle Frame der Anwendung, mittels dem die Navigation realisiert werden soll.</param>
        public NavigationService(Frame rootFrame)
        {
            this.rootFrame = rootFrame;
            pageMap = new Dictionary<string, Type>();
        }

        /// <summary>
        /// Registriert eine Seite unter einem bestimmten Schlüssel beim NavigationService.
        /// </summary>
        /// <param name="pageKey">Der Schlüssel unter dem die Seite registriert werden soll.</param>
        /// <param name="page">Der Typ der Klasse der zu registrierenden Seite.</param>
        public void RegisterPage(string pageKey, Type page)
        {
            pageMap.Add(pageKey, page);
        }

        /// <summary>
        /// Navigiere zur Seite, die unter dem angegebenen Schlüssel registriert ist. 
        /// </summary>
        /// <param name="pageKey">Der Schlüssel der Seite.</param>
        public void Navigate(string pageKey)
        {
            //var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            //await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.Navigate(pageMap[pageKey]));

            rootFrame.Navigate(pageMap[pageKey]);
        }

        /// <summary>
        /// Navigiere zur Seite, die unter dem angegeben Schlüssel registriert ist. Gib dabei den spezifizierten Parameter mit.
        /// </summary>
        /// <param name="pageKey">Der Schlüssel der Seite.</param>
        /// <param name="parameter">Der zu übergebende Parameter.</param>
        public async void Navigate(string pageKey, object parameter)
        {
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher; 
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.Navigate(pageMap[pageKey], parameter));
        }

        /// <summary>
        /// Zeigt an, ob der BackStack (d.h. der Navigationsverlauf) ein Element enthält, auf das man zurückkehren könnte.
        /// </summary>
        /// <returns>True, wenn es ein solches Element gibt, ansonsten false.</returns>
        public bool CanGoBack()
        {
            return rootFrame.CanGoBack;
        }

        /// <summary>
        /// Kehre zum letzten Element im Navigationsverlauf zurück.
        /// </summary>
        public async void GoBack()
        {
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.GoBack());
        }

        /// <summary>
        /// Entfernt das zuletzt gespeicherte Element aus dem Navigationsverlauf.
        /// </summary>
        public void RemoveEntryFromBackStack()
        {
            rootFrame.BackStack.RemoveAt(rootFrame.BackStack.Count - 1);
        }
    }
}
