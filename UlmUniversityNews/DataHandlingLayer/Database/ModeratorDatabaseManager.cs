using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Database
{
    public class ModeratorDatabaseManager
    {
        /// <summary>
        /// Speichere die Daten des übergebenen Moderator-Objekt in der Datenbank ab.
        /// </summary>
        /// <param name="moderator">Das Objekt mit den Daten des Moderators.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void StoreModerator(Moderator moderator)
        {
            if(moderator == null)
            {
                Debug.WriteLine("No valid moderator object is passed to the StoreModerator method.");
                return;
            }

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                using(var insertStmt = conn.Prepare(@"INSERT INTO Moderator (Id, FirstName, LastName, Email) 
                    VALUES (?,?,?,?);"))
                {
                    insertStmt.Bind(1, moderator.Id);
                    insertStmt.Bind(2, moderator.FirstName);
                    insertStmt.Bind(3, moderator.LastName);
                    insertStmt.Bind(4, moderator.Email);

                    insertStmt.Step();
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException occurred in StoreModerator. The message is: {0}.", sqlEx.Message);
                throw new DatabaseException("Moderator could not be stored.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception occurred in StoreModerator. The message is: {0} and the stack trace is {1}.",
                    ex.Message, 
                    ex.StackTrace);
                throw new DatabaseException("Moderator could not be stored.");
            }
        }

        /// <summary>
        /// Gibt an, ob ein Eintrag zu dem Moderator mit der angegebenen Id existiert.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators, für den geprüft wird, ob ein Eintrag existiert.</param>
        /// <returns>Liefert true zurück, wenn ein Eintrag für den Moderator existiert, ansonsten false.</returns>
        public bool IsModeratorStored(int moderatorId)
        {
            bool isStored = false;
            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                using (var stmt = conn.Prepare(@"SELECT Id FROM Moderator WHERE Id=?;"))
                {
                    stmt.Bind(1, moderatorId);

                    if(stmt.Step() == SQLiteResult.ROW)
                    {
                        isStored = true;
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException occurred in IsModeratorStored. The message is: {0}.", sqlEx.Message);
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception occurred in IsModeratorStored. The message is: {0} and the stack trace is {1}.",
                    ex.Message,
                    ex.StackTrace);
                return false;
            }
            return isStored;
        }

        /// <summary>
        /// Holt die Daten des Moderators mit der angegebenen Id aus der Datenbank und gibt sie zurück.
        /// </summary>
        /// <param name="channelId">Die Id des Moderators, der abgerufen werden soll.</param>
        /// <returns>Ein Objekt der Klasse Moderator, oder null falls der Eintrag nicht in der Datenbank ist.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf des Datensatzes fehlschlägt.</exception>
        public Moderator GetModerator(int moderatorId)
        {
            Moderator moderator = null;
            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                using (var stmt = conn.Prepare(@"SELECT * FROM Moderator WHERE Id=?;"))
                {
                    stmt.Bind(1, moderatorId);

                    if(stmt.Step() == SQLiteResult.ROW)
                    {
                        int id = Convert.ToInt32(stmt["Id"]);
                        string firstName = (string)stmt["FirstName"];
                        string lastName = (string)stmt["LastName"];
                        string email = (string)stmt["Email"];

                        moderator = new Moderator()
                        {
                            Id = id,
                            FirstName = firstName,
                            LastName = lastName,
                            Email = email
                        };
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException occurred in GetModerator. The message is: {0}.", sqlEx.Message);
                throw new DatabaseException("Could not retrieve Moderator.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception occurred in IsModeratorStored. The message is: {0} and the stack trace is {1}.",
                    ex.Message,
                    ex.StackTrace);
                throw new DatabaseException("Could not retrieve Moderator.");
            }
            return moderator;
        }
    }
}
