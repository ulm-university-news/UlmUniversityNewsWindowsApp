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

        public const int ServerDatabaseFailure = 5000;

        // Allgemeingültige Fehlercodes.
        public const int BadRequest = 400;
        public const int NotFound = 404;
        public const int MethodNotAllowed = 405;
        public const int UnsupportedMediaType = 415;

        // Windows Phone App spezifische Fehlercodes.
        public const int ServerUnreachable = 6000;
        public const int JsonParserError = 6001;
        public const int WnsChannelInitializationFailed = 6002;
        public const int LocalDatabaseException = 6003;
    }
}
