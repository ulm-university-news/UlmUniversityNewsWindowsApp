using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welches den Semester-String eines Kanals auf ein neues String Format konvertiert.
    /// Der zurückgegebene String hängt von der aktuell als bevorzugt eingestellten Sprache ab.
    /// </summary>
    public class ChannelTermToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string term = value as string;
            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");

            if (term != null)
            {
                if (term.StartsWith("S"))
                {
                    term = term.Remove(0, 1);
                    term = loader.GetString("ChannelTermStringConverterSummer") + " " + term;
                }
                else if (term.StartsWith("W"))
                {
                    term = term.Remove(0, 1);
                    term = loader.GetString("ChannelTermStringConverterWinter") + " " + term;
                }
            }

            return term;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
