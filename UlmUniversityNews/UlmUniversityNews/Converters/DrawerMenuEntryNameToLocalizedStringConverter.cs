using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Diese Konverterklasse wandelt einen Ressourcenschlüssel für einen Drawer Menüeintrag in einen
    /// String um, der abhängig von der vom Nutzer bevorzugten Sprache zurückgeliefert wird.
    /// </summary>
    public class DrawerMenuEntryNameToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string menuEntryName = value as string;
            string localizedString = string.Empty;

            //System.Diagnostics.Debug.WriteLine("Im in the drawer menu localized string converter, the value is: {0}.", menuEntryName);
            
            if(menuEntryName != null)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                localizedString = loader.GetString(menuEntryName);
            }

            return localizedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
