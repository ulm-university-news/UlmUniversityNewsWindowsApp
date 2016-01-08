using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace DataHandlingLayer.Common
{
    public class IncrementalLoadingCollection<T, I> : ObservableCollection<I>, ISupportIncrementalLoading
        where T : IIncrementalSource<I>, new()
    {
        #region Fields
        // Die Referenz auf die tatsächliche Implementierung der GetPagedItems Methode.
        private T source;   

        // Wie viele Items sollen pro Seite, d.h. pro Aufruf der GetPagedItems Methode geladen werden.
        private int itemsPerPage;   

        // Gibt an, ob weitere Elemente geladen werden können.
        private bool hasMoreItems;

        /* Der Index, der angibt wie viele Seiten man schon geladen hat und über den sich somit 
         * auch bestimmen lässt, wie viele Elemente man schon insgesamt geladen hat. */
        private int currentPage;

        // Die Id der Resource, von der die Elemente inkrementell geladen werden sollen.
        private int resourceId;
        #endregion Fields

        /// <summary>
        /// Erzeugt eine Instanz der IncrementalLoadingCollection.
        /// </summary>
        /// <param name="resourceId">Die Id der Resource, von der die Elemente dynamisch geladen werden sollen.</param>
        /// <param name="itemsPerPage">Die Anzahl an Elemente pro Ladevorgang.</param>
        public IncrementalLoadingCollection(int resourceId, int itemsPerPage)
        {
            this.source = new T();  // Erzeuge Instanz der Implementierungsklasse von IIncrementalSource.
            this.itemsPerPage = itemsPerPage;

            this.resourceId = resourceId;

            // Setze zunächst auf true. Wird auf false gesetzt, wenn erkannt wird, dass keine weiteren Elemente geladen werden können.
            this.hasMoreItems = true;
        }
        
        /// <summary>
        /// Gibt an, ob es weitere Elemente gibt, die geladen werden können.
        /// </summary>
        public bool HasMoreItems
        {
            get { return hasMoreItems; }
        }

        /// <summary>
        /// Lade eine bestehende Collection von Elementen in die IncrementalLoadingCollection.
        /// </summary>
        /// <param name="collection">Eine Collection mit Elementen des Typs <typeparamref name="T"/>.</param>
        public async Task LoadExistingCollectionAsync(List<I> collection)
        {
            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;
            await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                // Füge die neu hinzugekommenen Einträge der Collection hinzu.
                foreach (I item in collection)
                {
                    this.Add(item);
                }
            });
        }

        /// <summary>
        /// Wird aufgerufen, wenn neue Elemente geladen werden sollen.
        /// </summary>
        /// <param name="count"></param>
        public Windows.Foundation.IAsyncOperation<LoadMoreItemsResult> LoadMoreItemsAsync(uint count)
        {
            Debug.WriteLine("LoadMoreItemsAsync is called with current count parameter {0}.", count);

            var dispatcher = Windows.UI.Core.CoreWindow.GetForCurrentThread().Dispatcher;

            // Führe folgenden Code als asynchronen Code aus.
            return Task.Run<LoadMoreItemsResult>(async () =>
                {
                    uint resultCount = 0;
                    var result = await source.GetPagedItems(resourceId, currentPage++, itemsPerPage);

                    if(result != null)
                        Debug.WriteLine("GetPagedItems returned an enumerable with {0} entries.", result.Count());

                    // Wenn das Resultat der GetPagedItems Methode null ist, oder keinen Eintrag hat, dann 
                    // können keine weiteren Einträge geladen werden.
                    if(result == null || result.Count() == 0)
                    {
                        Debug.WriteLine("No more items to load.");
                        hasMoreItems = false;
                    }
                    else
                    {
                        // Aktualisiere Count
                        resultCount = (uint)result.Count();

                        await dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                // Füge die neu hinzugekommenen Einträge der Collection hinzu.
                                foreach(I item in result)
                                {
                                    this.Add(item);
                                }
                            });
                    }

                    // Gebe LoadMoreItemsResult Instanz zurück.
                    return new LoadMoreItemsResult() { Count = resultCount };
                }).AsAsyncOperation<LoadMoreItemsResult>();
        }
    }
}
