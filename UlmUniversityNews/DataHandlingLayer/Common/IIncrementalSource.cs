using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Common
{
    /// <summary>
    /// Interface, welches eine Quelle für das inkrementelle Laden von Items des Typ T angibt.
    /// </summary>
    /// <typeparam name="T">Der Typ des Items, das dynamisch geladen werden soll.</typeparam>
    public interface IIncrementalSource<T>
    {
        Task<IEnumerable<T>> GetPagedItems(int resourceId, int pageIndex, int pageSize);
    }
}
