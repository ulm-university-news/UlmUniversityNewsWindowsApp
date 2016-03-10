using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche einen Boolean-Wert auf einen String abbildet. Es wird true - "Ja",
    /// false - "Nein" zurückgegeben, abhängig jedoch von der bevorzugten Sprache. Der Konverter ist relevant
    /// für die Anzeige von Boolean Feldern wie "Überspringe nächsten Reminder Termin".
    /// </summary>
    public class BooleanValueToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string localizedString = string.Empty;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            if (value != null)
            {
                bool input = (bool)value;
                if (input)
                {
                    localizedString = loader.GetString("BooleanValueTrueString");
                }
                else
                {
                    localizedString = loader.GetString("BooleanValueFalseString");
                }
            }

            return localizedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
