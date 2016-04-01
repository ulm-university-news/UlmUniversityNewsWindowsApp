using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    public class StatusBarInformationToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string inputString = value as string;
            string outputString = string.Empty;
            if (inputString != null)
            {
                var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
                if (inputString != string.Empty)
                    outputString = loader.GetString(inputString);

                if (outputString == null || outputString == string.Empty)
                {
                    outputString = inputString;
                }
            }

            return outputString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
