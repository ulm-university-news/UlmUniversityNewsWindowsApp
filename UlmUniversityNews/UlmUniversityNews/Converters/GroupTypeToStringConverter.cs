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
    /// Wandelt ein Objekt vom Typ GroupType (Enum) in einen String um.
    /// </summary>
    public class GroupTypeToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            GroupType groupType = (GroupType)value;
            string typeString = string.Empty;

            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            switch (groupType)
            {
                case GroupType.WORKING:
                    typeString = loader.GetString("ListItemGroupTypeWorkingString");
                    break;
                case GroupType.TUTORIAL:
                    typeString = loader.GetString("ListItemGroupTypeTutorialString");
                    break;
            }

            return typeString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
