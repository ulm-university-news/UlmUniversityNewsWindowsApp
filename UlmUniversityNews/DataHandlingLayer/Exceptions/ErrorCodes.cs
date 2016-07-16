using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Exceptions
{
    public static class ErrorCodes
    {
        // Fehlercodes (ErrorCodes).
        public const int UserNotFound = 1000;
        public const int UserForbidden = 1001;
        public const int UserDataIncomplete = 1002;
        public const int UserNameInvalid = 1003;
        public const int UserPushTokenInvalid = 1004;

        // Moderator:
        public const int ModeratorNotFound = 2000;
        public const int ModeratorForbidden = 2001;
        public const int ModeratorDataIncomplete = 2002;
        public const int ModeratorInvalidName = 2003;
        public const int ModeratorInvalidFirstName = 2004;
        public const int ModeratorInvalidLastName = 2005;
        public const int ModeratorInvalidEmail = 2006;
        public const int ModeratorInvalidPassword = 2007;
        public const int ModeratorInvalidMotivation = 2008;
        public const int ModeratorNameAlreadyExists = 2009;
        public const int ModeratorDeleted = 2010;
        public const int ModeratorLocked = 2011;
        public const int ModeratorUnauthorized = 2012;

        // Kanal
        public const int ChannelNotFound = 3000;
        public const int ChannelDataIncomplete = 3002;
        public const int ChannelInvalidName = 3003;
        public const int ChannelInvalidTerm = 3004;
        public const int ChannelInvalidContacts = 3005;
        public const int ChannelInvalidLocations = 3006;
        public const int ChannelInvalidDescription = 3007;
        public const int ChannelInvalidDates = 3008;
        public const int ChannelInvalidType = 3009;
        public const int ChannelInvalidLecturer = 3010;
        public const int ChannelInvalidAssistant = 3011;
        public const int ChannelInvalidCost = 3012;
        public const int ChannelInvalidParticipants = 3013;
        public const int ChannelInvalidOrganizer = 3014;
        public const int ChannelInvalidWebsite = 3015;
        public const int ChannelInvalidStartDate = 3016;
        public const int ChannelInvalidEndDate = 3017;
        public const int ChannelNameAlreadyExists = 3018;

        // Announcements:
        public const int AnnouncementNotFound = 3100;
        public const int AnnouncementDataIncomplete = 3102;
        public const int AnnouncementInvalidText = 3103;
        public const int AnnouncementInvalidTitle = 3104;

        // Reminder
        public const int ReminderNotFound = 3200;
        public const int ReminderDataIncomplete = 3202;
        public const int ReminderInvalidText = 3203;
        public const int ReminderInvalidTitle = 3204;
        public const int ReminderInvalidInterval = 3205;

        // Group
        public const int GroupNotFound = 4000;
        public const int GroupDataIncomplete = 4002;
        public const int GroupInvalidName = 4003;
        public const int GroupInvalidPassword = 4004;
        public const int GroupInvalidDescription = 4005;
        public const int GroupInvalidTerm = 4006;
        public const int GroupInvalidGroupAdmin = 4007;
        public const int GroupIncorrectPassword = 4008;
        public const int GroupMissingPassword = 4009;
        public const int GroupParticipantNotFound = 4010;
        public const int GroupAdminNotAllowedToExit = 4011;
        public const int GroupAdminRightsTransferHasFailed = 4012;

        public const int ConversationNotFound = 4100;
        public const int ConversationDataIncomplete = 4102;
        public const int ConversationInvalidTitle = 4103;
        public const int ConversationStorageFailedDueToMissingAdmin = 4104;
        public const int ConversationIsClosed = 4105;

        public const int BallotNotFound = 4300;
        public const int BallotDataIncomplete = 4302;
        public const int BallotInvalidTitle = 4303;
        public const int BallotInvalidDescription = 4304;
        public const int BallotClosed = 4305;
        public const int BallotUserHasAlreadyVoted = 4306;

        public const int ServerDatabaseFailure = 5000;

        // Allgemeingültige Fehlercodes (von HTTP).
        public const int BadRequest = 400;
        public const int Unauthorized = 401;
        public const int NotFound = 404;
        public const int MethodNotAllowed = 405;
        public const int Gone = 410;
        public const int UnsupportedMediaType = 415;
        public const int Locked = 423;


        // Windows Phone App spezifische Fehlercodes.
        public const int ServerUnreachable = 6000;
        public const int JsonParserError = 6001;
        public const int WnsChannelInitializationFailed = 6002;
        public const int LocalDatabaseException = 6003;
    }
}
