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
    /// Konverter, der einen Wert aus dem Enum Faculty auf einen String abbildet. Der Fakultätsnamen
    /// wird dabei unter Berücksichtigung der eingestellten Sprache ausgegeben.
    /// </summary>
    public class FacultyToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Faculty faculty = (Faculty)value;
            string facultyString = string.Empty;

            var loader = Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse("Resources");
            switch(faculty)
            {
                case Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY:
                    facultyString = loader.GetString("ListItemFacultyEngineeringAndComputerScienceString");
                    break;
                case Faculty.MATHEMATICS_ECONOMICS:
                    facultyString = loader.GetString("ListItemFacultyMathematicsAndEconomicsString");
                    break;
                case Faculty.MEDICINES:
                    facultyString = loader.GetString("ListItemFacultyMedicinesString");
                    break;
                case Faculty.NATURAL_SCIENCES:
                    facultyString = loader.GetString("ListItemFacultyNaturalSciencesString");
                    break;
            }

            return facultyString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            Faculty faculty;
            Enum.TryParse(value as string, out faculty);
            return faculty;
        }
    }
}
