using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    public class ValidationMessageOnLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string localizedString = string.Empty;
            if(value != null)
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                string valueString = value as string;
                localizedString = loader.GetString(valueString);
            }
            return localizedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return value;
        }
    }
}
