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
    /// Konverter-Klasse, um einen Wert des Enums GroupType auf den Index des entsprechenden ComboBoxItems
    /// abzubilden und umgekehrt.
    /// </summary>
    public class GroupTypeEnumToComboBoxItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                // Wandle Enum-Wert in Index um.
                GroupType groupType;
                bool successful = Enum.TryParse(value.ToString(), true, out groupType);
                if (successful)
                {
                    return (int)groupType;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't parse enum value in GroupTypeEnumToComboBoxItemIndexConverter.");
                }
            }
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Extrahiere Enum-Wert aus Index.
            GroupType groupType = (GroupType)Enum.ToObject(typeof(GroupType), value);
            return groupType;
        }
    }
}
