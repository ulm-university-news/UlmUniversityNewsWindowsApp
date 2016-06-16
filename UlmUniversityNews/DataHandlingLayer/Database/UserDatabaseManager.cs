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
    public class UserDatabaseManager
    {
        /// <summary>
        /// Erzeugt eine Instanz der Klasse UserDatabaseManager.
        /// </summary>
        public UserDatabaseManager()
        {

        }

        /// <summary>
        /// Gibt an, ob ein Datensatz eines Nutzer zu dieser Id existiert.
        /// </summary>
        /// <param name="userId">Die Id des Nutzers.</param>
        /// <returns>Liefert true, wenn der Datensatz existiert, ansonsten false.</returns>
        public bool IsUserStored(int userId)
        {
            Stopwatch sw = Stopwatch.StartNew();
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
                        string query = @"SELECT COUNT(*) AS amount
                            FROM User 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, userId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int amount = Convert.ToInt32(stmt["amount"]);

                                if (amount == 1)
                                    isStored = true;
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("IsUserStored: Failed to check whether user is stored. Msg is {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("IsUserStored: Failed to check whether user is stored. Msg is {0}.", ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            sw.Stop();
            Debug.WriteLine("IsUserStored: Required time: {0}.", sw.Elapsed.TotalMilliseconds);

            return isStored;
        }

        /// <summary>
        /// Speichert einen Nutzerdatensatz in der Datenbank ab.
        /// </summary>
        /// <param name="user">Der zu speichernde Datensatz als Objekt der Klasse User.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Speichern fehlschlägt.</exception>
        public void StoreUser(User user)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"INSERT INTO User (Id, Name) 
                            VALUES (?, ?);";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, user.Id);
                            stmt.Bind(2, user.Name);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to store user with id {0} to database.", user.Id);
                            else
                                Debug.WriteLine("Successfully stored user with id {0} to database.", user.Id);
                        }

                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("StoreUser: SQLiteException occurred. Message is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StoreUser: Exception occurred. Message is {0}.", ex.Message);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
        }

        /// <summary>
        /// Speichert eine Menge von Nutzer-Ressourcen in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="users">Eine Liste mit zu speichernden Datenobjekten vom Typ User.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn die Speicherung fehlschlägt.</exception>
        public void BulkInsertUsers(List<User> users)
        {
            if (users == null || users.Count == 0)
            {
                Debug.WriteLine("BulkInsertUsers: No valid users passed to the BulkInsertUsers method.");
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
                        // Starte eine Transaktion.
                        using (var statement = conn.Prepare("BEGIN TRANSACTION"))
                        {
                            statement.Step();
                        }

                        string query = @"INSERT INTO User (Id, Name) 
                            VALUES (?, ?);";

                        var insertStmt = conn.Prepare(query);

                        using (insertStmt)
                        {
                            foreach (User user in users)
                            {
                                insertStmt.Bind(1, user.Id);
                                insertStmt.Bind(2, user.Name);

                                insertStmt.Step();

                                // Reset für nächste Ausführung.
                                insertStmt.Reset();
                            }
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            if (statement.Step() == SQLiteResult.DONE)
                            {
                                Debug.WriteLine("BulkInsertUsers: Stored {0} users per Bulk Insert.", users.Count);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLException occured in BulkInsertUsers.");
                        Debug.WriteLine("SQLException: " + sqlEx.HResult + " and message: " + sqlEx.Message);

                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Bulk insert of users has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception occured in BulkInsertUsers.");
                        Debug.WriteLine("Exception: " + ex.HResult + " and message: " + ex.Message);

                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Bulk insert of users has failed.");
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

        /// <summary>
        /// Aktualisiert die Nutzerdatensätze in der Datenbank. Die zu 
        /// überarbeitenden Datensätze werden mit den neuen Daten als Paremter übergeben.
        /// </summary>
        /// <param name="users">Die Menge an zu aktualisierenden Nutzer. Liste von Objekten des
        ///     Typ User.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateUsers(List<User> users)
        {
            if (users == null || users.Count == 0)
            {
                Debug.WriteLine("UpdateUsers: No valid users passed to the update method.");
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
                        string query = @"UPDATE User 
                            SET Name=?, OldName=Name 
                            WHERE Name<>? AND Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            foreach (User user in users)
                            {
                                stmt.Bind(1, user.Name);
                                stmt.Bind(2, user.Name);
                                stmt.Bind(2, user.Id);

                                if (stmt.Step() != SQLiteResult.DONE)
                                    Debug.WriteLine("UpdateUsers: Failed to update user with id {0}.", user.Id);
                                else
                                    Debug.WriteLine("UpdateUsers: Successfully updated user with id {0}.", user.Id);

                                // Zurücksetzen für nächste Iteration.
                                stmt.Reset();
                            }                           
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("UpdateUsers: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UpdateUsers: Exception occurred. Msg is {0}.", ex.Message);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            else
            {
                Debug.WriteLine("UpdateUsers: Mutex timeout.");
                throw new DatabaseException("UpdateUsers: Timeout: Failed to get access to DB.");
            }
        }

    }
}
