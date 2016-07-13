using DataHandlingLayer.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;

namespace UlmUniversityNews.Converters
{
    /// <summary>
    /// Konverter-Klasse, welche die Daten einer Abstimmung auf einen Pfad für ein
    /// entsprechendes Icon abbildet. Benötigt das Nutzerobjekt des aktuellen lokalen Nutzers, um prüfen zu können,
    /// ob es sich beim lokalen Nutzer um den Administrator der Abstimmung handelt.
    /// </summary>
    public class BallotToIconPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Ballot ballot = value as Ballot;
            string iconPath = string.Empty;

            if (ballot != null)
            {
                // Prüfe, ob der lokale Nutzer Administrator dieser Abstimmung ist.
                User currentLocalUser = LocalUser.GetInstance().GetCachedLocalUserObject();
                bool isAdmin = false;
                if (ballot.AdminId == currentLocalUser.Id)
                    isAdmin = true;

                bool closedFieldHasValue = ballot.IsClosed.HasValue;
                if (closedFieldHasValue && ballot.IsClosed == false && !isAdmin)
                {
                    iconPath = "/Assets/ballotIcons/abstimmung.png";
                }
                else if (closedFieldHasValue && ballot.IsClosed == false && isAdmin)
                {
                    iconPath = "/Assets/ballotIcons/abstimmung_admin.png";
                }
                else if (closedFieldHasValue && ballot.IsClosed == true && !isAdmin)
                {
                    iconPath = "/Assets/ballotIcons/ballot_closed_white.png";
                }
                else if (closedFieldHasValue && ballot.IsClosed == true && isAdmin)
                {
                    iconPath = "/Assets/ballotIcons/ballot_closed_admin_white.png";
                }
                else
                {
                    iconPath = "/Assets/ballotIcons/abstimmung.png";
                }
            }

            return iconPath;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
