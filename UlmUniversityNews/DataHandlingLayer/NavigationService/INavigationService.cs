using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.NavigationService
{
    public interface INavigationService
    {
        void Navigate(string pageKey);
        void Navigate(string pageKey, object parameter);
        bool CanGoBack();
        void GoBack();
        void RemoveEntryFromBackStack();
        void ClearBackStack();
    }
}
