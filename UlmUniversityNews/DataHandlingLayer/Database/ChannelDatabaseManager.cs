using DataHandlingLayer.DataModel;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLitePCL;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.DataModel.Enums;
using System.Threading;

namespace DataHandlingLayer.Database
{
    public class ChannelDatabaseManager
    {
        /// <summary>
        /// Speichere einen Kanal in der lokalen Datenbank. 
        /// </summary>
        /// <param name="channel">Das Kanal Objekt mit den Daten des Kanals.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn die Kanaldaten nicht in der DB gespeichert werden konnten.</exception>
        public void StoreChannel(Channel channel)
        {
            if(channel == null)
            {
                Debug.WriteLine("No valid channel object passed to the StoreChannel method.");
                return;
            }

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
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

                        // Speichere Kanaldaten.
                        using (var insertStmt = conn.Prepare("INSERT INTO Channel (Id, Name, Description, " +
                            "CreationDate, ModificationDate, Type, Term, Location, Dates, Contact, Website, Deleted, NotificationSettings_NotifierId) " +
                            "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);"))
                        {
                            insertStmt.Bind(1, channel.Id);
                            insertStmt.Bind(2, channel.Name);
                            insertStmt.Bind(3, channel.Description);
                            insertStmt.Bind(4, DatabaseManager.DateTimeToSQLite(channel.CreationDate));
                            insertStmt.Bind(5, DatabaseManager.DateTimeToSQLite(channel.ModificationDate));
                            insertStmt.Bind(6, (int)channel.Type);
                            insertStmt.Bind(7, channel.Term);
                            insertStmt.Bind(8, channel.Locations);
                            insertStmt.Bind(9, channel.Dates);
                            insertStmt.Bind(10, channel.Contacts);
                            insertStmt.Bind(11, channel.Website);
                            insertStmt.Bind(12, (channel.Deleted) ? 1 : 0);

                            // Channel hat zu Begin immer Default Benachrichtigungseinstellungen.
                            channel.AnnouncementNotificationSetting = NotificationSetting.APPLICATION_DEFAULT;
                            insertStmt.Bind(13, (int)channel.AnnouncementNotificationSetting);

                            insertStmt.Step();
                        }

                        // Speichere Subklassen Parameter abhängig vom Typ des Kanals.
                        switch (channel.Type)
                        {
                            case DataModel.Enums.ChannelType.LECTURE:
                                Lecture lecture = (Lecture)channel;

                                // Speichere Vorlesungsdaten.
                                using (var insertLectureStmt = conn.Prepare("INSERT INTO Lecture (Channel_Id, " +
                                    "Faculty, StartDate, EndDate, Lecturer, Assistant) " +
                                    "VALUES (?, ?, ?, ?, ?, ?);"))
                                {
                                    insertLectureStmt.Bind(1, channel.Id);
                                    insertLectureStmt.Bind(2, (int)lecture.Faculty);
                                    insertLectureStmt.Bind(3, lecture.StartDate);
                                    insertLectureStmt.Bind(4, lecture.EndDate);
                                    insertLectureStmt.Bind(5, lecture.Lecturer);
                                    insertLectureStmt.Bind(6, lecture.Assistant);

                                    insertLectureStmt.Step();
                                }

                                Debug.WriteLine("Stored lecture channel.");
                                break;
                            case DataModel.Enums.ChannelType.EVENT:
                                Event eventChannel = (Event)channel;

                                // Speichere Eventdaten.
                                using (var insertEventStmt = conn.Prepare("INSERT INTO Event (Channel_Id, Cost, Organizer) " +
                                    "VALUES (?, ?, ?);"))
                                {
                                    insertEventStmt.Bind(1, channel.Id);
                                    insertEventStmt.Bind(2, eventChannel.Cost);
                                    insertEventStmt.Bind(3, eventChannel.Organizer);

                                    insertEventStmt.Step();
                                }

                                Debug.WriteLine("Stored event channel.");
                                break;
                            case DataModel.Enums.ChannelType.SPORTS:
                                Sports sportsChannel = (Sports)channel;

                                // Speichere Sportgruppendaten.
                                using (var insertSportsStmt = conn.Prepare("INSERT INTO Sports (Channel_Id, Cost, NumberOfParticipants) " +
                                    "VALUES (?, ?, ?);"))
                                {
                                    insertSportsStmt.Bind(1, channel.Id);
                                    insertSportsStmt.Bind(2, sportsChannel.Cost);
                                    insertSportsStmt.Bind(3, sportsChannel.NumberOfParticipants);

                                    insertSportsStmt.Step();
                                }

                                Debug.WriteLine("Stored sports channel.");
                                break;
                            default:
                                Debug.WriteLine("There is no subclass for channel type OTHER and STUDENT_GROUP, so storing is already complete.");
                                break;
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in StoreChannel. Exception message is: {0}.", sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing channel data in database has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in StoreChannel. " +
                            "Exception message is: {0}, and stack trace is {1}.", ex.Message, ex.StackTrace);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing channel data in database has failed.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }    // Ende using block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }               
        }

        /// <summary>
        /// Aktualisiere den Kanal. Bei dieser Methode werden nur die Kanalattribute in der Datenbank
        /// aktualisiert. Mögliche Attribute von Subklassen der Kanal Klasse werden ignoriert.
        /// </summary>
        /// <param name="channel">Eine Objektinstanz von Kanal mit den aktualisierten Daten für den Kanal.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Aktualisierung des Kanals fehlschlägt.</exception>
        public void UpdateChannel(Channel channel)
        {
            if (channel == null)
            {
                Debug.WriteLine("No valid channel object passed to the UpdateChannel method.");
                return;
            }

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Aktualisiere Daten in Kanal-Tabelle.
                        using (var updateChannelStmt = conn.Prepare("UPDATE Channel SET Name=?, Description=?, " +
                            "CreationDate=?, ModificationDate=?, Type=?, Term=?, Location=?, Dates=?, Contact=?, " + 
                            "Website=?, Deleted=?, NotificationSettings_NotifierId=? " +
                            "WHERE Id=?;"))
                        {
                            updateChannelStmt.Bind(1, channel.Name);
                            updateChannelStmt.Bind(2, channel.Description);
                            updateChannelStmt.Bind(3, DatabaseManager.DateTimeToSQLite(channel.CreationDate));
                            updateChannelStmt.Bind(4, DatabaseManager.DateTimeToSQLite(channel.ModificationDate));
                            updateChannelStmt.Bind(5, (int)channel.Type);
                            updateChannelStmt.Bind(6, channel.Term);
                            updateChannelStmt.Bind(7, channel.Locations);
                            updateChannelStmt.Bind(8, channel.Dates);
                            updateChannelStmt.Bind(9, channel.Contacts);
                            updateChannelStmt.Bind(10, channel.Website);
                            updateChannelStmt.Bind(11, (channel.Deleted) ? 1 : 0);
                            updateChannelStmt.Bind(12, (int)channel.AnnouncementNotificationSetting);

                            updateChannelStmt.Bind(13, channel.Id);

                            updateChannelStmt.Step();

                            Debug.WriteLine("Update channel with id {0}.", channel.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in UpdateChannel. The message is: {0}." + sqlEx.Message);
                        throw new DatabaseException("Update of the channel with id " + channel.Id + " has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in UpdateChannel. The message is: {0}, " +
                            "and the stack trace is: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Update of channel has failed.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende using Block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }              
        }

        /// <summary>
        /// Aktualisiert den Kanal unter Berücksichtigung der entsprechenden Subklassen-Attribute.
        /// Es wird abhängig vom Typ des Kanals die Kanal-Tabelle und die Tabellen für die Subklassen aktualisiert.
        /// </summary>
        /// <param name="channel">Das Objekt vom Typ Kanal oder vom Typ eines der Unterklassen.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Aktualisierung des Kanals fehlschlägt.</exception>
        public void UpdateChannelWithSubclass(Channel channel)
        {
            if (channel == null)
            {
                Debug.WriteLine("No valid channel object passed to the StoreChannel method.");
                return;
            }

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
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

                        // Setze Aktualisierungsquery für Kanal-Tabelle ab.
                        using (var updateChannelStmt = conn.Prepare("UPDATE Channel " +
                            "SET Name=?, Description=?, CreationDate=?, ModificationDate=?, " +
                            "Type=?, Term=?, Location=?, Dates=?, Contact=?, Website=?, Deleted=?, NotificationSettings_NotifierId=? " + 
                            "WHERE Id=?;"))
                        {
                            updateChannelStmt.Bind(1, channel.Name);
                            updateChannelStmt.Bind(2, channel.Description);
                            updateChannelStmt.Bind(3, DatabaseManager.DateTimeToSQLite(channel.CreationDate));
                            updateChannelStmt.Bind(4, DatabaseManager.DateTimeToSQLite(channel.ModificationDate));
                            updateChannelStmt.Bind(5, (int)channel.Type);
                            updateChannelStmt.Bind(6, channel.Term);
                            updateChannelStmt.Bind(7, channel.Locations);
                            updateChannelStmt.Bind(8, channel.Dates);
                            updateChannelStmt.Bind(9, channel.Contacts);
                            updateChannelStmt.Bind(10, channel.Website);
                            updateChannelStmt.Bind(11, (channel.Deleted) ? 1 : 0);
                            updateChannelStmt.Bind(12, (int)channel.AnnouncementNotificationSetting);

                            updateChannelStmt.Bind(13, channel.Id);

                            updateChannelStmt.Step();
                            Debug.WriteLine("Update channel with id {0}.", channel.Id);
                        }

                        // Aktualisiere auch Subklassen-Tabelle abhängig vom Typ des Kanals.
                        switch (channel.Type)
                        {
                            case DataModel.Enums.ChannelType.LECTURE:
                                Lecture lecture = (Lecture)channel;

                                // Aktualisierung von Vorlesungs-Tabelle.
                                using (var updateLectureStmt = conn.Prepare("UPDATE Lecture SET " +
                                    "StartDate=?, EndDate=?, Lecturer=?, Assistant=? " +
                                    "WHERE Channel_Id=?;"))
                                {
                                    updateLectureStmt.Bind(1, lecture.StartDate);
                                    updateLectureStmt.Bind(2, lecture.EndDate);
                                    updateLectureStmt.Bind(3, lecture.Lecturer);
                                    updateLectureStmt.Bind(4, lecture.Assistant);
                                    updateLectureStmt.Bind(5, channel.Id);

                                    updateLectureStmt.Step();
                                }

                                Debug.WriteLine("Updated lecture.");
                                break;
                            case DataModel.Enums.ChannelType.EVENT:
                                Event channelEvent = (Event)channel;

                                // Aktualisierung von Event-Tabelle.
                                using (var updateEventStmt = conn.Prepare("UPDATE Event SET Cost=?, Organizer=? " +
                                    "WHERE Channel_Id=?;"))
                                {
                                    updateEventStmt.Bind(1, channelEvent.Cost);
                                    updateEventStmt.Bind(2, channelEvent.Organizer);
                                    updateEventStmt.Bind(3, channel.Id);

                                    updateEventStmt.Step();
                                }

                                Debug.WriteLine("Updated event.");
                                break;
                            case DataModel.Enums.ChannelType.SPORTS:
                                Sports channelSports = (Sports)channel;

                                // Aktualisierung von Sports-Tabelle.
                                using (var updateSportsStmt = conn.Prepare("UPDATE Sports SET Cost=?, NumberOfParticipants=? " +
                                    "WHERE Channel_Id=?;"))
                                {
                                    updateSportsStmt.Bind(1, channelSports.Cost);
                                    updateSportsStmt.Bind(2, channelSports.NumberOfParticipants);
                                    updateSportsStmt.Bind(3, channel.Id);

                                    updateSportsStmt.Step();
                                }

                                Debug.WriteLine("Updated sports.");
                                break;
                            default:
                                Debug.WriteLine("There is no subclass for channel type OTHER and STUDENT_GROUP, so updating is already complete.");
                                break;
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in UpdateChannelWithSubclass. The message is: {0}." + sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }
                        throw new DatabaseException("Update of the channel with id " + channel.Id + " has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in UpdateChannelWithSubclass. The message is: {0}, " +
                            "and the stack trace is: {1}.", ex.Message, ex.StackTrace);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }
                        throw new DatabaseException("Update of channel has failed.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende using Block.
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }       
        }

        /// <summary>
        /// Rufe alle in der Datenbank gespeicherten Kanäle ab, einschließlich Subklassen
        /// von Kanälen wie Lecture, Sports, Events, etc.
        /// </summary>
        /// <returns>Liste von Kanal-Objekten. Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Abrufen aller Kanäle fehlschlägt.</exception>
        public List<Channel> GetChannels()
        {
            List<Channel> channels = new List<Channel>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Frage zunächst Daten der Tabelle Kanal ab.
                        using (var getChannelsStmt = conn.Prepare("SELECT * FROM Channel;"))
                        {
                            // Iteriere über Ergebnisse.
                            while (getChannelsStmt.Step() == SQLiteResult.ROW)
                            {
                                Channel channelTmp = retrieveChannelObjectFromStatement(conn, getChannelsStmt);
                                channels.Add(channelTmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetChannels. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Get channels has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetChannels. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Get channels has failed.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende using Block.      
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            Debug.WriteLine("Return a channel list with {0} elements.", channels.Count);
            return channels;
        }

        /// <summary>
        /// Lösche den Kanal mit der angegebenen Id aus der Datenbank. Es werden auch die
        /// Datensätze aus Tabellen mit Subklassenattributen dieses Kanals gelöscht, falls
        /// welche existieren.
        /// </summary>
        /// <param name="channelId">Die Id des zu löschenden Kanals.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Löschvorgang nicht erfolgreich ist.</exception>
        public void DeleteChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var deleteChannelStmt = conn.Prepare(@"DELETE FROM Channel WHERE Id=?;"))
                        {
                            deleteChannelStmt.Bind(1, channelId);
                            deleteChannelStmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in DeleteChannel. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Delete channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in DeleteChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Delete channel has failed.");
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
        /// Prüft, ob der Kanal mit der angegebenen Id in der lokalen Datenbank vorhanden ist.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der geprüft werden soll.</param>
        /// <returns>Liefert true, wenn der Kanal in der lokalen DB gespeichert ist, sonst false.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Zugriff auf die Datenbank fehlschlägt.</exception>
        public bool IsChannelContained(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT COUNT(*) AS channelCount
                            FROM Channel
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, channelId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int resultCount = Convert.ToInt32(stmt["channelCount"]);

                                if (resultCount == 1)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsChannelContained. The message is: {0}.", sqlEx.Message);
                        return false;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsChannelContained. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        return false;
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

            return false;
        }

        /// <summary>
        /// Markiert den Kanal, der durch die angegbene Id identifiziert ist, als gelöscht.
        /// Der Kanal bleibt jedoch in den Datensätzen vorhanden.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der als gelöscht markiert werden soll.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Kanal nicht als gelöscht marktiert werden konnte.</exception>
        public void MarkChannelAsDeleted(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"UPDATE Channel 
                            SET Deleted=? WHERE Id=?;"))
                        {
                            stmt.Bind(1, 1);    // Setze Deleted auf true.
                            stmt.Bind(2, channelId);

                            stmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in MarkChannelAsDeleted. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Mark channel as deleted failed. " + sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in MarkChannelAsDeleted. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Mark channel as deleted failed. " + ex.Message);
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
        /// Gibt an, ob für den Kanal mit der übergebenen Id das Flag DeletionNoticed gesetzt ist.
        /// Das Flag gibt an, ob der Nutzer bereits über die Löschung des Kanals informiert wurde, d.h.
        /// diese auf jeden Fall bemerkt hat.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem das Flag abgefragt wird.</param>
        /// <returns>Liefert true, wenn das Flag gesetzt ist, sonst false.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf fehlschlägt.</exception>
        public bool IsChannelDeletionNoticedFlagSet(int channelId)
        {
            bool deletionNoticed = false;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT DeletionNoticedFlag 
                            FROM Channel 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, channelId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int deletionNoticedFlag = Convert.ToInt32(stmt["DeletionNoticedFlag"]);

                                deletionNoticed = (deletionNoticedFlag == 1) ? true : false;
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsChannelDeletionNoticedFlagSet. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Retrieve DeletionNoticedFlag failed. Msg is: " + sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsChannelDeletionNoticedFlagSet. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Retrieve DeletionNoticedFlag failed. Msg is: " + ex.Message);
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

            return deletionNoticed;
        }

        /// <summary>
        /// Setzt das Flag DeletionNoticed für den Kanal mit der angegebenen Id.
        /// Das Flag gibt an, ob der Nutzer bereits über die Löschung des Kanals informiert wurde, d.h.
        /// diese auf jeden Fall bemerkt hat.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den das Flag gesetzt wird.</param>
        /// <param name="flagValue">Der Wert des Flags.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Setzten des Flags fehlschlägt.</exception>
        public void SetChannelDeletionNoticedFlag(int channelId, bool flagValue)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"UPDATE Channel 
                            SET DeletionNoticedFlag=? 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, (flagValue) ? 1 : 0);
                            stmt.Bind(2, channelId);

                            stmt.Step();
                            Debug.WriteLine("Updated the DeletionNoticedFlag for channel with id {0}."  + 
                                "New value is: {1}.", channelId, flagValue);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in SetChannelDeletionNoticedFlag. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Setting of DeletionNoticedFlag failed. Msg is: " + sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in SetChannelDeletionNoticedFlag. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Setting of DeletionNoticedFlag failed. Msg is: " + ex.Message);
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
        /// Rufe den Kanal mit der angegebenen Id aus der Datenbank ab ung gibt ein Objekt
        /// vom Typ Channel mit den Daten zurück.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der abgerufen werden soll.</param>
        /// <returns>Eine Instanz der Klasse Channel. Kann null liefer, wenn der Kanal nicht in der Datenbank existiert.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Abrufen des Kanals fehlschlägt.</exception>
        public Channel GetChannel(int channelId)
        {
            Channel channel = null;
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Frage Daten aus Kanal-Tabelle ab.
                        using (var getChannelStmt = conn.Prepare("SELECT * FROM Channel WHERE Id=?;"))
                        {
                            getChannelStmt.Bind(1, channelId);

                            // Wurde ein Eintrag zurückgeliefert?
                            if (getChannelStmt.Step() == SQLiteResult.ROW)
                            {
                                channel = retrieveChannelObjectFromStatement(conn, getChannelStmt);

                                // Ermittle noch die Anzahl an ungelesenen Announcements für diesen Kanal.
                                string unreadAnnouncementsQuery = @"SELECT COUNT(*) AS UnreadAnnouncements 
                                    FROM Announcement AS a JOIN Message AS m ON a.Message_Id=m.Id 
                                    WHERE a.Channel_Id=? AND m.Read=?;";

                                using (var stmt = conn.Prepare(unreadAnnouncementsQuery))
                                {
                                    stmt.Bind(1, channelId);
                                    stmt.Bind(2, 0);    // Read soll false sein.

                                    stmt.Step();

                                    int nrOfUnreadAnnouncements = Convert.ToInt32(stmt["UnreadAnnouncements"]);
                                    Debug.WriteLine("Retrieved number of unread announcements for channel with id {0}. " +
                                        "The channel has {1} unread announcements.", channelId, nrOfUnreadAnnouncements);
                                    channel.NumberOfUnreadAnnouncements = nrOfUnreadAnnouncements;
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetChannel. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Get channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Get channel has failed.");
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
            
            return channel;
        }

        /// <summary>
        /// Hole alle Kanäle aus der Datenbank, die der lokale Nutzer abonniert hat.
        /// Es werden die Kanaldaten inklusiver der Anzahl ungelesener Nachrichten des Kanals geladen.
        /// </summary>
        /// <returns>Eine Liste von Objekten vom Typ Kanal oder vom Typ einer der Subklassen von Kanal. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Abrufen der abonnierten Kanäle fehlschlägt.</exception>
        public List<Channel> GetSubscribedChannels()
        {
            List<Channel> channels = new List<Channel>();
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string getChannels = @"SELECT * 
                            FROM Channel 
                            WHERE Id IN 
                                (SELECT Channel_Id AS Id FROM SubscribedChannels);";

                        string unreadAnnouncementsQuery = @"SELECT COUNT(*) AS UnreadAnnouncements 
                            FROM Announcement AS a JOIN Message AS m ON a.Message_Id=m.Id 
                            WHERE a.Channel_Id=? AND m.Read=?;";

                        // Frage alle Kanäle ab, die in SubscribedChannels eingetragen sind.
                        using (var getSubscribedChannelsStmt = conn.Prepare(getChannels))
                        {
                            using (var getAmountOfUnreadMsgStmt = conn.Prepare(unreadAnnouncementsQuery))
                            {
                                // Iteriere über Ergebnisse.
                                while (getSubscribedChannelsStmt.Step() == SQLiteResult.ROW)
                                {
                                    // Extrahiere Kanal.
                                    Channel channelTmp = retrieveChannelObjectFromStatement(conn, getSubscribedChannelsStmt);

                                    // Ermittle noch die Anzahl an ungelesenen Announcements für diesen Kanal.
                                    getAmountOfUnreadMsgStmt.Bind(1, channelTmp.Id);
                                    getAmountOfUnreadMsgStmt.Bind(2, 0);    // Read soll false sein.

                                    if(getAmountOfUnreadMsgStmt.Step() == SQLiteResult.ROW)
                                    {
                                        channelTmp.NumberOfUnreadAnnouncements = Convert.ToInt32(getAmountOfUnreadMsgStmt["UnreadAnnouncements"]);
                                    }

                                    // Setzte Statement zurück für nächste Iteration.
                                    getAmountOfUnreadMsgStmt.Reset();

                                    channels.Add(channelTmp);
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetSubscribedChannels. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Get subscribed channels has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in GetSubscribedChannels. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Get subscribed channel has failed.");
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

            return channels;
        }

        /// <summary>
        /// Trage den Kanal mit der angegebenen Id als abonnierten Kanal in die Datenbank ein.
        /// </summary>
        /// <param name="channelId">Die Id des zu abonnierenden Kanal.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Markieren des Kanals als abonniert in der DB fehlschlägt.</exception>
        public void SubscribeChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var subscribeStmt = conn.Prepare(@"INSERT INTO SubscribedChannels (Channel_Id) VALUES (?);"))
                        {
                            subscribeStmt.Bind(1, channelId);

                            subscribeStmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in SubscribeChannel. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Subscribe channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in SubscribeChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Subscribe channel has failed.");
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
        /// Entferne den Kanal mit der angebenen Id aus der Menge der abonnierten Kanäle in der Datenbank.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der deabonniert werden soll.</param>
        public void UnsubscribeChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var unsubscribeStmt = conn.Prepare(@"DELETE FROM SubscribedChannels WHERE Channel_Id=?;"))
                        {
                            unsubscribeStmt.Bind(1, channelId);

                            unsubscribeStmt.Step();
                            Debug.WriteLine("Unsubscribed from channel with id {0}.", channelId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Hier keine Abbildung auf DatabaseException.
                        Debug.WriteLine("SQLiteException has occurred in UnsubscribeChannel. The message is: {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in UnubscribeChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
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
            }
        }

        /// <summary>
        /// Prüft, ob der Kanal mit der angegebenen Id als abonnierter Kanal in der Datenbank gelistet ist.
        /// </summary>
        /// <param name="channelId">Die Id des zu prüfenden Kanals.</param>
        /// <returns>Liefert true, wenn der Kanal unter den abonnierten Kanälen gelistet ist, ansonsten false.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn die Prüfung fehlschlägt.</exception>
        public bool IsChannelSubscribed(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"SELECT * FROM SubscribedChannels WHERE Channel_Id=? LIMIT 1;"))
                        {
                            stmt.Bind(1, channelId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                return true;
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsChannelSubscribed. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("Check of subscription status of channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in IsChannelSubscribed. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Check of subscription status of channel has failed.");
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
            
            return false;
        }

        /// <summary>
        /// Liefert das Datum zurück, an dem zum letzten Mal ein Update der Liste aller in
        /// der Anwendung verwalteten Kanäle durchgeführt wurde.
        /// </summary>
        /// <returns>Ein Objekt vom Typ DateTimeOffset.</returns>
        public DateTimeOffset GetDateOfLastChannelListUpdate()
        {
            DateTimeOffset lastUpdate = DateTimeOffset.MinValue;

            using (SQLiteConnection conn = DatabaseManager.GetConnection())
            {
                try
                {
                    using (var statement = conn.Prepare(@"  SELECT * 
                                                        FROM LastUpdateOnChannelsList 
                                                        WHERE Id=0;"))
                    {
                        if (statement.Step() == SQLiteResult.ROW)
                        {
                            lastUpdate = DatabaseManager.DateTimeFromSQLite(statement["LastUpdate"].ToString());
                        }
                    }
                }
                catch (SQLiteException sqlEx)
                {
                    Debug.WriteLine("SQLiteException has occurred in GetDateOfLastChannelListUpdate. The message is: {0}, " +
                        "and the stack trace: {1}.", sqlEx.Message, sqlEx.StackTrace);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Exception has occurred in GetDateOfLastChannelListUpdate. The message is: {0}, " +
                        "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                }
            }
            
            return lastUpdate;
        }

        /// <summary>
        /// Setzt das Datum der letzten Aktualisierung der Kanäle, die in der Anwendung verwaltet werden.
        /// </summary>
        /// <param name="lastUpdate">Das Datum der letzten Änderung in Form eines DateTimeOffset Objekts.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Setzen des letzten Änderungsdatums fehlschlägt.</exception>
        public void SetDateOfLastChannelListUpdate(DateTimeOffset lastUpdate)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Frage zunächst ab, ob es schon ein Änderungsdatum in der Tabelle gibt.
                        DateTimeOffset tableEntry = GetDateOfLastChannelListUpdate();
                        if (tableEntry == DateTimeOffset.MinValue)
                        {
                            // Noch kein Eintrag in Tabelle, füge also einen ein.
                            using (var statement = conn.Prepare(@"INSERT INTO LastUpdateOnChannelsList (Id, LastUpdate) VALUES (?,?);"))
                            {
                                statement.Bind(1, 0);
                                statement.Bind(2, DatabaseManager.DateTimeToSQLite(lastUpdate));

                                statement.Step();
                            }
                        }
                        else
                        {
                            // Aktualisiere bereits vorhandenen Eintrag.
                            using (var statement = conn.Prepare(@"  UPDATE LastUpdateOnChannelsList 
                                                            SET LastUpdate=? WHERE Id=0;"))
                            {
                                statement.Bind(1, DatabaseManager.DateTimeToSQLite(lastUpdate));

                                statement.Step();
                            }
                        }

                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in SetDateOfLastChannelListUpdate. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("SetDateOfLastChannelListUpdate channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in SetDateOfLastChannelListUpdate. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("Setting the date of the last update on the channel list has failed.");
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
        /// Trage einen Moderator als Verantwortlichen für einen Kanal in der Datenbank ein.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem der Moderator hinzugefügt werden soll-</param>
        /// <param name="moderatorId">Die Id des hinzuzufügenden Moderators.</param>
        /// <param name="isActive">Gibt an, ob der Moderator den Kanal aktiv verwaltet.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Moderator nicht hinzugefügt werden konnte.</exception>
        public void AddModeratorToChannel(int channelId, int moderatorId, bool isActive)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Prüfe, ob bereits ein Eintrag für diesen Moderator in der Datenbank ist.
                        using (var checkStmt = conn.Prepare(@"SELECT * FROM ModeratorChannel 
                            WHERE Channel_Id=? AND Moderator_Id=?;"))
                        {
                            checkStmt.Bind(1, channelId);
                            checkStmt.Bind(2, moderatorId);

                            if (checkStmt.Step() == SQLiteResult.ROW)
                            {
                                // Aktualisiere das IsActive Feld des bestehenden Eintrags.
                                using (var stmt = conn.Prepare(@"UPDATE ModeratorChannel 
                                    SET Active=? 
                                    WHERE Channel_Id=? AND Moderator_Id=?;"))
                                {
                                    stmt.Bind(1, (isActive) ? 1 : 0);
                                    stmt.Bind(2, channelId);
                                    stmt.Bind(3, moderatorId);

                                    stmt.Step();
                                }
                            }
                            else
                            {
                                // Füge den Eintrag ein.
                                using (var stmt = conn.Prepare(@"INSERT INTO ModeratorChannel (Channel_Id, Moderator_Id, Active) 
                                    VALUES (?,?,?);"))
                                {
                                    stmt.Bind(1, channelId);
                                    stmt.Bind(2, moderatorId);
                                    stmt.Bind(3, (isActive) ? 1 : 0);

                                    stmt.Step();
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in AddModeratorToChannel. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("AddModeratorToChannel channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in AddModeratorToChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("AddModeratorToChannel channel has failed.");
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
        /// Entfernt alle Einträge von verantwortlichen Moderatoren für einen Kanal 
        /// aus der entsprechenden Datenbanktabelle. Löscht jedoch nicht die
        /// eigentlichen Moderatoren-Datensätze.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Einträge von verantwortlichen 
        ///     Moderatoren gelöscht werden sollen.</param>
        public void RemoveAllModeratorsFromChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"DELETE FROM ModeratorChannel WHERE Channel_Id=?;"))
                        {
                            stmt.Bind(1, channelId);

                            stmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Werfe keine Exception. Schlägt Löschvorgang fehl bleiben Einträge zwar lokal gespeichert,
                        // werden spätestens aber entfernt, wenn der Kanal gelöscht wird.
                        Debug.WriteLine("RemoveAllModerators from channel has failed. Message is {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("RemoveAllModerators from channel has failed. Message is {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
            }   
        }

        /// <summary>
        /// Liefert eine Liste an Moderatoren, die in der Datenbank als aktive Verantwortliche des Kanals
        /// eingetragen sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die verantwortlichen Moderatoren abgefragt werden sollen.</param>
        /// <returns>Eine Liste von Moderator Objekten.</returns>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn die Ausführung fehlschlägt.</exception>
        public List<Moderator> GetResponsibleModeratorsForChannel(int channelId)
        {
            List<Moderator> responsibleModerators = new List<Moderator>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        using (var stmt = conn.Prepare(@"SELECT * 
                            FROM Moderator AS m JOIN ModeratorChannel AS mc ON m.Id=mc.Moderator_Id 
                            WHERE mc.Channel_Id=? AND mc.Active=?;"))
                        {
                            stmt.Bind(1, channelId);
                            stmt.Bind(2, 1);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                int id = Convert.ToInt32(stmt["Id"]);
                                string firstName = (string)stmt["FirstName"];
                                string lastName = (string)stmt["LastName"];
                                string email = (string)stmt["Email"];

                                Moderator moderator = new Moderator()
                                {
                                    Id = id,
                                    FirstName = firstName,
                                    LastName = lastName,
                                    Email = email
                                };
                                responsibleModerators.Add(moderator);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetResponsibleModeratorsForChannel. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException("GetResponsibleModeratorsForChannel channel has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in GetResponsibleModeratorsForChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                        throw new DatabaseException("GetResponsibleModeratorsForChannel channel has failed.");
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
            
            return responsibleModerators;
        }

        /// <summary>
        /// Prüft ob der Moderator mit der angegebenen Id in der Datenbank als ein aktiver Verantwortlicher 
        /// des Kanals mit der gegebenen Id eingetragen ist. 
        /// </summary>
        /// <param name="channelId">Die Kanal-Id des zu prüfenden Kanals.</param>
        /// <param name="moderatorId">Die Moderator-Id des zu prüfenden Moderators.</param>
        /// <returns>Liefert true, wenn der Moderator als aktiver Verantworlichter des Kanals eingetragen ist, anonsten false.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Zugriff auf Datenbank nicht möglich war.</exception>
        public bool IsResponsibleForChannel(int channelId, int moderatorId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT COUNT(*) AS countResults 
                            FROM Moderator AS m JOIN ModeratorChannel AS mc ON m.Id=mc.Moderator_Id 
                            WHERE m.Id=? AND mc.Channel_Id=? AND mc.Active=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, moderatorId);
                            stmt.Bind(2, channelId);
                            stmt.Bind(3, 1);    // true

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int resultCount = Convert.ToInt32(stmt["countResults"]);

                                if (resultCount == 1)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in IsResponsibleForChannel. The message is: {0}.", sqlEx.Message);
                    }
                    catch (DatabaseException ex)
                    {
                        Debug.WriteLine("Exception has occurred in IsResponsibleForChannel. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
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

            return false;
        }

        /// <summary>
        /// Holt die vom Moderator mit der angegebenen Id verwalteten Kanäle aus der lokalen Datenbank.
        /// </summary>
        /// <param name="moderatorId">Die Id des Moderators.</param>
        /// <returns>Liste von Channel Objekten.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn der Abruf der Daten fehlschlägt.</exception>
        public List<Channel> GetManagedChannels(int moderatorId)
        {
            List<Channel> managedChannels = new List<Channel>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT * 
                            FROM Channel AS c JOIN ModeratorChannel AS mc ON c.Id=mc.Channel_Id 
                            WHERE mc.Moderator_Id=? AND mc.Active=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, moderatorId);
                            stmt.Bind(2, 1);        // true

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                Channel channelTmp = retrieveChannelObjectFromStatement(conn, stmt);

                                managedChannels.Add(channelTmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in GetManagedChannels. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in GetManagedChannels. The message is: {0}, " +
                            "and the stack trace: {1}.", ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return managedChannels;
        }

        /// <summary>
        /// Speichere die Daten der gegebenen Announcement in der Datenbank ab.
        /// </summary>
        /// <param name="announcement">Das Announcement Objekt mit den Announcement Daten.</param>
        /// <exception cref="DatabaseException">Wirft eine Exception, wenn die Speicherung fehlschlägt.</exception>
        public void StoreAnnouncement(Announcement announcement)
        {
            if(announcement == null)
            {
                Debug.WriteLine("No valid announcement object passed to the StoreAnnouncement method.");
                return;
            }

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
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

                        // Speichere Daten in Message Tabelle.
                        using (var insertMessageStmt = conn.Prepare(@"INSERT INTO Message (Id, Text, CreationDate, Priority, Read) 
                            VALUES (?,?,?,?,?);"))
                        {
                            insertMessageStmt.Bind(1, announcement.Id);
                            insertMessageStmt.Bind(2, announcement.Text);
                            insertMessageStmt.Bind(3, DatabaseManager.DateTimeToSQLite(announcement.CreationDate));
                            insertMessageStmt.Bind(4, (int)announcement.MessagePriority);
                            insertMessageStmt.Bind(5, 0);   // Nachricht noch nicht gelesen.

                            insertMessageStmt.Step();
                        }

                        // Speichere Daten in Announcement Tabelle.
                        using (var insertAnnouncementStmt = conn.Prepare(@"INSERT INTO Announcement (MessageNumber,
                            Channel_Id, Title, Author_Moderator_Id, Message_Id) VALUES (?,?,?,?,?);"))
                        {
                            insertAnnouncementStmt.Bind(1, announcement.MessageNumber);
                            insertAnnouncementStmt.Bind(2, announcement.ChannelId);
                            insertAnnouncementStmt.Bind(3, announcement.Title);
                            insertAnnouncementStmt.Bind(4, announcement.AuthorId);
                            insertAnnouncementStmt.Bind(5, announcement.Id);

                            insertAnnouncementStmt.Step();
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                            Debug.WriteLine("Announcement with id {0} stored.", announcement.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in StoreAnnouncement. Exception message is: {0}.", sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing announcement data in database has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in StoreAnnouncement. " +
                            "Exception message is: {0}, and stack trace is {1}.", ex.Message, ex.StackTrace);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing announcement data in database has failed.");
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
        /// Speichert eine Menge von Announcement Objekten in der Datenbank ab.
        /// </summary>
        /// <param name="announcements">Eine Liste von Announcement-Objekten.</param>
        /// <exception cref="DatabaseException">Wirft eine Exception, wenn die Speicherung fehlschlägt.</exception>
        public void BulkInsertOfAnnouncements(List<Announcement> announcements)
        {
            if(announcements == null || announcements.Count == 0)
            {
                Debug.WriteLine("No announcements to insert in BulkInsertOfAnnouncements.");
                return;
            }

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
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

                        // Statement für die Message-Tabelle.
                        var insertMessageStmt = conn.Prepare(@"INSERT INTO Message (Id, Text,
                            CreationDate, Priority, Read) VALUES (?,?,?,?,?);");
                        // Statement für die Announcement-Tabelle.
                        var insertAnnouncementStmt = conn.Prepare(@"INSERT INTO Announcement (MessageNumber,
                            Channel_Id, Title, Author_Moderator_Id, Message_Id) VALUES (?,?,?,?,?);");

                        // Führe insert-Statements durch.
                        foreach (Announcement announcement in announcements)
                        {
                            insertMessageStmt.Bind(1, announcement.Id);
                            insertMessageStmt.Bind(2, announcement.Text);
                            insertMessageStmt.Bind(3, DatabaseManager.DateTimeToSQLite(announcement.CreationDate));
                            insertMessageStmt.Bind(4, (int)announcement.MessagePriority);
                            insertMessageStmt.Bind(5, 0);   // Nachricht noch nicht gelesen.

                            if (insertMessageStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to store the current announcement with id {0}.", announcement.Id);

                            insertAnnouncementStmt.Bind(1, announcement.MessageNumber);
                            insertAnnouncementStmt.Bind(2, announcement.ChannelId);
                            insertAnnouncementStmt.Bind(3, announcement.Title);
                            insertAnnouncementStmt.Bind(4, announcement.AuthorId);
                            insertAnnouncementStmt.Bind(5, announcement.Id);

                            if (insertAnnouncementStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Failed to store the current announcement with id {0}.", announcement.Id);

                            insertMessageStmt.Reset();
                            insertAnnouncementStmt.Reset();
                        }

                        // Commit der Transaktion.
                        using (var statement = conn.Prepare("COMMIT TRANSACTION"))
                        {
                            statement.Step();
                            Debug.WriteLine("Stored {0} announcements in the database.", announcements.Count);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("SQLiteException has occurred in BulkInsertOfAnnouncements. Exception message is: {0}.", sqlEx.Message);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing announcement data in database has failed.");
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Exception has occurred in BulkInsertOfAnnouncements. " +
                            "Exception message is: {0}, and stack trace is {1}.", ex.Message, ex.StackTrace);
                        // Rollback der Transaktion.
                        using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                        {
                            statement.Step();
                        }

                        throw new DatabaseException("Storing announcement data in database has failed.");
                    }
                    finally
                    {
                        mutex.ReleaseMutex();
                    }
                }   // Ende using Block     
            }
            else
            {
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }       
        }

        /// <summary>
        /// Löscht alle Announcement Nachrichten aus der Datenbank, die 
        /// für den Kanal mit der angegebenen Id gespeichert sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals dessen Announcements gelöscht werden sollen.</param>
        public void DeleteAllAnnouncementsOfChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Lösche die Einträge aus der Message Tabelle, für die 
                        // es einen entsprechenden Eintrag in der Announcement-Tabelle für den Kanal gibt.
                        // Die entsprechenden Announcement-Einträge werden dann automatisch gelöscht (CASCADE).
                        using (var deleteMsgStmt = conn.Prepare(@"DELETE FROM Message 
                            WHERE Id IN (
                                SELECT Message_Id as Id 
                                FROM Announcement 
                                WHERE Channel_Id=?
                            );"))
                        {
                            deleteMsgStmt.Bind(1, channelId);

                            deleteMsgStmt.Step();
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        // Keine Abbildung auf eine DatabaseException bei dieser Operation.
                        Debug.WriteLine("DeleteAllAnnouncementsOfChannel has failed. The message is: {0}.", sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteAllAnnouncementsOfChannel has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
            } 
        }

        /// <summary>
        /// Ruft zu den vom lokalen Nutzer abonnierten Kanälen die Anzahl an ungelesenen Nachrichten ab
        /// und speichert diese in einem Verzeichnis. Das Verzeichnis bildet ab von der Kanal-Id auf 
        /// die Anzahl an ungelesenen Announcements für diesen Kanal. Das Verzeichnis enthält nur Einträge 
        /// bei denen die Anzahl ungelesener Nachrichten größer 0 ist.
        /// </summary>
        /// <returns>Ein Verzeichnis, indem für jeden abonnierten Kanal mit mehr als einer ungelesenen Nachricht die Anzahl der ungelesenen Announcements
        ///     gespeichert werden. Die Anzahl kann über die Kanal-Id als Schlüssel extrahiert werden.</returns>
        public Dictionary<int, int> DetermineAmountOfUnreadAnnouncementForMyChannels()
        {
            Dictionary<int, int> amountOfUnreadMsgMap = new Dictionary<int, int>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        // Frage zu allen abonnierten Kanälen die ungelesenen Nachrichten ab und zähle sie.
                        string query = @"SELECT a.Channel_Id AS Channel_Id, COUNT(*) AS NrUnreadMessages 
                            FROM Message AS m JOIN Announcement AS a ON m.Id=a.Message_Id
                            WHERE a.Channel_Id IN (
                                    SELECT Channel_Id 
                                    FROM SubscribedChannels)
                                AND m.Read=? 
                            GROUP BY a.Channel_Id;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, 0);    // Nachrichten, die nicht als gelesen markiert sind.

                            // Iteriere über Ergebnisse.
                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                int channelId = Convert.ToInt32(stmt["Channel_Id"]);
                                int amountOfUnreadMsg = Convert.ToInt32(stmt["NrUnreadMessages"]);

                                // Speichere das Tupel im Verzeichnis.
                                amountOfUnreadMsgMap.Add(channelId, amountOfUnreadMsg);
                            }
                        }

                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("DetermineAmountOfUnreadAnnouncementForMyChannels has failed. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (DatabaseException ex)
                    {
                        Debug.WriteLine("DetermineAmountOfUnreadAnnouncementForMyChannels has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return amountOfUnreadMsgMap;
        }

        /// <summary>
        /// Holt die Announcement Daten zu dem Kanal mit der angegebenen Id aus der Datenbank.
        /// Die Methode gibt alle Announcements zu dem angegebenen Kanal zurück.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, zu dem die Announcements abgerufen werden sollen.</param>
        /// <returns>Eine Liste von Announcement Objekten.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf der Announcements fehlschlägt.</exception>
        public List<Announcement> GetAllAnnouncementsOfChannel(int channelId)
        {
            List<Announcement> announcements = new List<Announcement>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT * 
                            FROM Message AS m JOIN Announcement AS a ON m.Id=a.Message_Id 
                            WHERE Channel_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, channelId);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                int id = Convert.ToInt32(stmt["Id"]);
                                string text = (string)stmt["Text"];
                                DateTimeOffset creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"].ToString());
                                Priority priority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);
                                bool read = ((long)stmt["Read"] == 1) ? true : false;
                                int messageNr = Convert.ToInt32(stmt["MessageNumber"]);
                                int authorId = Convert.ToInt32(stmt["Author_Moderator_Id"]);
                                string title = (string)stmt["Title"];

                                Announcement announcement = new Announcement(id, text, messageNr, creationDate, priority, read, channelId, authorId, title);
                                announcements.Add(announcement);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetAllAnnouncementsOfChannel has failed. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetAllAnnouncementsOfChannel has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return announcements;
        }

        /// <summary>
        /// Holt die angegebene Anzahl an aktuellesten Announcements aus der Datenbank. Die aktuellesten
        /// Announcements sind dabei diejenigen, die zeitlich gesehen zuletzt gesendet wurden. Über den Offset 
        /// kann angegeben werden, dass diese Anzahl an Announcements übersprungen werden soll. Das ist für das 
        /// inkrementelle Laden von älteren Announcements wichtig.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, von dem die Announcements abgerufen werden sollen.</param>
        /// <param name="number">Die Anzahl an Announcements, die abgerufen werden soll.</param>
        /// <param name="offset">Der Offset, der angibt wie viele der neusten Announcements übersprungen werden sollen.</param>
        /// <returns>Eine Liste von Announcement Objekten.</returns>
        /// <exception cref="DatbaseException">Wirft DatabaseException, wenn der Abfruf der Announcements fehlschlägt.</exception>
        public List<Announcement> GetLatestAnnouncements(int channelId, int number, int offset)
        {
            List<Announcement> latestAnnouncements = new List<Announcement>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT * 
                            FROM Message AS m JOIN Announcement AS a ON m.Id=a.Message_Id 
                            WHERE Channel_Id=? 
                            ORDER BY a.MessageNumber DESC 
                            LIMIT ? OFFSET ?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, channelId);
                            stmt.Bind(2, number);
                            stmt.Bind(3, offset);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                int id = Convert.ToInt32(stmt["Id"]);
                                string text = (string)stmt["Text"];
                                DateTimeOffset creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"].ToString());
                                Priority priority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);
                                bool read = ((long)stmt["Read"] == 1) ? true : false;
                                int messageNr = Convert.ToInt32(stmt["MessageNumber"]);
                                int authorId = Convert.ToInt32(stmt["Author_Moderator_Id"]);
                                string title = (string)stmt["Title"];

                                Announcement announcement = new Announcement(id, text, messageNr, creationDate, priority, read, channelId, authorId, title);
                                latestAnnouncements.Add(announcement);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("GetLatestAnnouncements has failed. The message is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetLatestAnnouncements has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
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
            
            return latestAnnouncements;
        }

        /// <summary>
        /// Liefert die höchste MessageNumber, die einer der 
        /// in der Datenbank für den angegebenen Kanal gespeicherten Announcement zugeordnet ist.
        /// </summary>
        /// <param name="channelId">Die Kanal-Id des Kanals, für den die höchste gespeicherte MessageNr abgerufen werden soll.</param>
        /// <returns>Die höchste MessageNr.</returns>
        public int GetHighestMessageNumberOfChannel(int channelId)
        {
            int highestNr = 0;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"SELECT MAX(MessageNumber) AS HighestMsgNr
                            FROM Announcement 
                            WHERE Channel_Id=?;";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, channelId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                highestNr = Convert.ToInt32(stmt["HighestMsgNr"]);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Retrieval of highest message number for channel with id {0} has failed. " +
                            "Message is {1}.", channelId, sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Retrieval of highest message number has failed."
                            + " Message is {0} and stack trace is {1}.", ex.Message, ex.StackTrace);
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
            }

            return highestNr;
        }

        /// <summary>
        /// Markiert die bislang als ungelesen markierten Announcements des Kanals mit 
        /// der angegebenen Id in der Datenbank als gelesen.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, dessen Nachrichten als gelesen markiert werden sollen.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Markierung der Announcements fehlschlägt.</exception>
        public void MarkAnnouncementsAsRead(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string query = @"UPDATE Message 
                            SET Read=? 
                            WHERE Read=? AND Id IN (
                                SELECT Message_Id As Id 
                                FROM Announcement 
                                WHERE Channel_Id=?);";

                        using (var stmt = conn.Prepare(query))
                        {
                            stmt.Bind(1, 1);    // Setze Read auf true.
                            stmt.Bind(2, 0);
                            stmt.Bind(3, channelId);

                            stmt.Step();
                            Debug.WriteLine("Marked announcements as read for channel with id {0}.", channelId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("MarkAnnouncementsAsRead has failed.");
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("MarkAnnouncementsAsRead has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
                        throw new DatabaseException(ex.Message);
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

        #region Reminder
        /// <summary>
        /// Speichert den übergebenen Reminder in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="reminder">Das Reminder Objekt mit den Daten des Reminder.</param>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn das Abspeichern fehlschlägt.</exception>
        public void StoreReminder(Reminder reminder)
        {
            if (reminder == null)
                return;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"INSERT INTO Reminder (Id, Channel_Id, StartDate, EndDate, CreationDate, 
                            ModificationDate, ""Interval"", ""Ignore"", Title, Text, Priority, Author_Moderator_Id) 
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

                        using (var insertStmt = conn.Prepare(sql))
                        {
                            insertStmt.Bind(1, reminder.Id);
                            insertStmt.Bind(2, reminder.ChannelId);
                            insertStmt.Bind(3, DatabaseManager.DateTimeToSQLite(reminder.StartDate));
                            insertStmt.Bind(4, DatabaseManager.DateTimeToSQLite(reminder.EndDate));
                            insertStmt.Bind(5, DatabaseManager.DateTimeToSQLite(reminder.CreationDate));
                            insertStmt.Bind(6, DatabaseManager.DateTimeToSQLite(reminder.ModificationDate));
                            insertStmt.Bind(7, reminder.Interval);
                            insertStmt.Bind(8, (reminder.Ignore) ? 1 : 0);
                            insertStmt.Bind(9, reminder.Title);
                            insertStmt.Bind(10, reminder.Text);
                            insertStmt.Bind(11, (int)reminder.MessagePriority);
                            insertStmt.Bind(12, reminder.AuthorId);

                            insertStmt.Step();

                            Debug.WriteLine("Stored reminder with id {0}.", reminder.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Couldn't store reminder with id {0}.", reminder.Id);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("StoreReminder has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }

        /// <summary>
        /// Speichert eine Liste von Reminder Objekten in der lokalen Datenbank ab.
        /// </summary>
        /// <param name="reminderList">Eine Liste von Reminder Objekten.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Bulk Insert fehlschlägt.</exception>
        public void BulkInsertReminder(List<Reminder> reminderList)
        {
            if (reminderList == null || reminderList.Count == 0)
                return;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"INSERT INTO Reminder (Id, Channel_Id, StartDate, EndDate, CreationDate, 
                            ModificationDate, ""Interval"", ""Ignore"", Title, Text, Priority, Author_Moderator_Id) 
                            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

                        using (var insertStmt = conn.Prepare(sql))
                        {
                            // Führe Insert für alle Reminder aus.
                            foreach (Reminder reminder in reminderList)
                            {
                                insertStmt.Bind(1, reminder.Id);
                                insertStmt.Bind(2, reminder.ChannelId);
                                insertStmt.Bind(3, DatabaseManager.DateTimeToSQLite(reminder.StartDate));
                                insertStmt.Bind(4, DatabaseManager.DateTimeToSQLite(reminder.EndDate));
                                insertStmt.Bind(5, DatabaseManager.DateTimeToSQLite(reminder.CreationDate));
                                insertStmt.Bind(6, DatabaseManager.DateTimeToSQLite(reminder.ModificationDate));
                                insertStmt.Bind(7, reminder.Interval);
                                insertStmt.Bind(8, (reminder.Ignore) ? 1 : 0);
                                insertStmt.Bind(9, reminder.Title);
                                insertStmt.Bind(10, reminder.Text);
                                insertStmt.Bind(11, (int)reminder.MessagePriority);
                                insertStmt.Bind(12, reminder.AuthorId);

                                if (insertStmt.Step() != SQLiteResult.DONE)
                                {
                                    Debug.WriteLine("Failed to insert reminder with id {0}.", reminder.Id);
                                }

                                // Vorbereiten des Stmt für nächste Iteration.
                                insertStmt.Reset();
                            }
                        }

                        Debug.WriteLine("Successfully stored {0} reminders per bulk insertion.", reminderList.Count);

                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Bulk insert of reminders has failed. Msg is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Bulk insert of reminders has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }

        /// <summary>
        /// Aktualisiert den lokalen Datensatz für den übergebenen Reminder.
        /// Schreibt die Werte des übergebenen Objekts als neuen Datensatz anstelle des alten
        /// Datensatzes in die Datenbank.
        /// </summary>
        /// <param name="updatedReminder">Das Reminder Objekt mit den aktualisierten Reminder-Daten.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Update fehlschlägt.</exception>
        public void UpdateReminder(Reminder updatedReminder)
        {
            if (updatedReminder == null)
                return;

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"UPDATE Reminder 
                            SET Channel_Id=?, StartDate=?, EndDate=?, CreationDate=?, ModificationDate=?, 
                            ""Interval""=?, ""Ignore""=?, Title=?, Text=?, Priority=?, Author_Moderator_Id=? 
                            WHERE Id=?;";

                        using (var updateStmt = conn.Prepare(sql))
                        {
                            updateStmt.Bind(1, updatedReminder.ChannelId);
                            updateStmt.Bind(2, DatabaseManager.DateTimeToSQLite(updatedReminder.StartDate));
                            updateStmt.Bind(3, DatabaseManager.DateTimeToSQLite(updatedReminder.EndDate));
                            updateStmt.Bind(4, DatabaseManager.DateTimeToSQLite(updatedReminder.CreationDate));
                            updateStmt.Bind(5, DatabaseManager.DateTimeToSQLite(updatedReminder.ModificationDate));
                            updateStmt.Bind(6, updatedReminder.Interval);
                            updateStmt.Bind(7, (updatedReminder.Ignore) ? 1 : 0);
                            updateStmt.Bind(8, updatedReminder.Title);
                            updateStmt.Bind(9, updatedReminder.Text);
                            updateStmt.Bind(10, (int)updatedReminder.MessagePriority);
                            updateStmt.Bind(11, updatedReminder.AuthorId);

                            updateStmt.Bind(12, updatedReminder.Id);

                            if (updateStmt.Step() != SQLiteResult.DONE)
                                Debug.WriteLine("Update for reminder with id {0} has failed.", updatedReminder.Id);
                            else
                                Debug.WriteLine("Successfully updated reminder with id {0}.", updatedReminder.Id);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Update of reminder with id {0} has failed. Msg is: {1}.",
                            updatedReminder.Id,
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("Update of reminder has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }

        /// <summary>
        /// Liefert eine Liste von Reminder Objekten, die in der lokalen Datenbank für den
        /// Kanal mit der angegebenen Id gespeichert sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Reminder abgerufen werden sollen.</param>
        /// <returns>Eine Liste von Reminder Objekten.</returns>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn das Abrufen der Informationen fehlschlägt.</exception>
        public List<Reminder> GetRemindersForChannel(int channelId)
        {
            List<Reminder> reminders = new List<Reminder>();

            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT *
                            FROM Reminder 
                            WHERE Channel_Id=?;";

                        int id, interval, authorId;
                        string title, text;
                        bool ignore;
                        Priority priority;
                        DateTimeOffset startDate, endDate, creationDate, modificationDate;

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, channelId);

                            while (stmt.Step() == SQLiteResult.ROW)
                            {
                                id = Convert.ToInt32(stmt["Id"]);
                                startDate = DatabaseManager.DateTimeFromSQLite(stmt["StartDate"].ToString());
                                endDate = DatabaseManager.DateTimeFromSQLite(stmt["EndDate"].ToString());
                                creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"].ToString());
                                modificationDate = DatabaseManager.DateTimeFromSQLite(stmt["ModificationDate"].ToString());
                                interval = Convert.ToInt32(stmt["Interval"]);
                                ignore = ((long)stmt["Ignore"] == 1) ? true : false;
                                title = (string)stmt["Title"];
                                text = (string)stmt["Text"];
                                authorId = Convert.ToInt32(stmt["Author_Moderator_Id"]);
                                priority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);

                                Reminder reminderTmp = new Reminder(id, creationDate, modificationDate, startDate, endDate, interval,
                                    ignore, channelId, authorId, title, text, priority);
                                reminders.Add(reminderTmp);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Extraction of reminders for channel with id {0} has failed. Msg is: {1}.",
                            channelId,
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetRemindersForChannel has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return reminders;
        }

        /// <summary>
        /// Holt den Reminder mit der angegebenen Id aus der lokalen Datenbank, sofern
        /// der Datensatz vorhanden ist.
        /// </summary>
        /// <param name="reminderId">Die Id des Reminders, der aus der DB geholt werden soll.</param>
        /// <returns>Ein Objekt vom Typ Reminder, oder null, falls kein Datensatz existiert hat.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Abruf der Daten fehlschlägt.</exception>
        public Reminder GetReminder(int reminderId)
        {
            Reminder reminder = null;

             // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT * 
                            FROM Reminder 
                            WHERE Id=?;";

                        int channelId, interval, authorId;
                        string title, text;
                        bool ignore;
                        Priority priority;
                        DateTimeOffset startDate, endDate, creationDate, modificationDate;

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, reminderId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                channelId = Convert.ToInt32(stmt["Channel_Id"]);
                                startDate = DatabaseManager.DateTimeFromSQLite(stmt["StartDate"].ToString());
                                endDate = DatabaseManager.DateTimeFromSQLite(stmt["EndDate"].ToString());
                                creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"].ToString());
                                modificationDate = DatabaseManager.DateTimeFromSQLite(stmt["ModificationDate"].ToString());
                                interval = Convert.ToInt32(stmt["Interval"]);
                                ignore = ((long)stmt["Ignore"] == 1) ? true : false;
                                title = (string)stmt["Title"];
                                text = (string)stmt["Text"];
                                authorId = Convert.ToInt32(stmt["Author_Moderator_Id"]);
                                priority = (Priority)Enum.ToObject(typeof(Priority), stmt["Priority"]);

                                reminder = new Reminder(reminderId, creationDate, modificationDate, startDate, endDate, interval,
                                    ignore, channelId, authorId, title, text, priority);
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Extraction of reminder with id {0} has failed. Msg is: {1}.",
                            reminderId,
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("GetReminder has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return reminder;
        }

        /// <summary>
        /// Löscht alle Reminder Datensätze, die zu dem Kanal mit der angegebenen Id gespeichert sind.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, für den die Reminder gelöscht werden sollen.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Löschen fehlschlägt.</exception>
        public void DeleteRemindersForChannel(int channelId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"DELETE FROM Reminder 
                            WHERE Channel_Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, channelId);

                            stmt.Step();
                            Debug.WriteLine("Deleted reminders from channel with id {0}.", channelId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Deletion of reminders for channel with id {0} has failed. Msg is: {1}.",
                            channelId,
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteRemindersForChannel has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }

        /// <summary>
        /// Löscht den Reminder mit der angegebenen Id aus der Datenbank. 
        /// </summary>
        /// <param name="reminderId">Die Id des zu löschenden Reminders.</param>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Löschen fehlschlägt.</exception>
        public void DeleteReminder(int reminderId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"DELETE FROM Reminder 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, reminderId);

                            stmt.Step();
                            Debug.WriteLine("Deleted reminder with id {0}.", reminderId);
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Deletion of reminder with id {0} has failed. Msg is: {1}.",
                            reminderId,
                            sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("DeleteReminder has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }
        }

        /// <summary>
        /// Prüft, ob es einen Datensatz für den Reminder mit der übergebenen Id in der 
        /// lokalen Datenbank gibt.
        /// </summary>
        /// <param name="reminderId">Die Id des zu prüfenden Reminder.</param>
        /// <returns>Liefert true, wenn der Datensatz in der lokalen DB existiert, sonst false.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn Prüfung fehlschlägt.</exception>
        public bool IsReminderContained(int reminderId)
        {
            // Frage das Mutex Objekt ab.
            Mutex mutex = DatabaseManager.GetDatabaseAccessMutexObject();

            // Fordere Zugriff auf die Datenbank an.
            if (mutex.WaitOne(4000))
            {
                using (SQLiteConnection conn = DatabaseManager.GetConnection())
                {
                    try
                    {
                        string sql = @"SELECT COUNT(*) AS Amount
                            FROM Reminder 
                            WHERE Id=?;";

                        using (var stmt = conn.Prepare(sql))
                        {
                            stmt.Bind(1, reminderId);

                            if (stmt.Step() == SQLiteResult.ROW)
                            {
                                int resultAmount = Convert.ToInt32(stmt["Amount"]);

                                if (resultAmount == 1)
                                    return true;
                            }
                        }
                    }
                    catch (SQLiteException sqlEx)
                    {
                        Debug.WriteLine("Error during IsReminderContained execution. Msg is: {0}.", sqlEx.Message);
                        throw new DatabaseException(sqlEx.Message);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("IsReminderContained has failed. The message is: {0} and stack trace is {1}.",
                            ex.Message, ex.StackTrace);
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
                Debug.WriteLine("Couldn't get access to database. Time out.");
                throw new DatabaseException("Could not get access to the database.");
            }

            return false;
        }
        #endregion Reminder

        /// <summary>
        /// Hilfsmethode, die aus einem durch eine Query zurückgelieferten Statement ein Objekt des Typs Kanal extrahiert.
        /// Je nach Typ des Kanals werden zusätzliche Informationen aus Subklassen-Tabellen abgefragt und ein Objekt
        /// der Subklasse extrahiert.
        /// </summary>
        /// <param name="conn">Eine aktive Connection zur Datenbank, um Informationen aus Subklassen-Tabellen abfragen zu können.</param>
        /// <param name="stmt">Das Statement, aus dem die Informationen extrahiert werden sollen.</param>
        /// <returns>Ein Objekt vom Typ Channel.</returns>
        private Channel retrieveChannelObjectFromStatement(SQLiteConnection conn, ISQLiteStatement stmt)
        {
            // Channel Objekt.
            Channel channel = null;

            try
            {
                // Initialisierung der Variablen.
                int id;
                string name, description, term, location, dates, contact, website, startDate, endDate, lecturer,
                    assistant, cost, organizer, participants;
                bool deleted;
                ChannelType type;
                Faculty faculty;
                NotificationSetting announcementNotificationSetting;
                DateTimeOffset creationDate, modificationDate;

                // Frage Kanal-Werte ab.
                id = Convert.ToInt32(stmt["Id"]);
                name = (string)stmt["Name"];
                description = (string)stmt["Description"];
                type = (ChannelType)Enum.ToObject(typeof(ChannelType), stmt["Type"]);
                creationDate = DatabaseManager.DateTimeFromSQLite(stmt["CreationDate"].ToString());
                modificationDate = DatabaseManager.DateTimeFromSQLite(stmt["ModificationDate"].ToString());
                term = (string)stmt["Term"];
                location = (string)stmt["Location"];
                dates = (string)stmt["Dates"];
                contact = (string)stmt["Contact"];
                website = (string)stmt["Website"];
                deleted = ((long)stmt["Deleted"] == 1) ? true : false;
                announcementNotificationSetting = (NotificationSetting)Enum.ToObject(typeof(NotificationSetting), stmt["NotificationSettings_NotifierId"]);

                // Falls notwendig, hole Daten aus Tabelle der Subklasse.
                switch (type)
                {
                    case ChannelType.LECTURE:
                        using (var getLectureStmt = conn.Prepare("SELECT * FROM Lecture WHERE Channel_Id=?;"))
                        {
                            getLectureStmt.Bind(1, id);

                            // Hole Ergebnis der Query.
                            if (getLectureStmt.Step() == SQLiteResult.ROW)
                            {
                                faculty = (Faculty)Enum.ToObject(typeof(Faculty), getLectureStmt["Faculty"]);
                                startDate = (string)getLectureStmt["StartDate"];
                                endDate = (string)getLectureStmt["EndDate"];
                                lecturer = (string)getLectureStmt["Lecturer"];
                                assistant = (string)getLectureStmt["Assistant"];

                                // Erstelle Lecture Objekt und füge es der Liste hinzu.
                                Lecture lecture = new Lecture(id, name, description, type, creationDate, modificationDate, term, location,
                                    dates, contact, website, deleted, faculty, startDate, endDate, lecturer, assistant);
                                channel = lecture;
                            }
                        }
                        break;
                    case ChannelType.EVENT:
                        using (var getEventStmt = conn.Prepare("SELECT * FROM Event WHERE Channel_Id=?;"))
                        {
                            getEventStmt.Bind(1, id);

                            // Hole Ergebnis der Query.
                            if (getEventStmt.Step() == SQLiteResult.ROW)
                            {
                                cost = (string)getEventStmt["Cost"];
                                organizer = (string)getEventStmt["Organizer"];

                                // Erstelle Event Objekt und füge es der Liste hinzu.
                                Event eventObj = new Event(id, name, description, type, creationDate, modificationDate, term, location,
                                    dates, contact, website, deleted, cost, organizer);
                                channel = eventObj;
                            }
                        }
                        break;
                    case ChannelType.SPORTS:
                        using (var getSportsStmt = conn.Prepare("SELECT * FROM Sports WHERE Channel_Id=?;"))
                        {
                            getSportsStmt.Bind(1, id);

                            // Hole Ergebnis der Query.
                            if (getSportsStmt.Step() == SQLiteResult.ROW)
                            {
                                cost = (string)getSportsStmt["Cost"];
                                participants = (string)getSportsStmt["NumberOfParticipants"];

                                // Erstelle Sports Objekt und füge es der Liste hinzu.
                                Sports sportsObj = new Sports(id, name, description, type, creationDate, modificationDate, term, location,
                                    dates, contact, website, deleted, cost, participants);
                                channel = sportsObj;
                            }
                        }
                        break;
                    default:
                        // Keine Subklasse, also erzeuge Kanal Objekt.
                        channel = new Channel(id, name, description, type, creationDate, modificationDate, term, location,
                            dates, contact, website, deleted);
                        break;
                }

                // Füge announcementNotificationSetting Einstellung noch dem Objekt hinzu.
                channel.AnnouncementNotificationSetting = announcementNotificationSetting;
            }
            catch(SQLiteException sqlEx){
                Debug.WriteLine("SQLiteException has occurred in retrieveChannelObjectFromStatement. The message is: {0}.", sqlEx.Message);
                throw new DatabaseException("Retrieve channel has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in retrieveChannelObjectFromStatement. The message is: {0}, " +
                    "and the stack trace: {1}.", ex.Message, ex.StackTrace);
                throw new DatabaseException("Retrieve channel has failed.");
            }
            
            return channel;
        }

    }
}
