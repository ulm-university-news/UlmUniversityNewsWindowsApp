using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Exceptions
{
    public class APIException : Exception
    {
        private int responseStatusCode;
        /// <summary>
        /// Der Status Code, der in der HTTP Response übermittelt wurde.
        /// </summary>
        public int ResponseStatusCode
        {
            get { return responseStatusCode; }
            set { responseStatusCode = value; }
        }
            
        private int errorCode;
        /// <summary>
        /// Der vom Server übermittelte Error Code.
        /// </summary>
        public int ErrorCode
        {
            get { return errorCode; }
            set { errorCode = value; }
        }
        
        /// <summary>
        /// Neue APIException mit gegebenem HTTP Status Code und Error Code.
        /// </summary>
        /// <param name="statusCode">Der in der HTTP Response übermittelte Status Code.</param>
        /// <param name="errorCode">Der vom Server übermittelte Error Code.</param>
        public APIException(int statusCode, int errorCode)
        {
            this.responseStatusCode = statusCode;
            this.errorCode = errorCode;
        }

        /// <summary>
        /// Neue APIException mit gegebenem HTTP Status Code und Error Code.
        /// </summary>
        /// <param name="statusCode">Der in der HTTP Response übermittelte Status Code.</param>
        /// <param name="errorCode">Der vom Server übermittelte Error Code.</param>
        /// <param name="message">Beschreibung des aufgetretenen Fehlers.</param>
        public APIException(int statusCode, int errorCode, string message)
            : base(message)
        {
            this.responseStatusCode = statusCode;
            this.errorCode = errorCode;
        }

    }
}
