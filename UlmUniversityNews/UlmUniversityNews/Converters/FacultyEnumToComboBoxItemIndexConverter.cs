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
    /// Konverter-Klasse, um einen Wert des Enums Faculty auf den Index des entsprechenden ComboBoxItems
    /// abzubilden und umgekehrt.
    /// </summary>
    public class FacultyEnumToComboBoxItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                // Wandle Enum-Wert in Index um.
                Faculty faculty;
                bool successful = Enum.TryParse(value.ToString(), true, out faculty);
                if (successful)
                {
                    return (int)faculty;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Couldn't parse enum value in ChannelTypeEnumToComboBoxItemIndexConverter.");
                }
            }
            return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            // Extrahiere Enum - Wert aus Index.
            Faculty faculty = (Faculty)Enum.ToObject(typeof(Faculty), value);
            return faculty;
        }
    }
}
