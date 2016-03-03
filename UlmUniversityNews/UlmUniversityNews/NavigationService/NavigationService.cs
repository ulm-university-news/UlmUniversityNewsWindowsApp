using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.UI.Xaml.Controls;

namespace UlmUniversityNews.NavigationService
{
    public class NavigationService : DataHandlingLayer.NavigationService.INavigationService
    {
        #region Fields
        /// <summary>
        /// Datenstruktur, die Seitentypen auf ihre Schlüssel abbildet.
        /// </summary>
        private Dictionary<string, Type> pageMap;

        /// <summary>
        /// Eine Referenz auf den Frame der Anwendung. Der Frame wird für die Realisierung der Navigation verwendet. 
        /// </summary>
        private Frame rootFrame;

        /// <summary>
        /// Hält den Zustand der besuchten Seiten zur Laufzeit der Anwendung, indem er die Schlüssel der Seiten speichert.
        /// Mittels dieser Liste kann man herausfinden, auf welcher Seite in der Anwendung man sich aktuell befindet.
        /// </summary>
        //private List<string> pageKeyHistory;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der Klasse NavigationService.
        /// </summary>
        /// <param name="rootFrame">Der aktuelle Frame der Anwendung, mittels dem die Navigation realisiert werden soll.</param>
        public NavigationService(Frame rootFrame)
        {
            this.rootFrame = rootFrame;
            //pageKeyHistory = new List<string>();
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
        public async void Navigate(string pageKey)
        {
            Debug.WriteLine("In Navigate(pageKey) method of the NavigationService and the current Thread ID is: {0}.", Environment.CurrentManagedThreadId);

            //if(pageKeyHistory.Count > 0 && String.Compare(pageKeyHistory.Last(), pageKey) == 0)
            //{
            //    Debug.WriteLine("No need to perform navigation. We are already on this page.");
            //    return;
            //}

            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.Navigate(pageMap[pageKey]));

            //pageKeyHistory.Add(pageKey);
        }

        /// <summary>
        /// Navigiere zur Seite, die unter dem angegeben Schlüssel registriert ist. Gib dabei den spezifizierten Parameter mit.
        /// </summary>
        /// <param name="pageKey">Der Schlüssel der Seite.</param>
        /// <param name="parameter">Der zu übergebende Parameter.</param>
        public async void Navigate(string pageKey, object parameter)
        {
            Debug.WriteLine("In Navigate(pageKey, object) method of the NavigationService and the current Thread ID is: {0}.", Environment.CurrentManagedThreadId);

            //if (pageKeyHistory.Count > 0 && String.Compare(pageKeyHistory.Last(), pageKey) == 0)
            //{
            //    Debug.WriteLine("No need to perform navigation. We are already on this page.");
            //    return;
            //}

            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher; 
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.Navigate(pageMap[pageKey], parameter));

            //pageKeyHistory.Add(pageKey);
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
            Debug.WriteLine("In GoBack() method of the NavigationService and the current Thread ID is: {0}.", Environment.CurrentManagedThreadId);
            
            if(CanGoBack())
            {
                //if(pageKeyHistory.Count > 0)
                //{
                //    Debug.WriteLine("Removing element {0} from page history.", pageKeyHistory.ElementAt(pageKeyHistory.Count - 1));
                //    pageKeyHistory.RemoveAt(pageKeyHistory.Count - 1);
                //}

                var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
                await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () => rootFrame.GoBack());
            }
        }

        /// <summary>
        /// Entfernt das zuletzt gespeicherte Element aus dem Navigationsverlauf.
        /// </summary>
        public async void RemoveEntryFromBackStack()
        {
            //if (pageKeyHistory.Count > 0)
            //{
            //    Debug.WriteLine("Removing element {0} from page history.", pageKeyHistory.ElementAt(pageKeyHistory.Count - 1));
            //    pageKeyHistory.RemoveAt(pageKeyHistory.Count - 1);
            //}

            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (rootFrame.BackStack.Count >= 1)
                {
                    rootFrame.BackStack.RemoveAt(rootFrame.BackStack.Count - 1);
                }
                else
                {
                    Debug.WriteLine("Cannot remove entry from back stack as there is no entry on the back stack.");
                }
            });

        }

        /// <summary>
        /// Löscht alle Einträge des Back-Stack.
        /// </summary>
        public async void ClearBackStack()
        {
            var dispatcher = CoreApplication.MainView.CoreWindow.Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                {
                    rootFrame.BackStack.Clear();
                });
        }
    }
}
