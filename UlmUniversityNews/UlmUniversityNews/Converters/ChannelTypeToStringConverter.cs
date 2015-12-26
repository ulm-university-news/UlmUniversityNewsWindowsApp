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
    /// Wandelt ein Objekt vom Typ ChannelType (Enum) in einen String um.
    /// </summary>
    public class ChannelTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ChannelType type = (ChannelType)value;
            return type.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ChannelType type;
            Enum.TryParse(value as string, out type);
            return type;
        }
    }
}
