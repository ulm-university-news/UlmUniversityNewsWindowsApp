using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter Klasse, welche ein für ein Reminder Objekt definiertes Intervall von einem 
    /// ganzzahligen Intervall-Wert in eine Intervallbeschreibung umwandelt. Die Intervallbeschreibung
    /// wird abhängig von der bevorzugten Sprache des Nutzers zurückgeliefert.
    /// </summary>
    public class ReminderIntervalToLocalizedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            string localizedString = string.Empty;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();

            if (value != null)
            {
                int intervalValue = (int)value;

                // Sonderfall Interval = 0.
                if (intervalValue == 0)
                {
                    // OneTime Reminder.
                    loader.GetString("ReminderIntervalConverterStringOneTimeReminderDesc");
                }

                // Berechne Intervall in Tage um.
                int days = intervalValue / (60 * 60 * 24);
 
                // Behandle Sonderfall Wochen:
                if (days == 7)
                {
                    localizedString = loader.GetString("ReminderIntervalConverterStringOneWeekDesc");
                    return localizedString;
                }
                else if (days == 14 || days == 21 || days == 28)
                {
                    localizedString = loader.GetString("ReminderIntervalConverterStringMultipleWeeksDesc1") + 
                        " " + (days % 7) + " " +
                        loader.GetString("ReminderIntervalConverterStringMultipleWeeksDesc2");
                    return localizedString;
                }

                // Behandle Tage.
                if (days == 1)
                {
                    localizedString = loader.GetString("ReminderIntervalConverterStringOneDayDesc");
                }
                else
                {
                    localizedString = loader.GetString("ReminderIntervalConverterStringMultipleDaysDesc1") +
                        " " + days + " " +
                    loader.GetString("ReminderIntervalConverterStringMultipleDaysDesc2");
                }
            }

            return localizedString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
