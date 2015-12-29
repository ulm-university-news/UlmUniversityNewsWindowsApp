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

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Starte eine Transaktion.
                using (var statement = conn.Prepare("BEGIN TRANSACTION"))
                {
                    statement.Step();
                }

                // Speichere Kanaldaten.
                using (var insertStmt = conn.Prepare("INSERT INTO Channel (Id, Name, Description, " + 
                    "CreationDate, ModificationDate, Type, Term, Location, Dates, Contact, Website, Deleted) " + 
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);"))
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
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in StoreChannel. Exception message is: {0}.", sqlEx.Message);
                // Rollback der Transaktion.
                using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                {
                    statement.Step();
                }

                throw new DatabaseException("Storing channel data in database has failed.");
            }
            catch(Exception ex)
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
                Debug.WriteLine("No valid channel object passed to the StoreChannel method.");
                return;
            }

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Aktualisiere Daten in Kanal-Tabelle.
                using (var updateChannelStmt = conn.Prepare("UPDATE Channel SET Name=?, Description=?, " + 
                    "CreationDate=?, ModificationDate=?, Type=?, Term=?, Location=?, Dates=?, Contact=?, Website=?, Deleted=? " + 
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
                    updateChannelStmt.Bind(12, channel.Id);

                    updateChannelStmt.Step();
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in UpdateChannel. The message is: {0}." + sqlEx.Message);
                throw new DatabaseException("Update of the channel with id " + channel.Id + " has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in UpdateChannel. The message is: {0}, " + 
                    "and the stack trace is: {1}.", ex.Message, ex.StackTrace);
                throw new DatabaseException("Update of channel has failed.");
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

            SQLiteConnection conn = DatabaseManager.GetConnection();
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
                    "Type=?, Term=?, Location=?, Dates=?, Contact=?, Website=?, Deleted=? WHERE Id=?;"))
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
                    updateChannelStmt.Bind(12, channel.Id);

                    updateChannelStmt.Step();
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
                        using(var updateSportsStmt = conn.Prepare("UPDATE Sports SET Cost=?, NumberOfParticipants=? " +
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
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in UpdateChannelWithSubclass. The message is: {0}." + sqlEx.Message);
                // Rollback der Transaktion.
                using (var statement = conn.Prepare("ROLLBACK TRANSACTION"))
                {
                    statement.Step();
                }
                throw new DatabaseException("Update of the channel with id " + channel.Id + " has failed.");
            }
            catch(Exception ex)
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

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Frage zunächst Daten der Tabelle Kanal ab.
                using (var getChannelsStmt = conn.Prepare("SELECT * FROM Channel;"))
                {
                    // Iteriere über Ergebnisse.
                    while(getChannelsStmt.Step() == SQLiteResult.ROW)
                    {
                        Channel channelTmp = retrieveChannelObjectFromStatement(conn, getChannelsStmt);
                        channels.Add(channelTmp);
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in GetChannels. The message is: {0}." + sqlEx.Message);
                throw new DatabaseException("Get channels has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("SQLiteException has occurred in GetChannels. The message is: {0}, " + 
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Get channels has failed.");
            }

            Debug.WriteLine("Return a channel list with {0} elements." + channels.Count);
            return channels;
        }

        /// <summary>
        /// Rufe den Kanal mit der angegebenen Id aus der Datenbank ab.
        /// </summary>
        /// <param name="channelId">Die Id des Kanals, der abgerufen werden soll.</param>
        /// <returns>Eine Instanz der Klasse Channel. Kann null liefer, wenn der Kanal nicht in der Datenbank existiert.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Abrufen des Kanals fehlschlägt.</exception>
        public Channel GetChannel(int channelId)
        {
            Channel channel = null;

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Frage Daten aus Kanal-Tabelle ab.
                using (var getChannelStmt = conn.Prepare("SELECT * FROM Channel WHERE Id=?;"))
                {
                    getChannelStmt.Bind(1, channelId);

                    // Wurde ein Eintrag zurückgeliefert?
                    if(getChannelStmt.Step() == SQLiteResult.ROW)
                    {
                        channel = retrieveChannelObjectFromStatement(conn, getChannelStmt);
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in GetChannel. The message is: {0}." + sqlEx.Message);
                throw new DatabaseException("Get channel has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("SQLiteException has occurred in GetChannel. The message is: {0}, " + 
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Get channel has failed.");
            }

            return channel;
        }

        /// <summary>
        /// Hole alle Kanäle aus der Datenbank, die der lokale Nutzer abonniert hat.
        /// </summary>
        /// <returns>Eine Liste von Objekten vom Typ Kanal oder vom Typ einer der Subklassen von Kanal. Die Liste kann auch leer sein.</returns>
        /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Abrufen der abonnierten Kanäle fehlschlägt.</exception>
        public List<Channel> GetSubscribedChannels()
        {
            List<Channel> channels = new List<Channel>();

            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Frage alle Kanäle ab, die in SubscribedChannels eingetragen sind.
                using(var getSubscribedChannelsStmt = conn.Prepare("SELECT * FROM Channel WHERE Id IN (" +
                    "SELECT Channel_Id AS Id FROM SubscribedChannels);"))
                {
                    // Iteriere über Ergebnisse.
                    while (getSubscribedChannelsStmt.Step() == SQLiteResult.ROW)
                    {
                        Channel channelTmp = retrieveChannelObjectFromStatement(conn, getSubscribedChannelsStmt);
                        channels.Add(channelTmp);
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in GetSubscribedChannels. The message is: {0}." + sqlEx.Message);
                throw new DatabaseException("Get subscribed channels has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in GetSubscribedChannels. The message is: {0}, " + 
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Get subscribed channel has failed.");
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
            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                using (var subscribeStmt = conn.Prepare("INSERT INTO SubscribedChannels (Channel_Id) VALUES (?);"))
                {
                    subscribeStmt.Bind(1, channelId);

                    subscribeStmt.Step();
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLiteException has occurred in SubscribeChannel. The message is: {0}." + sqlEx.Message);
                throw new DatabaseException("Subscribe channel has failed.");
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in GetSubscribedChannels. The message is: {0}, " + 
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Subscribe channel has failed.");
            }
        }

        /// <summary>
        /// Liefert das Datum zurück, an dem zum letzten Mal ein Update der Liste aller in
        /// der Anwendung verwalteten Kanäle durchgeführt wurde.
        /// </summary>
        /// <returns>Ein Objekt vom Typ DateTime.</returns>
        public DateTime GetDateOfLastChannelListUpdate()
        {
            DateTime lastUpdate = DateTime.MinValue;
            SQLiteConnection conn = DatabaseManager.GetConnection();
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
            catch (Exception ex)
            {
                Debug.WriteLine("Exception has occurred in GetDateOfLastChannelListUpdate. The message is: {0}, " +
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
            }
            return lastUpdate;
        }

        /// <summary>
        /// Setzt das Datum der letzten Aktualisierung der Kanäle, die in der Anwendung verwaltet werden.
        /// </summary>
        /// <param name="lastUpdate">Das Datum der letzten Änderung in Form eines DateTime Objekts.</param>
        /// /// <exception cref="DatabaseException">Wirft DatabaseException, wenn das Setzen des letzten Änderungsdatums fehlschlägt.</exception>
        public void SetDateOfLastChannelListUpdate(DateTime lastUpdate)
        {
            SQLiteConnection conn = DatabaseManager.GetConnection();
            try
            {
                // Frage zunächst ab, ob es schon ein Änderungsdatum in der Tabelle gibt.
                DateTime tableEntry = GetDateOfLastChannelListUpdate();
                if(tableEntry == DateTime.MinValue)
                {
                    // Noch kein Eintrag in Tabelle, füge also einen ein.
                    using(var statement = conn.Prepare(@"INSERT INTO (Id, LastUpdate) VALUES (?,?);"))
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
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in SetDateOfLastChannelListUpdate. The message is: {0}, " +
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Setting the date of the last update on the channel list has failed.");
            }
        }

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
                DateTime creationDate, modificationDate;

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
            }
            catch(Exception ex)
            {
                Debug.WriteLine("Exception has occurred in retrieveChannelObjectFromStatement. The message is: {0}, " +
                    "and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Retrieve channel has failed.");
            }
            
            return channel;
        }



    }
}
