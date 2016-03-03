using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Constants
{
    public static class Constants
    {
        // Local Settings Keys und Values bezüglich des Zugriffs auf den LockScreen/Background Tasks.
        public const string AccessToLockScreenKey = "accessToLockScreen";
        public const string AccessToLockScreenDenied = "accessToLockScreenDenied";
        public const string AccessToLockScreenGranted = "accessToLockScreenGranted";
        public const string ShowLockScreenMessageKey = "showLockScreenMsg";
        public const string ShowLockScreenMessageYes = "yes";
        public const string ShowLockScreenMessageNo = "no";


        // Local Settings Keys und Values bezüglich Login Status. Das ist relevant für die Wiederherstellung
        // des App Zustand bei einer durch das System verursachten Terminierung der Anwendung.
        // Die App soll nicht wieder in den Zustand wechseln, wenn ein Moderator zum Zeitpunkt der Terminierung eingeloggt war.
        public const string ModeratorLoggedInStatusKey = "loginStatus";
        public const int ModeratorLoggedIn = 1;
        public const int ModeratorNotLoggedIn = 0;
    }
}
