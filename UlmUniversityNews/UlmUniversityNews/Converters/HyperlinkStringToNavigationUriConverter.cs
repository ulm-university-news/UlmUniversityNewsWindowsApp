using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche einen String, der eine Webadresse darstellt, in ein Objekt vom Typ Uri
    /// umwandelt. Das Uri Objekt kann dann in einem Hyperlink an das NavigateUri Property gebunden werden.
    /// </summary>
    public class HyperlinkStringToNavigationUriConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string hyperlinkString = value as string;
            Uri hyperlinkUri = null;

            if (hyperlinkString != null)
            {
                if (!hyperlinkString.ToLower().StartsWith("http://") && !hyperlinkString.ToLower().StartsWith("https://"))
                {
                    hyperlinkString = "http://" + hyperlinkString;
                }

                // Prüfe, ob String eine valide URL darstellt.
                if (Uri.IsWellFormedUriString(hyperlinkString, UriKind.Absolute))
                {
                    Uri.TryCreate(hyperlinkString, UriKind.RelativeOrAbsolute, out hyperlinkUri);
                }
            }

            return hyperlinkUri;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
