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
                        string query = @"UPDATE ""Group"" 
                            SET Name=?, Description=?, Type=?, CreationDate=?, ModificationDate=?,
                            Term=?, Deleted=?, GroupAdmin_User_Id=?, NotificationSettings_NotifierId=?, IsDirty=? 
                            WHERE Id=?;";

                        using (var updateStmt = conn.Prepare(query))
                        {
                            updateStmt.Bind(1, updatedGroup.Name);
                            updateStmt.Bind(2, updatedGroup.Description);
                            updateStmt.Bind(3, (int) updatedGroup.Type);
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
                            FROM ""Group"" 
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
                                    Type = (GroupType)Enum.ToObject(typeof(GroupType), stmt["Type"]),
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

                            if (stmt.Step() == SQLiteResult.DONE)
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
        public List<User> GetParticipantsOfGroup(int groupId)
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
                                    updateStmt.Bind(1, participant.Active);
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
                                    insertStmt.Bind(3, participant.Active);

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
    }
}
