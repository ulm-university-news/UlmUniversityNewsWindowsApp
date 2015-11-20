using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace DataHandlingLayer.Database
{
    public class DatabaseManager
    {
        /// <summary>
        /// Öffnet eine Verbindung zur Datenbank und gibt diese zurück.
        /// </summary>
        /// <returns>Ein SQLiteConnection Objekt.</returns>
        public static SQLiteConnection GetConnection()
        {
            string sqliteFilename = "UlmUniversityNews.db";
            string path = Path.Combine(ApplicationData.Current.LocalFolder.Path, sqliteFilename);

            // Erzeuge die Connection.
            var conn = new SQLiteConnection(path);
            return conn;
        }

        /// <summary>
        /// Erstelle die erforderlichen Tabellen, falls sie noch nicht vorhanden sind. 
        /// </summary>
        public static void LoadDatabase()
        {
            Debug.WriteLine("Start loading the SQLite database.");
            try
            {
                SQLiteConnection conn = GetConnection();

                // Erstelle LocalUser Tabelle.
                createLocalUserTable(conn);

                // Schalte Foreign-Key Constraints ein.
                string sql = @"PRAGMA foreign_keys = ON";
                using (var statement = conn.Prepare(sql))
                {
                    statement.Step();
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine("Failed to load database.");
                Debug.WriteLine("Exception e: " + e.Message + " and HResult: " + e.HResult + "source: " + e.Source + " stack trace: " + e.StackTrace);
            }
            
            Debug.WriteLine("Finished loading the SQLite database.");
        }

        /// <summary>
        /// Erstellt die Tabelle LocalUser.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createLocalUserTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            LocalUser   (Id                 INTEGER NOT NULL,
                                        Name                VARCHAR(35) NOT NULL,
                                        ServerAccessToken   VARCHAR(56),
                                        PushAccessToken     VARCHAR(1024),
                                        Platform            INTEGER,
                                        PRIMARY KEY(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle User.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createUserTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            User    (Id     INTEGER NOT NULL,
                                    Name    TEXT NOT NULL,
                                    OldName TEXT,
                                    PRIMARY KEY(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Moderator.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createModeratorTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Moderator   (Id         INTEGER NOT NULL,
                                        FirstName   TEXT NOT NULL,
                                        LastName    TEXT NOT NULL,
                                        Email       TEXT NOT NULL,
                                        PRIMARY KEY(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Channel.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createChannelTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Channel     (Id                 INTEGER NOT NULL,
                                        Name                TEXT NOT NULL,
                                        Description         TEXT,
                                        CreationDate        INTEGER NOT NULL,
                                        ModificationDate    INTEGER NOT NULL,
                                        Type                INTEGER NOT NULL,
                                        Term                TEXT,
                                        Location            TEXT,
                                        Dates               TEXT,
                                        Contact             TEXT,
                                        Website             TEXT,
                                        PRIMARY KEY(Id)

                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle SubscribedChannel.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createSubscribedChannelsTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            SubscribedChannels  (Id         INTEGER NOT NULL,
                                                Channel_Id  INTEGER NOT NULL,
                                                PRIMARY KEY(Id, Channel_Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Lecture.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createLectureTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Lecture (Channel_Id     INTEGER NOT NULL,
                                    Faculty         INTEGER NOT NULL,
                                    StartDate       TEXT,
                                    EndDate         TEXT,
                                    Lecturer        TEXT,
                                    Assistant       TEXT,
                                    PRIMARY KEY(Channel_Id),
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Event.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createEventTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Event   (Channel_Id INTEGER NOT NULL,
                                    Cost        TEXT,
                                    Organizer   TEXT,
                                    PRIMARY KEY(Channel_Id),
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Sports. 
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createSportsTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Sports  (Channel_Id             INTEGER NOT NULL,
                                    Cost                    TEXT,
                                    NumberOfParticipants    TEXT,
                                    PRIMARY KEY(Channel_Id),
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }
    }
}
