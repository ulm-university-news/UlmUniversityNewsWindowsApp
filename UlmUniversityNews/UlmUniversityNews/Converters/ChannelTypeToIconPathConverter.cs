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
    /// Wandelt einen ChannelType ab auf einen Pfad zum zugehörigen Icon im Assets Ordner.
    /// </summary>
    public class ChannelTypeToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            ChannelType type = (ChannelType)value;

            System.Diagnostics.Debug.WriteLine("In ChannelTypeToIconPathConverter the type is: {0} and the parameter is {1}.", type.ToString(), parameter.ToString());

            string iconPath = string.Empty;
            switch (type)
            {
                case ChannelType.LECTURE:
                    if(parameter != null)
                    {
                        Lecture lecture = parameter as Lecture;
                        if(lecture != null)
                        {
                            // Frage die Fakultät der Vorlesung ab.
                            Faculty faculty = lecture.Faculty;

                            System.Diagnostics.Debug.WriteLine("In ChannelTypeToIconPathConverter the faculty is: {0}.", faculty.ToString());

                            switch (faculty)
                            {
                                case Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY:
                                    iconPath = "/Assets/ChannelIcons/V_informatik.png";
                                    break;
                                case Faculty.MATHEMATICS_ECONOMICS:
                                    iconPath = "/Assets/ChannelIcons/V_mathe.png";
                                    break;
                                case Faculty.MEDICINES:
                                    iconPath = "/Assets/ChannelIcons/V_medicine.png";
                                    break;
                                case Faculty.NATURAL_SCIENCES:
                                    iconPath = "/Assets/ChannelIcons/V_science.png";
                                    break;
                            }
                        }
                    }
                    break;
                case ChannelType.EVENT:
                    iconPath = "/Assets/ChannelIcons/event.png";
                    break;
                case ChannelType.SPORTS:
                    iconPath = "/Assets/ChannelIcons/sport.png";
                    break;
                case ChannelType.STUDENT_GROUP:
                    iconPath = "/Assets/ChannelIcons/student_group.png";
                    break;
                case ChannelType.OTHER:
                    iconPath = "/Assets/ChannelIcons/other.png";
                    break;
            }

            System.Diagnostics.Debug.WriteLine("Output of ChannelTypeToIconPathConverter is: {0}.", iconPath);
            return iconPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
