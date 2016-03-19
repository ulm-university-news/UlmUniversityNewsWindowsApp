using DataHandlingLayer.DataModel.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche einen Wert des Enums Priority auf einen String abbildet.
    /// Der String wird abhängig von der bevorzugten Sprache des Nutzers zurückgeliefert.
    /// </summary>
    public class PriorityEnumToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            string localizedString = string.Empty; 
            Priority priority = (Priority)value;
            
            switch (priority)
            {
                case Priority.NORMAL:
                    localizedString = loader.GetString("PriorityEnumConverterStringPrioNormal");
                    break;
                case Priority.HIGH:
                    localizedString = loader.GetString("PriorityEnumConverterStringPrioHigh");
                    break;
            }
            
            return localizedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
