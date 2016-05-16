using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
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
    public class GroupDatabaseManager
    {
        /// <summary>
        /// Speichert den Datensatz der übergebenen Gruppe in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="group">Die zu speichernde Gruppe.</param>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void StoreGroup(Group group)
        {
            if (group == null)
            {
                Debug.WriteLine("No valid group object passed to the StoreGroup method.");
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
                        string query = @"INSERT INTO Group (Id, Name, Description, Type, CreationDate, ModificationDate,
                            Term, Deleted, GroupAdmin_User_Id, NotificationSettings_NotifierId, IsDirty) 
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

                        using (var insertStmt = conn.Prepare(query))
                        {
                            insertStmt.Bind(1, group.Id);
                            insertStmt.Bind(2, group.Name);
                            insertStmt.Bind(3, group.Description);
                            insertStmt.Bind(4, (int) group.GroupType);
                            insertStmt.Bind(5, DatabaseManager.DateTimeToSQLite(group.CreationDate));
                            insertStmt.Bind(6, DatabaseManager.DateTimeToSQLite(group.ModificationDate));
                            insertStmt.Bind(7, group.Term);
                            insertStmt.Bind(8, (group.Deleted) ? 1 : 0);
                            insertStmt.Bind(9, group.GroupAdmin);

                            // Setze Default Setting bei einer neuen Gruppe.
                            group.GroupNotificationSetting = NotificationSetting.APPLICATION_DEFAULT;
                            insertStmt.Bind(10, (int) group.GroupNotificationSetting);

                            // IsDirty auf true, da neuer Datensatz.
                            insertStmt.Bind(11, 1); // 1 = true

                            if (insertStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to store group with id {0}.", group.Id);
                            else
                                Debug.WriteLine("Successfully stored group with id {0}.", group.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("StoreGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StoreGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
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
        /// Aktualisiert den Datensatz der Gruppe. Es werden die Daten aus dem
        /// übergebenen Objekt übernommen.
        /// </summary>
        /// <param name="updatedGroup">Das Gruppenobjekt mit den aktualisierten Daten.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateGroup(Group updatedGroup)
        {
            if (updatedGroup == null)
            {
                Debug.WriteLine("No valid group object passed to the UpdateGroup method.");
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
                        string query = @"UPDATE Group 
                            SET Name=?, Description=?, Type=?, CreationDate=?, ModificationDate=?,
                            Term=?, Deleted=?, GroupAdmin_User_Id=?, NotificationSettings_NotifierId=?, IsDirty=? 
                            WHERE Id=?;";

                        using (var updateStmt = conn.Prepare(query))
                        {
                            updateStmt.Bind(1, updatedGroup.Name);
                            updateStmt.Bind(2, updatedGroup.Description);
                            updateStmt.Bind(3, (int) updatedGroup.GroupType);
                            updateStmt.Bind(4, DatabaseManager.DateTimeToSQLite(updatedGroup.CreationDate));
                            updateStmt.Bind(5, DatabaseManager.DateTimeToSQLite(updatedGroup.ModificationDate));
                            updateStmt.Bind(6, updatedGroup.Term);
                            updateStmt.Bind(7, (updatedGroup.Deleted) ? 1 : 0);
                            updateStmt.Bind(8, updatedGroup.GroupAdmin);
                            updateStmt.Bind(9, (int) updatedGroup.GroupNotificationSetting);
                            updateStmt.Bind(10, 1); // Änderung am Datensatz, daher true.

                            updateStmt.Bind(11, updatedGroup.Id);

                            if (updateStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to update the group with id {0}.", updatedGroup.Id);
                            else
                                Debug.WriteLine("Successfully updated the group with id {0}.", updatedGroup.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("UpdateGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UpdateGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
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
        /// Liefert Datensatz der Gruppe mit der angegebenen Id aus der Datenbank zurück.
        /// </summary>
        /// <param name="id">Die Id der Gruppe.</param>
        /// <returns>Eine Instanz der Klasse Group, oder null, wenn kein entsprechender Datensatz existiert.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Abruf fehlschlägt.</exception>
        public Group GetGroup(int id)
        {
            Group group = null;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT * 
                            FROM Group 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, id);

                            if (stmt.Step() == SQLiteResult.DONE)
                            {
                                group = new Group()
                                {
                                    Id = id,
                                    Name = (string)stmt["Name"],
                                    Description = (string)stmt["Description"],
                                    GroupType = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
                                    CreationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["CreationDate"]),
                                    ModificationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["ModificationDate"]),
                                    Term = (string)stmt["Term"],
                                    Deleted = ((long)stmt["Deleted"] == 1) ? true : false,
                                    GroupAdmin = Convert.ToInt32(stmt["GroupAdmin_User_Id"]),
                                    GroupNotificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), stmt["NotificationSettings_NotifierId"]),
                                };
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            return group;
        }

        /// <summary>
        /// Liefert alle in der Datenbank gespeicherten Gruppen zurück.
        /// </summary>
        /// <returns>Eine Liste von Group Instanzen. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn der Abruf fehlschlägt.</exception>
        public List<Group> GetAllGroups()
        {
            List<Group> groupList = new List<Group>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT * 
                            FROM Group;";

                        using (var stmt = conn.Prepare(query))
                        {
                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                Group group = new Group()
                                {
                                    Id = Convert.ToInt32(stmt["Id"]),
                                    Name = (string)stmt["Name"],
                                    Description = (string)stmt["Description"],
                                    GroupType = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
                                    CreationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["CreationDate"]),
                                    ModificationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["ModificationDate"]),
                                    Term = (string)stmt["Term"],
                                    Deleted = ((long)stmt["Deleted"] == 1) ? true : false,
                                    GroupAdmin = Convert.ToInt32(stmt["GroupAdmin_User_Id"]),
                                    GroupNotificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), stmt["NotificationSettings_NotifierId"]),
                                };

                                groupList.Add(group);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetAllGroups: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetAllGroups: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            return groupList;
        }

        /// <summary>
        /// Gibt alle Datensätze von Gruppen zurück, die das IsDirty Flag gesetzt haben.
        /// </summary>
        /// <returns>Eine Liste von Instanzen der Klasse Group. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn der Abruf fehlschlägt.</exception>
        public List<Group> GetDirtyGroups()
        {
            List<Group> groupList = new List<Group>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT * 
                            FROM Group
                            WHERE IsDirty=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, 1);    // 1 = true

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                Group group = new Group()
                                {
                                    Id = Convert.ToInt32(stmt["Id"]),
                                    Name = (string)stmt["Name"],
                                    Description = (string)stmt["Description"],
                                    GroupType = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
                                    CreationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["CreationDate"]),
                                    ModificationDate = DatabaseManager.DateTimeFromSQLite((string)stmt["ModificationDate"]),
                                    Term = (string)stmt["Term"],
                                    Deleted = ((long)stmt["Deleted"] == 1) ? true : false,
                                    GroupAdmin = Convert.ToInt32(stmt["GroupAdmin_User_Id"]),
                                    GroupNotificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), stmt["NotificationSettings_NotifierId"]),
                                };

                                groupList.Add(group);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetDirtyGroups: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetDirtyGroups: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            return groupList;
        }

        /// <summary>
        /// Setze das IsDirty Flag von allen Gruppendatensätzen zurück.
        /// </summary>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Zurücksetzen fehlgeschlagen ist.</exception>
        public void ResetDirtyFlagOnGroups()
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
                        string query = @"UPDATE Group 
                                        SET IsDirty=? 
                                        WHERE IsDirty=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, 0);    // 0 = false
                            stmt.Bind(2, 1);    // 1 = true

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Error while resetting the isDirty flag in group table.");
                            else
                                Debug.WriteLine("Successfully resetted the isDirty flags in the group table.");
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("ResetDirtyFlagOnGroups: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ResetDirtyFlagOnGroups: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
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
