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

                // Speichere Kanal Parameter.
                using (var insertStmt = conn.Prepare("INSERT INTO Channel (Id, Name, Description, CreationDate, ModificationDate," +
                    " Type, Term, Location, Dates, Contact, Website) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);"))
                {
                    insertStmt.Bind(1, channel.Id);
                    insertStmt.Bind(2, channel.Name);
                    insertStmt.Bind(3, channel.Description);
                    insertStmt.Bind(4, channel.CreationDate);
                    insertStmt.Bind(5, channel.ModificationDate);
                    insertStmt.Bind(6, (int)channel.Type);
                    insertStmt.Bind(7, channel.Term);
                    insertStmt.Bind(8, channel.Locations);
                    insertStmt.Bind(9, channel.Dates);
                    insertStmt.Bind(10, channel.Contacts);
                    insertStmt.Bind(11, channel.Website);

                    insertStmt.Step();
                }

                // Speichere Subklassen Parameter abhängig vom Typ des Kanals.
                switch (channel.Type)
                {
                    case DataModel.Enums.ChannelType.LECTURE:
                        Lecture lecture = (Lecture)channel;

                        using (var insertLectureStmt = conn.Prepare("INSERT INTO Lecture (Channel_Id, Faculty, StartDate," +
                            " EndDate, Lecturer, Assistant) VALUES (?, ?, ?, ?, ?, ?);"))
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
                Debug.WriteLine("Exception has occurred in StoreChannel. Exception message is: {0}, and stack trace is {1}.", ex.Message, ex.StackTrace);
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
                using (var updateChannelStmt = conn.Prepare("UPDATE Channel SET Name=?, Description=?, CreationDate=?, ModificationDate=?, " + 
                    "Type=?, Term=?, Location=?, Dates=?, Contact=?, Website=? WHERE Id=?;"))
                {
                    updateChannelStmt.Bind(1, channel.Name);
                    updateChannelStmt.Bind(2, channel.Description);
                    updateChannelStmt.Bind(3, channel.CreationDate);
                    updateChannelStmt.Bind(4, channel.ModificationDate);
                    updateChannelStmt.Bind(5, (int)channel.Type);
                    updateChannelStmt.Bind(6, channel.Term);
                    updateChannelStmt.Bind(7, channel.Locations);
                    updateChannelStmt.Bind(8, channel.Dates);
                    updateChannelStmt.Bind(9, channel.Contacts);
                    updateChannelStmt.Bind(10, channel.Website);
                    updateChannelStmt.Bind(11, channel.Id);

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
                Debug.WriteLine("Exception has occurred in UpdateChannel. The message is: {0}, and the stack trace is: {1}.", ex.Message, ex.StackTrace);
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

                using (var updateChannelStmt = conn.Prepare("UPDATE Channel SET Name=?, Description=?, CreationDate=?, ModificationDate=?, " +
                    "Type=?, Term=?, Location=?, Dates=?, Contact=?, Website=? WHERE Id=?;"))
                {
                    updateChannelStmt.Bind(1, channel.Name);
                    updateChannelStmt.Bind(2, channel.Description);
                    updateChannelStmt.Bind(3, channel.CreationDate);
                    updateChannelStmt.Bind(4, channel.ModificationDate);
                    updateChannelStmt.Bind(5, (int)channel.Type);
                    updateChannelStmt.Bind(6, channel.Term);
                    updateChannelStmt.Bind(7, channel.Locations);
                    updateChannelStmt.Bind(8, channel.Dates);
                    updateChannelStmt.Bind(9, channel.Contacts);
                    updateChannelStmt.Bind(10, channel.Website);
                    updateChannelStmt.Bind(11, channel.Id);

                    updateChannelStmt.Step();
                }

                // Aktualisiere auch Subklassen-Tabelle abhängig vom Typ des Kanals.
                switch (channel.Type)
                {
                    case DataModel.Enums.ChannelType.LECTURE:
                        Lecture lecture = (Lecture)channel;

                        using (var updateLectureStmt = conn.Prepare("UPDATE Lecture SET StartDate=?, EndDate=?, " + 
                            "Lecturer=?, Assistant=? WHERE Channel_Id=?;"))
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

                        using (var updateEventStmt = conn.Prepare("UPDATE Event SET Cost=?, Organizer=? WHERE Channel_Id=?;"))
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

                        using(var updateSportsStmt = conn.Prepare("UPDATE Sports SET Cost=?, NumberOfParticipants=? WHERE Channel_Id=?;"))
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
                Debug.WriteLine("Exception has occurred in UpdateChannelWithSubclass. The message is: {0}, and the stack trace is: {1}.", ex.Message, ex.StackTrace);
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
                // Initialisierung der Variablen.
                int id;
                string name, description, term, location, dates, contact, website, startDate, endDate, lecturer,
                    assistant, cost, organizer, participants;
                ChannelType type;
                Faculty faculty;
                DateTime creationDate, modificationDate;

                // Frage zunächst Daten der Tabelle Kanal ab.
                using (var getChannelsStmt = conn.Prepare("SELECT * FROM Channel;"))
                {
                    // Iteriere über Ergebnisse.
                    while(getChannelsStmt.Step() == SQLiteResult.ROW)
                    {
                        id = Convert.ToInt32(getChannelsStmt["Id"]);
                        name = (string)getChannelsStmt["Name"];
                        description = (string)getChannelsStmt["Description"];
                        type = (ChannelType)Enum.ToObject(typeof(ChannelType), getChannelsStmt["Type"]);
                        creationDate = (DateTime)getChannelsStmt["CreationDate"];       // TODO test
                        modificationDate = (DateTime)getChannelsStmt["ModificationDate"];   // TODO test
                        term = (string)getChannelsStmt["Term"];
                        location = (string)getChannelsStmt["Location"];
                        dates = (string)getChannelsStmt["Dates"];
                        contact = (string)getChannelsStmt["Contact"];
                        website = (string)getChannelsStmt["Website"];

                        // Falls notwendig, hole Daten aus Tabelle der Subklasse.
                        switch(type)
                        {
                            case ChannelType.LECTURE:
                                using (var getLectureStmt = conn.Prepare("SELECT * FROM Lecture WHERE Channel_Id=?;"))
                                {
                                    getLectureStmt.Bind(1, id);

                                    // Hole Ergebnis der Query.
                                    if(getLectureStmt.Step() == SQLiteResult.ROW)
                                    {
                                        faculty = (Faculty)Enum.ToObject(typeof(Faculty), getLectureStmt["Faculty"]);
                                        startDate = (string)getLectureStmt["StartDate"];
                                        endDate = (string)getLectureStmt["EndDate"];
                                        lecturer = (string)getLectureStmt["Lecturer"];
                                        assistant = (string)getLectureStmt["Assistant"];

                                        // Erstelle Lecture Objekt und füge es der Liste hinzu.
                                        Lecture lecture = new Lecture(id, name, description, type, creationDate, modificationDate, term, location,
                                            dates, contact, website, faculty, startDate, endDate, lecturer, assistant);
                                        channels.Add(lecture);
                                    }
                                }
                                break;
                            case ChannelType.EVENT:
                                using (var getEventStmt = conn.Prepare("SELECT * FROM Event WHERE Channel_Id=?;"))
                                {
                                    getEventStmt.Bind(1, id);

                                    // Hole Ergebnis der Query.
                                    if(getEventStmt.Step() == SQLiteResult.ROW)
                                    {
                                        cost = (string)getEventStmt["Cost"];
                                        organizer = (string)getEventStmt["Organizer"];

                                        // Erstelle Event Objekt und füge es der Liste hinzu.
                                        Event eventObj = new Event(id, name, description, type, creationDate, modificationDate, term, location, 
                                            dates, contact, website, cost, organizer);
                                        channels.Add(eventObj);
                                    }
                                }
                                break;
                            case ChannelType.SPORTS:
                                using(var getSportsStmt = conn.Prepare("SELECT * FROM Sports WHERE Channel_Id=?;"))
                                {
                                    getSportsStmt.Bind(1, id);

                                    // Hole Ergebnis der Query.
                                    if(getSportsStmt.Step() == SQLiteResult.ROW)
                                    {
                                        cost = (string)getSportsStmt["Cost"];
                                        participants = (string)getSportsStmt["NumberOfParticipants"];

                                        // Erstelle Sports Objekt und füge es der Liste hinzu.
                                        Sports sportsObj = new Sports(id, name, description, type, creationDate, modificationDate, term, location, 
                                            dates, contact, website, cost, participants);
                                        channels.Add(sportsObj);
                                    }
                                }
                                break;
                            default:
                                // Keine Subklasse, also erzeuge Kanal Objekt.
                                Channel channel = new Channel(id, name, description, type, creationDate, modificationDate, term, location,
                                    dates, contact, website);
                                channels.Add(channel);
                                break;
                        }
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
                Debug.WriteLine("SQLiteException has occurred in GetChannels. The message is: {0}, and the stack trace: {1}." + ex.Message, ex.StackTrace);
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
                // Initialisierung der Variablen.
                int id;
                string name, description, term, location, dates, contact, website, startDate, endDate, lecturer,
                    assistant, cost, organizer, participants;
                ChannelType type;
                Faculty faculty;
                DateTime creationDate, modificationDate;

                // Frage Daten aus Kanal-Tabelle ab.
                using (var getChannelStmt = conn.Prepare("SELECT * FROM Channel WHERE Id=?;"))
                {
                    getChannelStmt.Bind(1, channelId);

                    if(getChannelStmt.Step() == SQLiteResult.ROW)
                    {
                        id = Convert.ToInt32(getChannelStmt["Id"]);
                        name = (string)getChannelStmt["Name"];
                        description = (string)getChannelStmt["Description"];
                        type = (ChannelType)Enum.ToObject(typeof(ChannelType), getChannelStmt["Type"]);
                        creationDate = (DateTime)getChannelStmt["CreationDate"];       // TODO test
                        modificationDate = (DateTime)getChannelStmt["ModificationDate"];   // TODO test
                        term = (string)getChannelStmt["Term"];
                        location = (string)getChannelStmt["Location"];
                        dates = (string)getChannelStmt["Dates"];
                        contact = (string)getChannelStmt["Contact"];
                        website = (string)getChannelStmt["Website"];

                        // Falls notwenig, hole Daten aus den Tabllen der Subklasse.
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

                                        // Erstelle Lecture Objekt.
                                        Lecture lecture = new Lecture(id, name, description, type, creationDate, modificationDate, term, location,
                                            dates, contact, website, faculty, startDate, endDate, lecturer, assistant);
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

                                        // Erstelle Event Objekt.
                                        Event eventObj = new Event(id, name, description, type, creationDate, modificationDate, term, location,
                                            dates, contact, website, cost, organizer);
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
                                            dates, contact, website, cost, participants);
                                        channel = sportsObj;
                                    }
                                }
                                break;
                            default:
                                // Keine Subklasse, also erzeuge Kanal Objekt.
                                channel = new Channel(id, name, description, type, creationDate, modificationDate, term, location,
                                    dates, contact, website);
                                break;
                        }
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
                Debug.WriteLine("SQLiteException has occurred in GetChannel. The message is: {0}, and the stack trace: {1}." + ex.Message, ex.StackTrace);
                throw new DatabaseException("Get channel has failed.");
            }

            return channel;
        }

    }
}
