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
            string typeString = string.Empty;

            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            switch(type)
            {
                case ChannelType.LECTURE:
                    typeString = loader.GetString("ListItemChannelTypeLectureString");
                    break;
                case ChannelType.EVENT:
                    typeString = loader.GetString("ListItemChannelTypeEventString");
                    break;
                case ChannelType.SPORTS:
                    typeString = loader.GetString("ListItemChannelTypeSportsString");
                    break;
                case ChannelType.STUDENT_GROUP:
                    typeString = loader.GetString("ListItemChannelTypeStudentGroupString");
                    break;
                case ChannelType.OTHER:
                    typeString = loader.GetString("ListItemChannelTypeOtherString");
                    break;
            }

            return typeString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            ChannelType type;
            Enum.TryParse(value as string, out type);
            return type;
        }
    }
}
