using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Exceptions
{
    /// <summary>
    /// Diese Exception wird geworfen von den Datenbankklassen, wenn eine Datenbankoperation auf der 
    /// lokalen Datenbank mit einem Fehler abgebrochen wurde.
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// Erzeugt eine neue DatabaseException mit einer gegebenen Fehlernachricht.
        /// </summary>
        /// <param name="message">Die Fehlernachricht.</param>
        public DatabaseException(string message)
            : base(message)
        {

        }

        /// <summary>
        /// Erzeugt eine neue DatabaseException mit einer gegebenen Fehlernachricht.
        /// </summary>
        /// <param name="message">Die Fehlernachricht.</param>
        /// <param name="inner">Eine vorausgehende Exception.</param>
        public DatabaseException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
