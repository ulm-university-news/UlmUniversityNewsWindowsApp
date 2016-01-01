using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Wenn man ein ItemClick Event über ein Behaviour auf ein Kommando abbildet, so wird als Parameter
    /// das EventArgs Objekt des Events übergeben. Es handelt sich hierbei um ein Objekt des 
    /// Windows.UI.Xaml.Controls namespace, das man nicht unbedingt aus einer MVVM Perspektive an 
    /// das ViewModel geben würde. Besser wäre es direkt das angeklickte Objekt zu übergeben.
    /// Der Converter extrahiert das geklickte Objekt aus dem EventArgs Objekt, so dass
    /// dieses Objekt dann weiter als Parameter des Kommandos übergeben wird.
    /// </summary>
    public class ItemClickedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var args = value as ItemClickEventArgs;

            if(args != null)
            {
                return args.ClickedItem;
            }

            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
