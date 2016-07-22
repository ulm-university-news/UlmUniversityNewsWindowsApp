using DataHandlingLayer.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter, der die Hintergrundfarbe des Drawer Menüs bestimmt, abhängig von den Daten der
    /// übergebenen Gruppeninstanz.
    /// </summary>
    public class GroupToDrawerMenuBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Group group = value as Group;
            if (group != null)
            {
                // Sonderbehandlung, wenn Gruppe als gelöscht markiert wurde.
                if (group.Deleted)
                {
                    return App.Current.Resources["UUNChannelMarkedAsDeletedColor"];
                }
            }

            return App.Current.Resources["UniUlmMainBackgroundColor"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
