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
                        string query = @"INSERT INTO ""Group"" (Id, Name, Description, Type, CreationDate, ModificationDate,
                            Term, Deleted, GroupAdmin_User_Id, NotificationSettings_NotifierId, IsDirty) 
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

                        Debug.WriteLine("Admin of group is: {0}.", group.GroupAdmin);

                        using (var insertStmt = conn.Prepare(query))
                        {
                            insertStmt.Bind(1, group.Id);
                            insertStmt.Bind(2, group.Name);
                            insertStmt.Bind(3, group.Description);
                            insertStmt.Bind(4, (int) group.Type);
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
        /// übergebenen Objekt übernommen. Aktualisiert die Benachrichtigungsoptionen
        /// für diese Gruppe nur, wenn es explizit geforder wird.
        /// </summary>
        /// <param name="updatedGroup">Das Gruppenobjekt mit den aktualisierten Daten.</param>
        /// <param name="updateNotificationSettings">Gibt an, ob die Benachrichtigungsoptionen geändert werden sollen.
        ///     Wird das nicht gesetzt, so werden die Benachrichtigungsoptionen bei der Aktualisierung ignoriert.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateGroup(Group updatedGroup, bool updateNotificationSettings)
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
                        if (updateNotificationSettings)
                        {
                            string query = @"UPDATE ""Group"" 
                            SET Name=?, Description=?, Type=?, CreationDate=?, ModificationDate=?,
                            Term=?, Deleted=?, GroupAdmin_User_Id=?, IsDirty=?, NotificationSettings_NotifierId=? 
                            WHERE Id=?;";

                            using (var updateStmt = conn.Prepare(query))
                            {
                                updateStmt.Bind(1, updatedGroup.Name);
                                updateStmt.Bind(2, updatedGroup.Description);
                                updateStmt.Bind(3, (int)updatedGroup.Type);
                                updateStmt.Bind(4, DatabaseManager.DateTimeToSQLite(updatedGroup.CreationDate));
                                updateStmt.Bind(5, DatabaseManager.DateTimeToSQLite(updatedGroup.ModificationDate));
                                updateStmt.Bind(6, updatedGroup.Term);
                                updateStmt.Bind(7, (updatedGroup.Deleted) ? 1 : 0);
                                updateStmt.Bind(8, updatedGroup.GroupAdmin);
                                // IsDirty:
                                updateStmt.Bind(9, 1); // Änderung am Datensatz, daher Dirty = true.

                                // NotificationSettings.
                                updateStmt.Bind(10, (int)updatedGroup.GroupNotificationSetting);

                                updateStmt.Bind(11, updatedGroup.Id);

                                if (updateStmt.Step() != SQLiteResult.DONE)
                                    Debug.WriteLine("Failed to update the group with id {0}.", updatedGroup.Id);
                                else
                                    Debug.WriteLine("Successfully updated the group with id {0}.", updatedGroup.Id);
                            }
                        }
                        else
                        {
                            string query = @"UPDATE ""Group"" 
                            SET Name=?, Description=?, Type=?, CreationDate=?, ModificationDate=?,
                            Term=?, Deleted=?, GroupAdmin_User_Id=?, IsDirty=? 
                            WHERE Id=?;";

                            using (var updateStmt = conn.Prepare(query))
                            {
                                updateStmt.Bind(1, updatedGroup.Name);
                                updateStmt.Bind(2, updatedGroup.Description);
                                updateStmt.Bind(3, (int)updatedGroup.Type);
                                updateStmt.Bind(4, DatabaseManager.DateTimeToSQLite(updatedGroup.CreationDate));
                                updateStmt.Bind(5, DatabaseManager.DateTimeToSQLite(updatedGroup.ModificationDate));
                                updateStmt.Bind(6, updatedGroup.Term);
                                updateStmt.Bind(7, (updatedGroup.Deleted) ? 1 : 0);
                                updateStmt.Bind(8, updatedGroup.GroupAdmin);
                                // IsDirty:
                                updateStmt.Bind(9, 1); // Änderung am Datensatz, daher Dirty = true.

                                updateStmt.Bind(10, updatedGroup.Id);

                                if (updateStmt.Step() != SQLiteResult.DONE)
                                    Debug.WriteLine("Failed to update the group with id {0}.", updatedGroup.Id);
                                else
                                    Debug.WriteLine("Successfully updated the group with id {0}.", updatedGroup.Id);
                            }
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
        /// Liefert Datensatz der Gruppe mit der angegebenen Id aus der Datenbank zurück. Liefert auch
        /// die Informationen über die Teilnehmer der Gruppe mit.
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
                        string getGroupQuery = @"SELECT * 
                            FROM ""Group"" 
                            WHERE Id=?;";

                        string queryParticipants = @"SELECT * 
                            FROM UserGroup AS ug JOIN User AS u ON ug.User_Id=u.Id
                            WHERE ug.Group_Id=? ANd ug.Active=?;";

                        using (var getParticipantsStmt = conn.Prepare(queryParticipants))
                        using (var getGroupStmt = conn.Prepare(getGroupQuery))
                        {
                            getGroupStmt.Bind(1, id);
                            
                            if (getGroupStmt.Step() == SQLiteResult.ROW)
                            {
                                group = new Group()
                                {
                                    Id = id,
                                    Name = (string)getGroupStmt["Name"],
                                    Description = (string)getGroupStmt["Description"],
                                    Type = (GroupType)Enum.ToObject(typeof(GroupType), getGroupStmt["Type"]),
                                    CreationDate = DatabaseManager.DateTimeFromSQLite((string)getGroupStmt["CreationDate"]),
                                    ModificationDate = DatabaseManager.DateTimeFromSQLite((string)getGroupStmt["ModificationDate"]),
                                    Term = (string)getGroupStmt["Term"],
                                    Deleted = ((long)getGroupStmt["Deleted"] == 1) ? true : false,
                                    GroupAdmin = Convert.ToInt32(getGroupStmt["GroupAdmin_User_Id"]),
                                    GroupNotificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), getGroupStmt["NotificationSettings_NotifierId"]),
                                };
                            }

                            // Lade Teilnehmer der Gruppe.
                            getParticipantsStmt.Bind(1, id);
                            getParticipantsStmt.Bind(2, 1);     // Nur aktive Teilnehmer.

                            List<User> participants = new List<User>();
                            while (getParticipantsStmt.Step() == SQLiteResult.ROW)
                            {
                                User user = new User()
                                {
                                    Id = Convert.ToInt32(getParticipantsStmt["Id"]),
                                    Name = (string)getParticipantsStmt["Name"]
                                };

                                participants.Add(user);
                            }

                            // Füge Teilnehmerliste der Gruppe hinzu.
                            if (group != null)
                                group.Participants = participants;
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
        /// Liefert alle in der Datenbank gespeicherten Gruppen zurück. Liefert nur die Gruppendaten.
        /// Liefert insbesondere keine Informationen über die Teilnehmer der Gruppe.
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
                            FROM ""Group"";";

                        using (var stmt = conn.Prepare(query))
                        {
                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                Group group = new Group()
                                {
                                    Id = Convert.ToInt32(stmt["Id"]),
                                    Name = (string)stmt["Name"],
                                    Description = (string)stmt["Description"],
                                    Type = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
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
        /// Liefert die Ids aller lokal verwalteten Gruppen zurück.
        /// </summary>
        /// <returns>Eine Liste von Ids.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public List<int> GetLocalGroupIdentifiers()
        {
            List<int> identifiers = new List<int>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT Id 
                            FROM ""Group"";";

                        using (var stmt = conn.Prepare(query))
                        {
                            int id;
                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                id = Convert.ToInt32(stmt["Id"]);

                                identifiers.Add(id);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetLocalGroupIdentifiers: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetLocalGroupIdentifiers: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            return identifiers;
        }

        /// <summary>
        /// Gibt alle Datensätze von Gruppen zurück, die das IsDirty Flag gesetzt haben.
        /// Liefert nur die Gruppendaten und keine Informationen über die Teilnehmer der Gruppe.
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
                            FROM ""Group""
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
                                    Type = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
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
                        string query = @"UPDATE ""Group"" 
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

        /// <summary>
        /// Gibt an, ob zu der Id ein Datensatz einer Gruppe gespeichert ist.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <returns>Liefert true, wenn ein Datensatz hierfür gespeichert ist, ansonsten false.</returns>
        public bool IsGroupStored(int groupId)
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
                            FROM ""Group"" 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);

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
                        Debug.WriteLine("IsGroupStored: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("IsGroupStored: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
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
        /// Löschen der Gruppe mit der angegebnen Id aus der lokalen Datenbank.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die gelöscht werden soll.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Vorgang fehlschlägt.</exception>
        public void DeleteGroup(int groupId)
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
                        string query = @"DELETE FROM ""Group"" 
                            Where Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to delete group with id {0}.", groupId);
                            else
                                Debug.WriteLine("Successfully deleted the group with id {0}.", groupId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("DeleteGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
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
        /// Liefert alle Teilnehmer der Gruppe mit der angegebnen Id in Form einer Liste zurück.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <returns>Eine Liste von Instanzen der Klasse User. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn die Abfrage fehlschlägt.</exception>
        public List<User> GetActiveParticipantsOfGroup(int groupId)
        {
            List<User> participants = new List<User>();

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
                            FROM User as U JOIN UserGroup AS UG ON U.Id=UG.User_Id 
                            WHERE UG.Group_Id=? AND UG.Active=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);
                            stmt.Bind(2, 1);    // 1 = true

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                User tmp = new User()
                                {
                                    Id = Convert.ToInt32(stmt["Id"]),
                                    Name = stmt["Name"] as string,
                                };

                                participants.Add(tmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetParticipantsOfGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetParticipantsOfGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }
            
            return participants;
        }

        /// <summary>
        /// Ruft alle für die Gruppe gespeicherten Teilnehmer ab, d.h. inaktive und aktive Teilnehmer.
        /// Die Teilnehmer werden in einem Lookup-Verzeichnis zurückgeliefert.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Teilnehmer abgerufen werden sollen.</param>
        /// <returns>Ein Verzeichnis, welches Objekte vom Typ User in einem Verzeichnis verfügbar macht.
        ///     Die Objekte sind im Verzeichnis über die Ids der Nutzer abrufbar.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public Dictionary<int, User> GetAllParticipantsOfGroup(int groupId)
        {
            Dictionary<int, User> participantDictionary = new Dictionary<int, User>();

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
                            FROM User as U JOIN UserGroup AS UG ON U.Id=UG.User_Id 
                            WHERE UG.Group_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                User tmp = new User()
                                {
                                    Id = Convert.ToInt32(stmt["Id"]),
                                    Name = stmt["Name"] as string,
                                    Active = ((long)stmt["Active"] == 1) ? true : false
                                };

                                participantDictionary.Add(tmp.Id, tmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetAllParticipantsOfGroup: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetAllParticipantsOfGroup: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetAllParticipantsOfGroup: Mutex timeout.");
                throw new DatabaseException("GetAllParticipantsOfGroup: Timeout: Failed to get access to DB.");
            }

            return participantDictionary;
        }

        /// <summary>
        /// Fügt den Teilnehmer mit der angegebnen Id der Gruppe hinzu, die durch die Gruppen-Id 
        /// repräsentiert ist.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der der Teilnehmer hinzugefügt werden soll.</param>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <param name="active">Gibt an, ob der Nutzer als aktiver Teilnehmer, oder als passiver Teilnehmer hinzugefügt werden soll.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Hinzufügen fehlschlägt.</exception>
        public void AddParticipantToGroup(int groupId, int participantId, bool active)
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
                        string query = @"INSERT INTO UserGroup (Group_Id, User_Id, Active) 
                            VALUES (?,?,?);";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);
                            stmt.Bind(2, participantId);
                            stmt.Bind(3, (active) ? 1 : 0);    // 1 = true, 0 = false

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("AddParticipantToGroup: Failed to add participant with id {0} to group with id {1}.",
                                    participantId, groupId);
                            else
                                Debug.WriteLine("AddParticipantToGroup: Successfully added participant with id {0} to group with id {1}.",
                                    participantId, groupId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("AddParticipantToGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("AddParticipantToGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
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
        /// Fügt eine Menge von Nutzer-Ressourcen als Teilnehmer der Gruppe mit der angegebnen Id hinzu.
        /// Wenn ein Nutzer übergeben wird, der bereits lokal als Teilnehmer der Gruppe registriert ist,
        /// so wird der Active Zustand des Teilnehmers aktualisiert.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Teilnehmer hinzugefügt werden sollen.</param>
        /// <param name="participants">Die Liste der Teilnehmer.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException wenn Speichervorgang fehlschlägt.</exception>
        public void AddParticipantsToGroup(int groupId, List<User> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                Debug.WriteLine("AddParticipantsToGroup: No participants to add.");
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
                        int amountOfInserts = 0;
                        int amountOfUpdates = 0;

                        string checkExistenceQuery = @"SELECT COUNT(*) AS amount 
                            FROM UserGroup 
                            WHERE Group_Id=? AND User_Id=?;";
                        
                        string insertQuery = @"INSERT INTO UserGroup (Group_Id, User_Id, Active) 
                            VALUES (?,?,?);";

                        string updateQuery = @"UPDATE UserGroup 
                            SET Active=?
                            WHERE Group_Id=? AND User_Id=?;";

                        // Starte eine Transaktion.
                        using (var statement = conn.Prepare("BEGIN TRANSACTION"))
                        {
                            statement.Step();
                        }

                        using (var updateStmt = conn.Prepare(updateQuery))
                        using (var checkStmt = conn.Prepare(checkExistenceQuery))
                        using (var insertStmt = conn.Prepare(insertQuery))
                        {
                            foreach (User participant in participants)
                            {
                                checkStmt.Bind(1, groupId);
                                checkStmt.Bind(2, participant.Id);

                                int amount = -1;
                                if (checkStmt.Step() == SQLiteResult.DONE)
                                {
                                    amount = Convert.ToInt32(checkStmt["amount"]);
                                }

                                if (amount == 1)
                                {
                                    // Datensatz schon vorhanden. Führe Aktualisierung des Active Feld aus.
                                    updateStmt.Bind(1, (participant.Active) ? 1 : 0);
                                    updateStmt.Bind(2, groupId);
                                    updateStmt.Bind(3, participant.Id);

                                    updateStmt.Step();
                                    amountOfUpdates++;
                                }
                                else
                                {
                                    // Füge Datensatz hinzu.
                                    insertStmt.Bind(1, groupId);
                                    insertStmt.Bind(2, participant.Id);
                                    insertStmt.Bind(3, (participant.Active) ? 1 : 0);

                                    insertStmt.Step();
                                    amountOfInserts++;
                                }
                                
                                // Reset für nächsten Durchlauf.
                                insertStmt.Reset();
                                updateStmt.Reset();
                                checkStmt.Reset();
                            }
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            if (statement.Step() == SQLiteResult.DONE)
                            {
                                Debug.WriteLine("BulkInsertUsers: Added {0} users per bulk insert to group with id {1} + "
                                    + "and updated the active status of {2} many participants.",
                                    amountOfInserts, groupId, amountOfUpdates);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("AddParticipantsToGroup: SQLiteException occurred. Message is: {0}.", sqlEx.Message);

                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("AddParticipantsToGroup: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);

                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

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
        /// Fragt den Active Status des Teilnehmers mit der angegebenen Id in der 
        /// spezifzierten Gruppe ab.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers.</param>
        /// <returns>Den Active Status. Kann auch null zurückliefern, wenn kein
        ///     Eintrag zu dieser Gruppe existiert für den angegebenen Teilnehmer.</returns>
        public bool? RetrieveActiveStatusOfParticipant(int groupId, int participantId)
        {
            bool? active = null;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT Active 
                            FROM UserGroup 
                            WHERE Group_Id=? AND User_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);
                            stmt.Bind(2, participantId);

                            if (stmt.Step() == SQLiteResult.ROW)
                                active = ((long)(stmt["Active"]) == 1) ? true : false;
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("RetrieveActiveStatusOfParticipant: SQLiteException occurred. Message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RetrieveActiveStatusOfParticipant: Exception occurred. Message is: {0} and Stack Trace: {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }
            }

            return active;
        }

        /// <summary>
        /// Änderung des Active Zustands eines Teilnehmers der Gruppe mit der angegebnen Id.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe.</param>
        /// <param name="participantId">Die Id des Teilnehmers, dessen Zustand angepasst werden soll.</param>
        /// <param name="active">Der neue Zustandswert.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn die Änderung fehlschlägt.</exception>
        public void ChangeActiveStatusOfParticipant(int groupId, int participantId, bool active)
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
                        string query = @"UPDATE UserGroup 
                            SET Active=? 
                            WHERE Group_Id=? AND User_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, (active) ? 1 : 0);
                            stmt.Bind(2, groupId);
                            stmt.Bind(3, participantId);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to update active status of participant with id {0} in group with id {1}.",
                                    participantId, groupId);
                            else
                                Debug.WriteLine("Successfully updated active status of participant with id {0} in group with id {1}.",
                                    participantId, groupId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("ChangeActiveStatusOfParticipant: SQLiteException occurred. Message is: {0}.",
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("ChangeActiveStatusOfParticipant: Exception occurred. Message is: {0} and Stack Trace: {1}.",
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

        // ******************************************** Konversationen ********************************************************************

        /// <summary>
        /// Speichert eine Konversation in den lokalen Datensätzen ab. Die Konversation
        /// wird implizit verknüpft mit der zugehörigen Gruppe und dem Administrator. Es ist
        /// deshalb Vorraussetzung für diese Methode, dass die Gruppe und der Administrator, d.h. 
        /// der Teilnehmer der Gruppe mit den Admin-Rechten, bereits in den lokalen Datensätzen vorhanden sind.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der diese Konversation gehört.</param>
        /// <param name="conversation">Das Objekt vom Typ Conversation mit den Daten der Konversation.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void StoreConversation(int groupId, Conversation conversation)
        {
            if (conversation == null)
            {
                Debug.WriteLine("StoreConversation: No valid conversation passed to the method.");
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
                        string insertQuery = @"INSERT INTO Conversation (Id, Title, Closed, Group_Id, ConversationAdmin_User_Id) 
                            VALUES (?, ?, ?, ?, ?);";

                        using (var insertStmt = conn.Prepare(insertQuery))
                        {
                            insertStmt.Bind(1, conversation.Id);
                            insertStmt.Bind(2, conversation.Title);

                            if (conversation.IsClosed.HasValue)
                                insertStmt.Bind(3, (conversation.IsClosed == true) ? 1 : 0);
                            else
                                insertStmt.Bind(3, 0);  // Setzte defaultmäßig auf closed = false.

                            insertStmt.Bind(4, groupId);
                            insertStmt.Bind(5, conversation.AdminId);

                            if (insertStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("StoreConversation: Failed to insert conversation with id {0}.", conversation.Id);
                            else
                                Debug.WriteLine("StoreConversation: Successfully inserted conversation with id {1}.", conversation.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("StoreConversation: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StoreConversation: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("StoreConversation: Mutex timeout.");
                throw new DatabaseException("Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Speichere eine Menge von Konversationen in der lokalen Datenbank ab.
        /// Die Einträge dürfen nicht bereits lokal gespeichert sein, sonst schlägt die Operation fehl.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, der die Konversationen zugeordnet werden.</param>
        /// <param name="conversations">Die Menge an zu speichernden Konversationen.</param>
        /// <exception cref="DatabaseManager">Wirft DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void BulkInsertConversations(int groupId, List<Conversation> conversations)
        {
            if (conversations == null || conversations.Count == 0)
            {
                Debug.WriteLine("BulkInsertConversations: No valid data passed to the method.");
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
                        string insertQuery = @"INSERT INTO Conversation (Id, Title, Closed, Group_Id, ConversationAdmin_User_Id) 
                            VALUES (?, ?, ?, ?, ?);";

                        // Starte eine Transaktion.
                        using (var statement = conn.Prepare("BEGIN TRANSACTION"))
                        {
                            statement.Step();
                        }

                        using (var insertStmt = conn.Prepare(insertQuery))
                        {
                            foreach (Conversation conversation in conversations)
                            {
                                insertStmt.Bind(1, conversation.Id);
                                insertStmt.Bind(2, conversation.Title);

                                if (conversation.IsClosed.HasValue)
                                    insertStmt.Bind(3, (conversation.IsClosed == true) ? 1 : 0);
                                else
                                    insertStmt.Bind(3, 0);  // Setzte defaultmäßig auf closed = false.

                                insertStmt.Bind(4, groupId);
                                insertStmt.Bind(5, conversation.AdminId);

                                // Führe insert aus.
                                insertStmt.Step();

                                // Setze Statement für nächste Iteration zurück.
                                insertStmt.Reset();
                            }
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                            Debug.WriteLine("BulkInsertConversations: Successfully inserted {0} conversations  " +
                                "via bulk insert.", conversations.Count);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("BulkInsertConversations: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("BulkInsertConversations: Exception occurred. Msg is {0}.", ex.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }
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
                Debug.WriteLine("BulkInsertConversations: Mutex timeout.");
                throw new DatabaseException("BulkInsertConversations: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Aktualisiert die Konversation, zu welcher der übergebenen Datensatz gehört.
        /// Ersetzt die alten Daten durch die im Objekt übergebenen Daten. Bei Änderungen
        /// des Gruppenadministrators muss darauf geachtet werden, dass der Teilnehmer der Gruppe
        /// bereits in den lokalen Datensätzen vorhanden ist.
        /// </summary>
        /// <param name="updatedConversation">Die neuen Daten der Konversation in Form eines Conversation Objekts.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateConversation(Conversation updatedConversation)
        {
            if (updatedConversation == null)
            {
                Debug.WriteLine("UpdateConversation: No valid conversation object passed to the method.");
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
                        string updateQuery = @"UPDATE Conversation 
                            SET Title=?, Closed=?, ConversationAdmin_User_Id=? 
                            WHERE Id=?;";

                        using (var updateStmt = conn.Prepare(updateQuery))
                        {
                            updateStmt.Bind(1, updatedConversation.Title);

                            // Aktualisiere Closed nur, wenn der Wert gesetzt wurde.
                            if (updatedConversation.IsClosed.HasValue)
                                updateStmt.Bind(2, (updatedConversation.IsClosed == true) ? 1 : 0);

                            updateStmt.Bind(3, updatedConversation.AdminId);
                            updateStmt.Bind(4, updatedConversation.Id);

                            if (updateStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("UpdateConversation: Failed to update the conversation with id {0}.", updatedConversation.Id);
                            else
                                Debug.WriteLine("UpdateConversation: Successfully updated the conversation with id {0}.", updatedConversation.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("UpdateConversation: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UpdateConversation: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("UpdateConversation: Mutex timeout.");
                throw new DatabaseException("UpdateConversation: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Führt eine Aktualisierung der Konversationen in der Datenbank aus.
        /// </summary>
        /// <param name="updatedConversations">Die Menge der Konversationen, die neue Daten 
        ///     für die Aktualisierung beinhalten.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Aktualisierung fehlschlägt.</exception>
        public void UpdateConversations(List<Conversation> updatedConversations)
        {
            if (updatedConversations == null && updatedConversations.Count == 0)
            {
                Debug.WriteLine("UpdateConversations: No valid data passed to the method.");
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
                        string updateQuery = @"UPDATE Conversation 
                            SET Title=?, Closed=?, ConversationAdmin_User_Id=? 
                            WHERE Id=?;";

                        using (var updateStmt = conn.Prepare(updateQuery))
                        {
                            foreach (Conversation updatedConversation in updatedConversations)
                            {
                                updateStmt.Bind(1, updatedConversation.Title);

                                // Aktualisiere Closed nur, wenn der Wert gesetzt wurde.
                                if (updatedConversation.IsClosed.HasValue)
                                    updateStmt.Bind(2, (updatedConversation.IsClosed == true) ? 1 : 0);

                                updateStmt.Bind(3, updatedConversation.AdminId);
                                updateStmt.Bind(4, updatedConversation.Id);

                                if (updateStmt.Step() != SQLiteResult.DONE)
                                    Debug.WriteLine("UpdateConversations: Failed to update the conversation with id {0}.", updatedConversation.Id);
                                else
                                    Debug.WriteLine("UpdateConversations: Successfully updated the conversation with id {0}.", updatedConversation.Id);

                                // Setze Statement zurück für nächste Iteration.
                                updateStmt.Reset();
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("UpdateConversations: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("UpdateConversations: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("UpdateConversations: Mutex timeout.");
                throw new DatabaseException("UpdateConversations: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Ruft die Konversation mit der angegebenen Id aus den lokalen Datensätzen ab. Die Konversation
        /// wird mit der Information über die Anzahl ungelesener Nachrichten zurückgeliefert.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, die abgerufen werden soll.</param>
        /// <param name="includingConvMessages">Gibt an, ob die zu dieser Konversation gehörenden Nachrichten
        ///     ebenfalls abgerufen und in dem Ergebnisobjekt gespeichert werden sollen.</param>
        /// <returns>Ein Objekt der Klasse Conversation mit den abgerufenen Daten.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public Conversation GetConversation(int conversationId, bool includingConvMessages)
        {
            Conversation conversation = null;

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
                            FROM Conversation AS C JOIN User as U ON C.ConversationAdmin_User_Id=U.Id
                            WHERE C.Id=?;";

                        string determineUnreadMsgQuery = @"SELECT COUNT(*) AS amount 
                            FROM ConversationMessage AS cm JOIN Message AS m ON cm.Message_Id=m.Id 
                            WHERE cm.Conversation_Id=? AND m.Read=?;";

                        string messagesQuery = @"SELECT * 
                            FROM ConversationMessage AS cm JOIN Message AS m ON cm.Message_Id=m.Id 
                            WHERE cm.Conversation_Id=?;";

                        using (var determineUnreadMsgStmt = conn.Prepare(determineUnreadMsgQuery))
                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, conversationId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                string title = stmt["Title"] as string;
                                bool closed = false;
                                if (stmt["Closed"] != null && (long)stmt["Closed"] == 1)
                                    closed = true;
                                int adminId = Convert.ToInt32(stmt["ConversationAdmin_User_Id"]);
                                string adminName = stmt["Name"] as string;  // Frage Name des Administrators direkt mit ab.
                                int groupId = Convert.ToInt32(stmt["Group_Id"]);

                                conversation = new Conversation()
                                {
                                    Id = conversationId,
                                    Title = title,
                                    IsClosed = closed,
                                    AdminId = adminId,
                                    AdminName = adminName,
                                    GroupId = groupId
                                };

                                // Bestimme Anzahl ungelesener Nachrichten.
                                determineUnreadMsgStmt.Bind(1, conversationId);
                                determineUnreadMsgStmt.Bind(2, 0);      // read = false

                                if (determineUnreadMsgStmt.Step() == SQLiteResult.ROW)
                                {
                                    int amountOfUnreadMsg = Convert.ToInt32(determineUnreadMsgStmt["amount"]);
                                    conversation.AmountOfUnreadMessages = amountOfUnreadMsg;
                                }

                                if (includingConvMessages)
                                {
                                    List<ConversationMessage> convMsg = new List<ConversationMessage>();

                                    using (var msgStmt = conn.Prepare(messagesQuery))
                                    {
                                        msgStmt.Bind(1, conversationId);

                                        int msgNr, authorId, messageId;
                                        string txt;
                                        DateTimeOffset creationDate;
                                        Priority msgPriority;
                                        bool read;

                                        while (msgStmt.Step() == SQLiteResult.ROW)
                                        {
                                            messageId = Convert.ToInt32(msgStmt["Id"]);
                                            txt = msgStmt["Text"] as string;
                                            creationDate = DatabaseManager.DateTimeFromSQLite(msgStmt["CreationDate"] as string);
                                            msgPriority = (Priority)Enum.ToObject(typeof(Priority), msgStmt["Priority"]);
                                            read = ((long)msgStmt["Read"] == 1) ? true : false;
                                            msgNr = Convert.ToInt32(msgStmt["MessageNumber"]);
                                            authorId = Convert.ToInt32(msgStmt["Author_User_Id"]);
                                            
                                            ConversationMessage tmp = new ConversationMessage()
                                            {
                                                Id = messageId,
                                                Text = txt,
                                                CreationDate = creationDate,
                                                ConversationId = conversationId,
                                                MessageNumber = msgNr,
                                                AuthorId = authorId,
                                                MessagePriority = msgPriority,
                                                Read = read
                                            };

                                            convMsg.Add(tmp);
                                        }

                                        // Füge dem Conversation Objekt hinzu.
                                        conversation.ConversationMessages = convMsg;
                                    }
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetConversation: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetConversation: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetConversation: Mutex timeout.");
                throw new DatabaseException("GetConversation: Timeout: Failed to get access to DB.");
            }

            return conversation;
        }

        /// <summary>
        /// Fragt die Konversationen ab, die zu der Gruppe mit der angegebenen Id gespeichert sind.
        /// Die Konversationen werden inklusive der Information über die Anzahl ungelesener Nachrichten
        /// geladen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversationen abgerufen werden sollen.</param>
        /// <returns>Eine Liste mit Instanzen von Conversation. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Abruf fehlschlägt.</exception>
        public List<Conversation> GetConversations(int groupId)
        {
            List<Conversation> conversations = new List<Conversation>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string getConversationsQuery = @"SELECT * 
                            FROM Conversation AS C JOIN User AS U ON C.ConversationAdmin_User_Id=U.Id
                            WHERE Group_Id=?;";

                        string determineUnreadMsgQuery = @"SELECT COUNT(*) AS amount 
                            FROM ConversationMessage AS cm JOIN Message AS m ON cm.Message_Id=m.Id 
                            WHERE cm.Conversation_Id=? AND m.Read=?;";

                        using (var getConvStmt = conn.Prepare(getConversationsQuery))
                        using (var determineUnreadMsgStmt = conn.Prepare(determineUnreadMsgQuery))
                        {
                            // Frage Konversationen ab.
                            getConvStmt.Bind(1, groupId);

                            int convId, adminId;
                            string title;
                            bool closed;
                            string adminName;

                            while (getConvStmt.Step() == SQLiteResult.ROW)
                            {
                                convId = Convert.ToInt32(getConvStmt["Id"]);
                                title = getConvStmt["Title"] as string;
                                adminId = Convert.ToInt32(getConvStmt["ConversationAdmin_User_Id"]);
                                adminName = getConvStmt["Name"] as string;
                                groupId = Convert.ToInt32(getConvStmt["Group_Id"]);

                                closed = false;
                                if (getConvStmt["Closed"] != null && (long)getConvStmt["Closed"] == 1)
                                    closed = true;

                                Conversation tmp = new Conversation()
                                {
                                    Id = convId,
                                    Title = title,
                                    AdminId = adminId,
                                    IsClosed = closed,
                                    AdminName = adminName,
                                    GroupId = groupId
                                };

                                // Bestimme Anzahl ungelesener Nachrichten für diese Konversation.
                                determineUnreadMsgStmt.Bind(1, convId);
                                determineUnreadMsgStmt.Bind(2, 0);  // read = false

                                if (determineUnreadMsgStmt.Step() == SQLiteResult.ROW)
                                {
                                    int amoundOfUnreadMsg = Convert.ToInt32(determineUnreadMsgStmt["amount"]);

                                    // Setzte Property in Conversation.
                                    tmp.AmountOfUnreadMessages = amoundOfUnreadMsg;
                                }

                                // Setze stmt für nächsten Durchgang zurück.
                                determineUnreadMsgStmt.Reset();

                                // Füge Konversation der Liste hinzu.
                                conversations.Add(tmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetConversations: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetConversations: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetConversations: Mutex timeout.");
                throw new DatabaseException("GetConversations: Timeout: Failed to get access to DB.");
            }

            return conversations;
        }

        /// <summary>
        /// Ruft alle lokal gespeicherten Konversationen ab. Die Konversationen werden
        /// ohne die Informationen bezüglich ungelesener Nachrichten geladen.
        /// </summary>
        /// <returns>Eine Liste von Conversation Objekten. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public List<Conversation> GetAllConversations()
        {
            List<Conversation> conversations = new List<Conversation>();

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
                            FROM Conversation AS C JOIN User AS U ON C.ConversationAdmin_User_Id=U.Id;";

                        using (var stmt = conn.Prepare(query))
                        {
                            int conversationId = Convert.ToInt32(stmt["Id"]);
                            string title = stmt["Title"] as string;
                            bool closed = false;
                            if (stmt["Closed"] != null && (long)stmt["Closed"] == 1)
                                closed = true;
                            int adminId = Convert.ToInt32(stmt["ConversationAdmin_User_Id"]);
                            string adminName = stmt["Name"] as string;
                            int groupId = Convert.ToInt32(stmt["Group_Id"]);

                            Conversation conversation = new Conversation()
                            {
                                Id = conversationId,
                                Title = title,
                                IsClosed = closed,
                                AdminId = adminId,
                                AdminName = adminName,
                                GroupId = groupId                                
                            };

                            conversations.Add(conversation);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetAllConversations: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetAllConversations: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetAllConversations: Mutex timeout.");
                throw new DatabaseException("GetAllConversations: Timeout: Failed to get access to DB.");
            }

            return conversations;
        }

        /// <summary>
        /// Löscht die Konversation mit der angegebenen Id aus den lokalen Datensätzen.
        /// </summary>
        /// <param name="conversationId">Die Id der zu löschenden Konversation.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Löschen fehlschlägt.</exception>
        public void DeleteConversation(int conversationId)
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
                        string query = @"DELETE 
                            FROM Conversation 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, conversationId);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("DeleteConversation: Failed to delete conversation with id {0}.", conversationId);
                            else
                                Debug.WriteLine("DeleteConversation: Successfully deleted conversation with id {0}.", conversationId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("DeleteConversation: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteConversation: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("DeleteConversation: Mutex timeout.");
                throw new DatabaseException("DeleteConversation: Timeout: Failed to get access to DB.");
            }
        }

        // ************************************ Nachrichten in Konversationen *****************************************************************************

        /// <summary>
        /// Speichert die übergebene Konversationsnachricht in den lokalen Datensätzen ab.
        /// </summary>
        /// <param name="conversationMsg">Das ConversationMessage Objekt mit den Daten der Nachricht.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void StoreConversationMessage(ConversationMessage conversationMsg)
        {
            if (conversationMsg == null)
            {
                Debug.WriteLine("StoreConversationMessage: No valid object passed to method.");
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

                        string insertToMessageTableQuery = @"INSERT INTO Message (Id, Text, CreationDate, Priority, Read) 
                            VALUES (?, ?, ?, ?, ?);";

                        string insertToConvMsgTableQuery = @"INSERT INTO ConversationMessage (MessageNumber, Conversation_Id, 
                            Author_User_Id, Message_Id) 
                            VALUES (?, ?, ?, ?);";

                        using (var insertToMsgStmt = conn.Prepare(insertToMessageTableQuery))
                        using (var insertToConvMsgStmt = conn.Prepare(insertToConvMsgTableQuery))
                        {
                            // Füge zunächst in Message Tabelle ein.
                            insertToMsgStmt.Bind(1, conversationMsg.Id);
                            insertToMsgStmt.Bind(2, conversationMsg.Text);
                            insertToMsgStmt.Bind(3, DatabaseManager.DateTimeToSQLite(conversationMsg.CreationDate));
                            insertToMsgStmt.Bind(4, (int)conversationMsg.MessagePriority);
                            insertToMsgStmt.Bind(5, 0); // Read auf false zu Beginn.

                            if (insertToMsgStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("StoreConversationMessage: Failed to store message part of conversation message with id {0}.", conversationMsg.Id);
                            else
                                Debug.WriteLine("StoreConversationMessage: Successfully stored message part of conversation msg with id {0}.", conversationMsg.Id);

                            // Füge in Conversation Message Tabelle ein.
                            insertToConvMsgStmt.Bind(1, conversationMsg.MessageNumber);
                            insertToConvMsgStmt.Bind(2, conversationMsg.ConversationId);
                            insertToConvMsgStmt.Bind(3, conversationMsg.AuthorId);
                            insertToConvMsgStmt.Bind(4, conversationMsg.Id);

                            if (insertToConvMsgStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("StoreConversationMessage: Failed to store conv message part of conversation msg with id {0}.", conversationMsg.Id);
                            else
                                Debug.WriteLine("StoreConversationMessage: Successfully stored conv message part of conversation msg with id {0}.", conversationMsg.Id);
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("StoreConversationMessage: SQLiteException has occurred. Exception message is: {0}.",
                            sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StoreConversationMessage: Exception occurred. Msg is {0}.", ex.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

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
                Debug.WriteLine("StoreConversationMessage: Mutex timeout.");
                throw new DatabaseException("StoreConversationMessage: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Speichere eine Menge von Konversationsnachrichten via Bulk Insert in die lokale Datenbank ein.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten gehören.</param>
        /// <param name="conversationMessages">Die Liste von Objekten der Klasse ConversationMessage.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Speicherung fehlschlägt.</exception>
        public void BulkInsertConversationMessages(int conversationId, List<ConversationMessage> conversationMessages)
        {
            if (conversationMessages == null || conversationMessages.Count == 0)
            {
                Debug.WriteLine("BulkInsertConversationMessages: No data to store.");
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
                        string getHighestMsgNr = @"SELECT MAX(MessageNumber) AS maxMsgNr 
                            FROM ConversationMessage 
                            WHERE Conversation_Id=?;";

                        string insertToMessageTableQuery = @"INSERT INTO Message (Id, Text, CreationDate, Priority, Read) 
                            VALUES (?, ?, ?, ?, ?);";

                        string insertToConvMsgTableQuery = @"INSERT INTO ConversationMessage (MessageNumber, Conversation_Id, 
                            Author_User_Id, Message_Id) 
                            VALUES (?, ?, ?, ?);";

                        // Frage zunächst die höchste Nachrichtennummer ab.
                        using (var stmt = conn.Prepare(getHighestMsgNr))
                        {
                            stmt.Bind(1, conversationId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int highestNr = Convert.ToInt32(stmt["maxMsgNr"]);

                                // Reduziere die Liste an zu speichernden Nachrichten auf die, die noch nicht lokal gespeichert sind.
                                // Also die, die eine höhere Nachrichtennummer als die bisher höchste haben.
                                Debug.WriteLine("BulkInsertConversationMessages: Reducing the messages to the ones with msgNr higher than {0}. The "+ 
                                    "current amount of messages is {1}.", highestNr, conversationMessages.Count);

                                conversationMessages = conversationMessages.Where(item => item.MessageNumber > highestNr).ToList<ConversationMessage>();

                                Debug.WriteLine("BulkInsertConversationMessages: Reduced to a size of {0}.", conversationMessages.Count);
                            }
                        }

                        // Starte eine Transaktion.
                        using (var statement = conn.Prepare("BEGIN TRANSACTION"))
                        {
                            statement.Step();
                        }

                        using (var insertToMsgStmt = conn.Prepare(insertToMessageTableQuery))
                        using (var insertToConvMsgStmt = conn.Prepare(insertToConvMsgTableQuery))
                        {
                            foreach (ConversationMessage conversationMsg in conversationMessages)
                            {
                                // Füge zunächst in Message Tabelle ein.
                                insertToMsgStmt.Bind(1, conversationMsg.Id);
                                insertToMsgStmt.Bind(2, conversationMsg.Text);
                                insertToMsgStmt.Bind(3, DatabaseManager.DateTimeToSQLite(conversationMsg.CreationDate));
                                insertToMsgStmt.Bind(4, (int)conversationMsg.MessagePriority);
                                insertToMsgStmt.Bind(5, 0); // Read auf false zu Beginn.

                                insertToMsgStmt.Step();

                                // Füge in Conversation Message Tabelle ein.
                                insertToConvMsgStmt.Bind(1, conversationMsg.MessageNumber);
                                insertToConvMsgStmt.Bind(2, conversationMsg.ConversationId);
                                insertToConvMsgStmt.Bind(3, conversationMsg.AuthorId);
                                insertToConvMsgStmt.Bind(4, conversationMsg.Id);

                                insertToConvMsgStmt.Step();

                                // Setze statements zurück für nächste Iteration.
                                insertToMsgStmt.Reset();
                                insertToConvMsgStmt.Reset();
                            }
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                            Debug.WriteLine("BulkInsertConversationMessages: Successfully inserted {0} conversation messages " + 
                                "via bulk insert into conversation with id {1}.", conversationMessages.Count, conversationId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("BulkInsertConversationMessages: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("BulkInsertConversationMessages: Exception occurred. Msg is {0}.", ex.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

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
                Debug.WriteLine("BulkInsertConversationMessages: Mutex timeout.");
                throw new DatabaseException("BulkInsertConversationMessages: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Fragt alle Konversationsnachrichten ab, die der Konversation mit der angegebenen Id zugeordnet sind.
        /// Die Abfrage kann durch den Nachrichtennummer Parameter eingeschränkt werden, so dass nur Nachrichten mit
        /// einer höhreren Nachrichtennummer als der angegebenen abgerufen werden.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, zu der die Nachrichten abgefragt werden sollen.</param>
        /// <param name="messageNr">Die Nachrichtennummer, aber der die Nachrichten abgerufen werden sollen.</param>
        /// <returns>Eine Liste von Objekten des Typs ConversationMessage.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public List<ConversationMessage> GetConversationMessages(int conversationId, int messageNr)
        {
            List<ConversationMessage> conversationMessages = new List<ConversationMessage>();

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
                            FROM ConversationMessage AS cm JOIN Message AS m ON cm.Message_Id=m.Id 
                                JOIN User AS u ON cm.Author_User_Id=u.Id
                            WHERE cm.Conversation_Id=? AND cm.MessageNumber > ?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            int msgId, authorId, msgNumber;
                            string text;
                            DateTimeOffset creationDate;
                            Priority msgPriority;
                            bool read;
                            string authorName;

                            stmt.Bind(1, conversationId);
                            stmt.Bind(2, messageNr);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                msgId = Convert.ToInt32(stmt["Id"]);
                                text = stmt["Text"] as string;
                                creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"] as string);
                                msgPriority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);
                                authorName = (string)stmt["Name"];

                                read = false;
                                if (stmt["Read"] != null && (long)stmt["Read"] == 1)
                                    read = true;

                                msgNumber = Convert.ToInt32(stmt["MessageNumber"]);
                                authorId = Convert.ToInt32(stmt["Author_User_Id"]);

                                ConversationMessage tmp = new ConversationMessage()
                                {
                                    Id = msgId,
                                    Text = text,
                                    CreationDate = creationDate,
                                    MessagePriority = msgPriority,
                                    AuthorId = authorId,
                                    ConversationId = conversationId,
                                    MessageNumber = msgNumber,
                                    Read = read,
                                    AuthorName = authorName
                                };

                                // Füge ConversationMessage der Liste hinzu.
                                conversationMessages.Add(tmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetConversationMessages: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetConversationMessages: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetConversationMessages: Mutex timeout.");
                throw new DatabaseException("GetConversationMessages: Timeout: Failed to get access to DB.");
            }

            return conversationMessages;
        }

        /// <summary>
        /// Holt die angegebene Anzahl an aktuellesten Konversationsnachrichten aus der Datenbank. Die aktuellesten
        /// Konversationsnachrichten sind dabei diejenigen, die zeitlich gesehen zuletzt gesendet wurden. Über den Offset 
        /// kann angegeben werden, dass die angegebene Anzahl an Konversationsnachrichten übersprungen werden soll. Das ist für das 
        /// inkrementelle Laden von älteren Konversationsnachrichten wichtig.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, aus der die Nachrichten abgerufen werden sollen.</param>
        /// <param name="number">Die Anzahl an Nachrichten, die abgerufen werden soll.</param>
        /// <param name="offset">Der Offset, der angibt wie viele der neusten Konversationsnachrichten übersprungen werden sollen.</param>
        /// <returns>Eine Liste von ConversationMessage Objekten.</param>
        /// <exception cref="DatbaseException">Wirft DatabaseException, wenn der Abfruf der Konversationsnachrichten fehlschlägt.</exception>
        public List<ConversationMessage> GetLastestConversationMessages(int conversationId, int number, int offset)
        {
            List<ConversationMessage> conversationMessages = new List<ConversationMessage>();

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
                            FROM Message AS m JOIN ConversationMessage AS cm ON m.Id=cm.Message_Id 
                                JOIN User AS u ON cm.Author_User_Id=u.Id
                            WHERE Conversation_Id=? 
                            ORDER BY cm.MessageNumber DESC 
                            LIMIT ? OFFSET ?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            int msgId, authorId, msgNumber;
                            string text;
                            DateTimeOffset creationDate;
                            Priority msgPriority;
                            bool read;
                            string authorName;

                            stmt.Bind(1, conversationId);
                            stmt.Bind(2, number);
                            stmt.Bind(3, offset);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                msgId = Convert.ToInt32(stmt["Id"]);
                                text = stmt["Text"] as string;
                                creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"] as string);
                                msgPriority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);
                                authorName = (string)stmt["Name"];

                                read = false;
                                if (stmt["Read"] != null && (long)stmt["Read"] == 1)
                                    read = true;

                                msgNumber = Convert.ToInt32(stmt["MessageNumber"]);
                                authorId = Convert.ToInt32(stmt["Author_User_Id"]);

                                ConversationMessage tmp = new ConversationMessage()
                                {
                                    Id = msgId,
                                    Text = text,
                                    CreationDate = creationDate,
                                    MessagePriority = msgPriority,
                                    AuthorId = authorId,
                                    ConversationId = conversationId,
                                    MessageNumber = msgNumber,
                                    Read = read,
                                    AuthorName = authorName
                                };

                                // Füge ConversationMessage der Liste hinzu.
                                conversationMessages.Add(tmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetLastestConversationMessages: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetLastestConversationMessages: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetLastestConversationMessages: Mutex timeout.");
                throw new DatabaseException("GetLastestConversationMessages: Timeout: Failed to get access to DB.");
            }

            return conversationMessages;
        }

        /// <summary>
        /// Ruft zu den Konversationen, die der Gruppe mit der angegebenen Id zugeordnet sind, die Anzahl
        /// ungelesener Nachrichten ab und speichert sie in einem Verzeichnis. Das Verzeichnis bildet ab
        /// von der Id der Konversation auf die Anzahl der ungelesenen Nachrichten für exakt diese Konversation. 
        /// Das Verzeichnis enthält nur Einträge bei denen die Anzahl ungelesener Nachrichten größer 0 ist. 
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, zu der die Konversationen gehören sollen.</param>
        /// <returns>Ein Verzeichnis, indem für jede Konversation der angegebenen Gruppe mit mehr als einer
        /// ungelesenen Nachricht die Anzahl der ungelesenen Nachrichten gespeichert werden. Die Anzahl kann über die
        /// Konversationen-Id als Schlüssel extrahiert werden.</returns>
        public Dictionary<int, int> DetermineAmountOfUnreadConvMsgForGroup(int groupId)
        {
            Dictionary<int, int> amountOfUnreadMsgMap = new Dictionary<int, int>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT cm.Conversation_Id, COUNT(*) AS amount 
                            FROM Conversation AS c JOIN ConversationMessage AS cm ON cm.Conversation_Id=c.Id
                                JOIN Message AS m ON cm.Message_Id=m.Id
                            WHERE c.Group_Id=? AND m.Read=? 
                            GROUP BY cm.Conversation_Id;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, groupId);
                            stmt.Bind(2, 0);    // read = false;

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                int conversationId = Convert.ToInt32(stmt["Conversation_Id"]);
                                int amountOfUnreadMsg = Convert.ToInt32(stmt["amount"]);

                                // Speichere das Tupel im Verzeichnis.
                                amountOfUnreadMsgMap.Add(conversationId, amountOfUnreadMsg);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("DetermineAmountOfUnreadConvMsgForGroup: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DetermineAmountOfUnreadConvMsgForGroup: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("DetermineAmountOfUnreadConvMsgForGroup: Mutex timeout.");
                throw new DatabaseException("DetermineAmountOfUnreadConvMsgForGroup: Timeout: Failed to get access to DB.");
            }

            return amountOfUnreadMsgMap;
        }

        /// <summary>
        /// Bestimmt die aktuell höchste Nachrichtennummer, die für die angegebene Konversation in den 
        /// lokalen Datensätzen gespeichert ist.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, für die die höchste Nachrichtennummer 
        ///     gespeichert werden soll.</param>
        /// <returns>Die aktuell höchste Nachrichtennummer.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abfruf fehlschlägt.</exception>
        public int GetHighestConversationMessageNumber(int conversationId)
        {
            int highestMsgNr = 0;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(DatabaseManager.MutexTimeoutValue))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT MAX (MessageNumber) AS maxNr
                            FROM ConversationMessage 
                            WHERE Conversation_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, conversationId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                highestMsgNr = Convert.ToInt32(stmt["maxNr"]);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetHighestConversationMessageNumber: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetHighestConversationMessageNumber: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("GetHighestConversationMessageNumber: Mutex timeout.");
                throw new DatabaseException("GetHighestConversationMessageNumber: Timeout: Failed to get access to DB.");
            }

            return highestMsgNr;
        }

        /// <summary>
        /// Markiert Konversationsnachrichten der Konversation, die durch die angegebene Id 
        /// identifieziert wird, als gelesen.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, für die die Nachrichten
        ///     als gelsen markiert werden sollen.</param>
        public void MarkConversationMessagesAsRead(int conversationId)
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
                        string query = @"UPDATE Message 
                                        SET Read=? 
                                        WHERE Id IN (
                                            SELECT Message_Id As Id
                                            FROM ConversationMessage
                                            WHERE Conversation_Id=?
                                        );";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, 1);    // 1 = true
                            stmt.Bind(2, conversationId);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("MarkConversationMessagesAsRead: Couldn't reset 'read' flag on conversation messages.");
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("MarkConversationMessagesAsRead: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("MarkConversationMessagesAsRead: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("MarkConversationMessagesAsRead: Mutex timeout.");
                throw new DatabaseException("MarkConversationMessagesAsRead: Timeout: Failed to get access to DB.");
            }
        }

        /// <summary>
        /// Löscht alle Konversationsnachrichten der Konversation, die durch die
        /// angegebene Id identifiziert ist.
        /// </summary>
        /// <param name="conversationId">Die Id der Konversation, zu der alle Nachrichten gelöscht werden sollen.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Löschen fehlschlägt.</exception>
        public void DeleteConversationMessages(int conversationId)
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
                        // Einträge in ConversationMessage Tabelle werden durch
                        // kaskadierendes Löschen automatisch entfernt.
                        string query = @"DELETE FROM Message 
                            WHERE Id IN (
                                SELECT Message_Id AS Id
                                FROM ConversationMessage 
                                WHERE Conversation_Id=?
                            );";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, conversationId);

                            if (stmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("DeleteConversationMessages: Failed to delete conversation messages " +
                                    "for conversation with id {0}.", conversationId);
                            else
                                Debug.WriteLine("DeleteConversationMessages: Successfully deleted conversation messages " + 
                                    "for conversation with id {0}.", conversationId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("DeleteConversationMessages: SQLiteException occurred. Msg is {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteConversationMessages: Exception occurred. Msg is {0}.", ex.Message);
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
                Debug.WriteLine("DeleteConversationMessages: Mutex timeout.");
                throw new DatabaseException("DeleteConversationMessages: Timeout: Failed to get access to DB.");
            }
        }
    }
}
