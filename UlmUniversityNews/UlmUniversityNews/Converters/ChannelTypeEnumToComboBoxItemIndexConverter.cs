using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using DataHandlingLayer.DataModel.Enums;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, um einen Wert des Enums ChannelType auf den Index des entsprechenden ComboBoxItems
    /// abzubilden und umgekehrt.
    /// </summary>
    public class ChannelTypeEnumToComboBoxItemIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value != null)
            {
                // Wandle Enum-Wert in Index um.
                ChannelType channelType;
                bool successful = Enum.TryParse(value.ToString(), true, out channelType);
                if (successful)
                {
                    return (int)channelType;
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
            ChannelType channeltype = (ChannelType)Enum.ToObject(typeof(ChannelType), value);
            return channeltype;
        }
    }
}
