using DataHandlingLayer.DataModel;
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
    /// Die Converter Klasse ChannelTypeToDrawerMenuBackgroundConverter bildet die Informationen
    /// des Kanals auf eine Hintergrundfarbe für das Drawer Menü ab. Somit kann realisiert werden, dass
    /// beispielsweise die Hintergrundfarbe bei einem Kanal vom Typ Vorlesung die Farbe der zugehörigen Fakultät
    /// annimmt.
    /// </summary>
    public class ChannelToDrawerMenuBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Channel channel = value as Channel;
            if(channel != null)
            {
                // Sonderbehandlung, wenn Kanal den Typ "Lecture" hat.
                if(channel.Type == ChannelType.LECTURE)
                {
                    // Wandle Objekt um in ein Objekt vom Typ Lecture.
                    Lecture lecture = channel as Lecture;
                    if(lecture != null)
                    {
                        // Gebe Farbe abhängig von der zugehörigen Fakultät zurück.
                        switch(lecture.Faculty)
                        {
                            case Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY:
                                return App.Current.Resources["UniUlmFacultyColorInformaticsAndEngineering"];
                            case Faculty.MATHEMATICS_ECONOMICS:
                                return App.Current.Resources["UniUlmFacultyColorMathematicsAndEconomics"];
                            case Faculty.MEDICINES:
                                return App.Current.Resources["UniUlmFacultyColorMedicine"];
                            case Faculty.NATURAL_SCIENCES:
                                return App.Current.Resources["UniUlmFacultyColorNaturalSciences"];
                        }
                    }
                }
            }
            return App.Current.Resources["UniUlmMainBackgroundColor"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
