using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Exceptions
{
    public class ClientException : Exception
    {
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
        /// Generiert eine neue ClientException mit dem gegebenen Fehlercode und einer Beschreibung des
        /// aufgetretenen Fehlers.
        /// </summary>
        /// <param name="errorCode">Der Fehlercode, durch den der aufgetretene Fehler identifiziert ist.</param>
        /// <param name="message">Eine Beschreibung des aufgetretenen Fehlers.</param>
        public ClientException(int errorCode, string message)
            : base(message)
        {
            this.errorCode = errorCode;
        }
    }
}
