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
        /// Wandelt eine Zeitangabe im DateTime Format um in einen String, um sie in einer
        /// SQLite Datenbank zu speichern.
        /// </summary>
        /// <param name="datetime">Das umzuwandelnde Datum.</param>
        /// <returns>Das Ergebnis der Umwandlung als String.</returns>
        public static string DateTimeToSQLite(DateTime datetime)
        {
            string dateTimeFormat = "{0}-{1}-{2} {3}:{4}:{5}.{6}";
            return string.Format(dateTimeFormat, datetime.Year, datetime.Month, datetime.Day,
                datetime.Hour, datetime.Minute, datetime.Second, datetime.Millisecond);
        }

        /// <summary>
        /// Wandelt einen String aus der SQLite Datenbank um in ein Objekt vom Typ DateTime.
        /// </summary>
        /// <param name="datetime">Der umzuwandelnde String.</param>
        /// <returns>Objekt vom Typ DateTime.</returns>
        public static DateTime DateTimeFromSQLite(string datetime)
        {
            return DateTime.Parse(datetime);
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

                // Erstelle User Tabelle.
                createUserTable(conn);

                // Erstelle Moderator Tabelle.
                createModeratorTable(conn);

                // Erstelle Channel Tabelle und zugehörige Sub-Tabellen.
                createChannelTable(conn);
                createLectureTable(conn);
                createEventTable(conn);
                createSportsTable(conn);
                createSubscribedChannelsTable(conn);
                createModerartorChannelTable(conn);
                createLastUpdateOnChannelsListTable(conn);

                // Erstelle Group Tabelle und zugehörige Sub-Tabellen.
                createGroupTable(conn);
                createBallotTable(conn);
                createOptionTable(conn);
                createUserOptionTable(conn);
                createUserGroupTable(conn);

                // Erstelle Conversation Tabelle.
                createConversationTable(conn);

                // Erstelle Message Tabelle und zugehörige Sub-Tabellen.
                createMessageTable(conn);
                createAnnouncementTable(conn);
                createConversationMessageTable(conn);

                // Erstelle Reminder Tabelle.
                createReminderTable(conn);

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
        /// Löscht das Datenbank Schema und alle zugehörigen Daten. Anschließend wird das Datenbank-Schema neu erstellt, so dass Änderungen am Datenbank Schema übernommen werden. 
        /// </summary>
        public static void UpgradeDatabase()
        {
            Debug.WriteLine("Start upgrading the database. This will remove all data and recreate the database schema.");
            try
            {
                SQLiteConnection conn = GetConnection();

                // Schalte Foreign-Key Constraints aus.
                string sqlFK = @"PRAGMA foreign_keys = OFF";
                using (var statement = conn.Prepare(sqlFK))
                {
                    statement.Step();
                }

                string[] tableNames = { "User", "LocalUser", "Moderator", "Channel", "Lecture", "Event", "Sports", "SubscribedChannels", "ModeratorChannel",
                                          "Group", "UserGroup", "Ballot", "Option", "UserOption", "Message", "Conversation", "ConversationMessage", "Announcement", "Reminder", "LastUpdateOnChannelsList"};
                for (int i = 0; i < tableNames.Length; i++)
                {
                    // Drop tables.
                    string sql = "DROP TABLE IF EXISTS \"" + tableNames[i] + "\";";
                    using (var statement = conn.Prepare(sql))
                    {
                        statement.Step();
                    }
                }

                // Foreign Key Constraints werden bei der Erstellung der Datenbank Tabellen wieder eingeschaltet.
                // Recreate the database scheme.
                LoadDatabase();                              
            } 
            catch(Exception e)
            {
                Debug.WriteLine("Failed to upgrade database.");
                Debug.WriteLine("Exception e: " + e.Message + " and HResult: " + e.HResult + "source: " + e.Source + " stack trace: " + e.StackTrace);
            }
            Debug.WriteLine("Finished upgrading the database.");
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
                                        CreationDate        DATETIME NOT NULL,
                                        ModificationDate    DATETIME NOT NULL,
                                        Type                INTEGER NOT NULL,
                                        Term                TEXT,
                                        Location            TEXT,
                                        Dates               TEXT,
                                        Contact             TEXT,
                                        Website             TEXT,
                                        Deleted             BOOLEAN,
                                        PRIMARY KEY(Id)

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
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE
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
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE
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
                                    FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle LastUpdateOnChannelsList.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createLastUpdateOnChannelsListTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            LastUpdateOnChannelsList  ( Id   INTEGER PRIMARY KEY,
                                                        LastUpdate DATETIME
                         );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle ModeratorChannel.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createModerartorChannelTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            ModeratorChannel (Channel_Id    INTEGER NOT NULL,
                                              Moderator_Id  INTEGER NOT NULL,
                                              Active        INTEGER NOT NULL,
                                              PRIMARY KEY(Channel_Id, Moderator_Id),
                                              FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE,
                                              FOREIGN KEY(Moderator_Id) REFERENCES Moderator(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle SubscribedChannels.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createSubscribedChannelsTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            SubscribedChannels (Channel_Id  INTEGER NOT NULL,
                                                PRIMARY KEY(Channel_Id),
                                                FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Group.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createGroupTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            ""Group""   (Id                 INTEGER NOT NULL,
                                        Name                TEXT NOT NULL,
                                        Description         TEXT,
                                        Type                INTEGER NOT NULL,
                                        CreationDate        DATETIME NOT NULL,
                                        ModificationDate    DATETIME NOT NULL,
                                        Term                TEXT,
                                        Deleted             INTEGER NOT NULL,
                                        GroupAdmin_User_Id  INTEGER NOT NULL,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(GroupAdmin_User_Id) REFERENCES User(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle UserGroup.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createUserGroupTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            UserGroup   (Group_Id       INTEGER NOT NULL,
                                        User_Id         INTEGER NOT NULL,
                                        Active          INTEGER NOT NULL,
                                        PRIMARY KEY(Group_Id, User_Id),
                                        FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id),
                                        FOREIGN KEY(User_Id) REFERENCES User(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Ballot.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createBallotTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Ballot  (Id                 INTEGER NOT NULL,
                                    Title               TEXT NOT NULL,
                                    Description         TEXT,
                                    Closed              INTEGER NOT NULL,
                                    MultipleChoice      INTEGER NOT NULL,
                                    Public              INTEGER NOT NULL,
                                    Group_Id            INTEGER NOT NULL,
                                    BallotAdmin_User_Id INTEGER,
                                    PRIMARY KEY(Id),
                                    FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id),
                                    FOREIGN KEY(BallotAdmin_User_Id) REFERENCES User(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Option.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createOptionTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            ""Option""  (Id         INTEGER NOT NULL,
                                        Text        TEXT NOT NULL,
                                        Ballot_Id   INTEGER NOT NULL,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(Ballot_Id) REFERENCES Ballot(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle UserOption.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createUserOptionTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            UserOption  (Option_Id  INTEGER NOT NULL,
                                        User_Id     INTEGER NOT NULL,
                                        PRIMARY KEY(Option_Id, User_Id),
                                        FOREIGN KEY(Option_Id) REFERENCES ""Option""(Id),
                                        FOREIGN KEY(User_Id) REFERENCES User(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Message.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createMessageTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Message (Id             INTEGER NOT NULL,
                                    Text            TEXT    NOT NULL,
                                    CreationDate    DATETIME NOT NULL,
                                    Priority        INTEGER NOT NULL,
                                    Read            INTEGER NOT NULL,
                                    PRIMARY KEY(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Announcement.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createAnnouncementTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Announcement    (MessageNumber      INTEGER NOT NULL,
                                            Channel_Id          INTEGER NOT NULL,
                                            Title               TEXT NOT NULL,
                                            Author_Moderator_Id INTEGER NOT NULL,
                                            Message_Id          INTEGER NOT NULL,
                                            PRIMARY KEY(MessageNumber, Channel_Id),
                                            FOREIGN KEY(Channel_Id) REFERENCES Channel(Id),
                                            FOREIGN KEY(Author_Moderator_Id) REFERENCES Moderator(Id),
                                            FOREIGN KEY(Message_Id) REFERENCES Message(Id)
                            );";
            using (var statment = conn.Prepare(sql))
            {
                statment.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Conversation.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createConversationTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Conversation    (Id                         INTEGER NOT NULL,
                                            Title                       TEXT    NOT NULL,
                                            Closed                      INTEGER NOT NULL,
                                            Group_Id                    INTEGER NOT NULL,
                                            ConversationAdmin_User_Id   INTEGER NOT NULL,
                                            PRIMARY KEY(Id),
                                            FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id),
                                            FOREIGN KEY(ConversationAdmin_User_Id) REFERENCES User(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle ConversationMessage.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createConversationMessageTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            ConversationMessage (MessageNumber          INTEGER NOT NULL,
                                                Conversation_Id         INTEGER NOT NULL,
                                                Author_User_Id          INTEGER NOT NULL,
                                                Message_Id              INTEGER NOT NULL,
                                                PRIMARY KEY(MessageNumber, Conversation_Id),
                                                FOREIGN KEY(Conversation_Id) REFERENCES Conversation(Id),
                                                FOREIGN KEY(Author_User_Id) REFERENCES User(Id),
                                                FOREIGN KEY(Message_Id) REFERENCES Message(Id)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Reminder.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void createReminderTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Reminder    (Id                 INTEGER NOT NULL,
                                        Channel_Id          INTEGER NOT NULL,
                                        StartDate           DATETIME NOT NULL,
                                        EndDate             DATETIME NOT NULL,    
                                        CreationDate        DATETIME NOT NULL,
                                        ModificationDate    DATETIME NOT NULL,
                                        ""Interval""            INTEGER,
                                        ""Ignore""              INTEGER,
                                        Title               TEXT NOT NULL,
                                        Text                TEXT NOT NULL,
                                        Priority            INTEGER NOT NULL,
                                        Author_Moderator_Id INTEGER NOT NULL,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(Channel_Id) REFERENCES Channel(Id),
                                        FOREIGN KEY(Author_Moderator_Id) REFERENCES Moderator(Id)                                        
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }
    }
}
