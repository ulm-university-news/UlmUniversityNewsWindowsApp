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

        // Längenbeschränkungen:
        public const int MaxChannelNameLength = 45;
        public const int MinChannelNameLength = 3;
        public const int MaxChannelDescriptionLength = 500;
        public const int MaxChannelLocationsInfoLength = 120;
        public const int MaxChannelDatesInfoLength = 150;
        public const int MaxChannelContactsInfoLength = 120;
        public const int MaxChannelWebsiteInfoLength = 500;
        public const int MaxChannelLecturerInfoLength = 120;
        public const int MaxChannelAssistantInfoLength = 120;
        public const int MaxChannelStartDateInfoLength = 45;
        public const int MaxChannelEndDateInfoLength = 45;
        public const int MaxChannelEventCostInfoLength = 150;
        public const int MaxChannelEventOrganizerInfoLength = 120;
        public const int MaxChannelSportsCostInfoLength = 150;
        public const int MaxChannelSportsNrOfParticipantsInfoLength = 150;

        public const int MinUsernameLength = 3;
        public const int MaxUsernameLength = 35;

        public const int MaxAnnouncementTitleLength = 45;
        public const int MaxAnnouncementContentLength = 500;

        // Patterns
        public const string TermPattern = @"^[W,S][0-9]{4}$";
        public const string UserNamePattern = @"^[-_a-zA-Z0-9]+$";
        public const string ChannelNamePattern = @"^[-!?_-öÖäÄüÜßa-zA-Z0-9\s]+$";
    }
}
