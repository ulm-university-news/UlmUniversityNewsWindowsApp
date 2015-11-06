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
