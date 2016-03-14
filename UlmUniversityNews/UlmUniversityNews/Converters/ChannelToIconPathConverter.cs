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
    /// Leitet aus dem ChannelType (und falls nötig der Faculty) den Pfad zum zugehörigen Icon im Assets Ordner ab.
    /// </summary>
    public class ChannelToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Channel channel = value as Channel;
            string iconPath = string.Empty;

            if(channel != null)
            {
                ChannelType type = channel.Type;

                //System.Diagnostics.Debug.WriteLine("In ChannelTypeToIconPathConverter the type is: {0}.", type.ToString());

                switch (type)
                {
                    case ChannelType.LECTURE:
                        Lecture lecture = channel as Lecture;
                        if (lecture != null)
                        {
                            // Frage die Fakultät der Vorlesung ab.
                            Faculty faculty = lecture.Faculty;

                            //System.Diagnostics.Debug.WriteLine("In ChannelTypeToIconPathConverter the faculty is: {0}.", faculty.ToString());

                            switch (faculty)
                            {
                                case Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY:
                                    iconPath = "/Assets/channelIcons/ic_lecture_informatics.png";
                                    break;
                                case Faculty.MATHEMATICS_ECONOMICS:
                                    iconPath = "/Assets/channelIcons/ic_lecture_mathematics.png";
                                    break;
                                case Faculty.MEDICINES:
                                    iconPath = "/Assets/channelIcons/ic_lecture_medicines.png";
                                    break;
                                case Faculty.NATURAL_SCIENCES:
                                    iconPath = "/Assets/channelIcons/ic_lecture_science.png";
                                    break;
                            }
                        }
                        break;
                    case ChannelType.EVENT:
                        iconPath = "/Assets/channelIcons/ic_event.png";
                        break;
                    case ChannelType.SPORTS:
                        iconPath = "/Assets/channelIcons/ic_sport.png";
                        break;
                    case ChannelType.STUDENT_GROUP:
                        iconPath = "/Assets/channelIcons/ic_group.png";
                        break;
                    case ChannelType.OTHER:
                        iconPath = "/Assets/channelIcons/ic_other.png";
                        break;
                }

                if (channel.Deleted)
                {
                    iconPath = "/Assets/channelIcons/ic_deleted.png";
                }
            }

            //System.Diagnostics.Debug.WriteLine("Output of ChannelTypeToIconPathConverter is: {0}.", iconPath);
            return iconPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
