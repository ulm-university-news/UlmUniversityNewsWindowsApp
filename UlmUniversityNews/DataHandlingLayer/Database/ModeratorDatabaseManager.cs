using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
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

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var insertStmt = conn.Prepare(@"INSERT INTO Moderator (Id, FirstName, LastName, Email) 
                            VALUES (?,?,?,?);"))
                        {
                            insertStmt.Bind(1, moderator.Id);
                            insertStmt.Bind(2, moderator.FirstName);
                            insertStmt.Bind(3, moderator.LastName);
                            insertStmt.Bind(4, moderator.Email);

                            insertStmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException occurred in StoreModerator. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Moderator could not be stored.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception occurred in StoreModerator. The message is: {0} and the stack trace is {1}.",
                            ex.Message,
                            ex.StackTrace);
                        throw new DatabaseException("Moderator could not be stored.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende des using Block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
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

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"SELECT Id FROM Moderator WHERE Id=?;"))
                        {
                            stmt.Bind(1, moderatorId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                isStored = true;
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
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
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende des using Block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
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

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"SELECT * FROM Moderator WHERE Id=?;"))
                        {
                            stmt.Bind(1, moderatorId);

                            if (stmt.Step() == SQLiteResult.ROW)
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
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException occurred in GetModerator. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Could not retrieve Moderator.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception occurred in GetModerator. The message is: {0} and the stack trace is {1}.",
                            ex.Message,
                            ex.StackTrace);
                        throw new DatabaseException("Could not retrieve Moderator.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende des using Block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return moderator;
        }

        /// <summary>
        /// Aktualisiert den Datensatz des Moderators, der durch das übergebene Moderatorenobjekt
        /// identifiziert wird. Das übergebene Moderatorenobjekt enthält die neuen Daten des Moderators.
        /// </summary>
        /// <param name="newModerator">Das Moderator Objekt mit den neuen Daten.</param>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn die Aktualisierung fehlschlägt.</exception>
        public void UpdateModerator(Moderator newModerator)
        {
            if (newModerator == null)
                return;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"UPDATE Moderator 
                            SET FirstName=?, LastName=?, Email=? 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, newModerator.FirstName);
                            stmt.Bind(2, newModerator.LastName);
                            stmt.Bind(3, newModerator.Email);
                            stmt.Bind(4, newModerator.Id);

                            if (stmt.Step() == SQLiteResult.DONE)
                                Debug.WriteLine("Updated moderator with id {0}.", newModerator.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException occurred in UpdateModerator. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Could not update Moderator. Msg is: " + sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception occurred in UpdateModerator. The message is: {0} and the stack trace is {1}.",
                            ex.Message,
                            ex.StackTrace);
                        throw new DatabaseException("Could not update Moderator. Msg is: " + ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }
    }
}
