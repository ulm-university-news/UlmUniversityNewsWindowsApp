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

    }
}
