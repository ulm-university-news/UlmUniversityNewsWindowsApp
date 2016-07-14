using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;

namespace DataHandlingLayer.Database
{
    public class DatabaseManager
    {
        #region Fields
        /// <summary>
        /// Ein Mutex Objekt, welches verwendet werden kann, um den Zugriff auf die SQLite Datenbank zu synchronisieren.
        /// Paralleler Zugriff auf die Datenbank kann dadurch gesteuert werden.
        /// </summary>
        private static Mutex databaseAccessControlMutex;
        /// <summary>
        /// Der Name des Mutex Objekts. Über diesen Namen wird das Objekt eindeutig identifiziert und kann von verschiedenen
        /// Prozessen aus abgerufen werden.
        /// </summary>
        private static string accessControlMutexName = "DatabaseAccessControlMutex";
        /// <summary>
        /// Gibt die maximale Zeit in Millisekunden an, die ein Thread auf den Zugriff auf den gesicherten Bereich
        /// warten soll, bevor mit einem Timeout abgebrochen wird.
        /// </summary>
        public static int MutexTimeoutValue = 8000;
        #endregion Fields

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

            // Schalte Foreign-Key Constraints ein.
            string sql = @"PRAGMA foreign_keys = ON";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }

            return conn;
        }

        /// <summary>
        /// Die Methode ruft das Mutex Objekt zur Steuerung des Zugriffs auf die Datenbank ab.
        /// </summary>
        /// <returns>Liefert das Mutex Objekt zurück.</returns>
        public static Mutex GetDatabaseAccessMutexObject()
        {
            lock (typeof(DatabaseManager))
            {
                if (databaseAccessControlMutex == null)
                {
                    databaseAccessControlMutex = new Mutex(false, accessControlMutexName);
                }
            }
            return databaseAccessControlMutex;
        }

        /// <summary>
        /// Wandelt eine Zeitangabe im DateTimeOffset Format um in einen String, um sie in einer
        /// SQLite Datenbank zu speichern.
        /// </summary>
        /// <param name="datetime">Das umzuwandelnde Datum mit zugehöriger Uhrzeit und Zeitzonenoffset.</param>
        /// <returns>Das Ergebnis der Umwandlung als String.</returns>
        public static string DateTimeToSQLite(DateTimeOffset datetime)
        {
            string dateTimeFormat = string.Empty;
            
            dateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzzz";
            CultureInfo cultureInfo = new CultureInfo("de-DE");
            string datetimeString = datetime.ToString(dateTimeFormat, cultureInfo);

            return datetimeString;
        }

        /// <summary>
        /// Wandelt einen String aus der SQLite Datenbank um in ein Objekt vom Typ DateTimeOffset.
        /// </summary>
        /// <param name="datetime">Der umzuwandelnde String.</param>
        /// <returns>Objekt vom Typ DateTimeOffset.</returns>
        public static DateTimeOffset DateTimeFromSQLite(string datetime)
        {
            CultureInfo cultureInfo = new CultureInfo("de-DE");
            string dateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffzzzz";
            DateTimeOffset dateTimeObject = DateTimeOffset.ParseExact(datetime, dateTimeFormat, cultureInfo);

            return dateTimeObject;
        }

        /// <summary>
        /// Erstelle die erforderlichen Tabellen, falls sie noch nicht vorhanden sind. 
        /// </summary>
        public static void LoadDatabase()
        {
            Debug.WriteLine("Start loading the SQLite database.");
            // Rufe das Mutex Objekt ab.
            Mutex mutex = GetDatabaseAccessMutexObject();

            // Frage Zugriff auf Datenbank an.
            if (mutex.WaitOne())
            {
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

                    // Füge einen Dummy-Moderator ein, um Announcements mit fehlerhaften Referenz auf einen Autor
                    // im Notfall auf diesen Moderator abbilden zu können.
                    addDummyModerator(conn);
                    // Füge Dummy-Nutzer ein.
                    addDummyUser(conn);

                    // Erstelle Settings Tabellen.
                    createOrderOptionsTable(conn);
                    createLanguageSettings(conn);
                    createNotificationSettings(conn);
                    
                    createSettingsTable(conn);

                    // Fülle Tabellen, falls noch keine Werte eingetragen wurden, und setze die Defaultwerte.
                    fillOrderOptionsTable(conn);
                    fillNotificationSettings(conn);
                    fillLanguageSettings(conn);
                    setDefaultSettings(conn);

                    conn.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to load database.");
                    Debug.WriteLine("Exception e: " + e.Message + " and HResult: " + e.HResult + "source: " + e.Source + " stack trace: " + e.StackTrace);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
                        
            Debug.WriteLine("Finished loading the SQLite database.");
        }

        /// <summary>
        /// Löscht das Datenbank Schema und alle zugehörigen Daten. Anschließend wird das Datenbank-Schema neu erstellt, so dass Änderungen am Datenbank Schema übernommen werden. 
        /// </summary>
        public static void UpgradeDatabase()
        {
            Debug.WriteLine("Start upgrading the database. This will remove all data and recreate the database schema.");
            Mutex mutex = GetDatabaseAccessMutexObject();
            if (mutex.WaitOne(4000))
            {
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
                                          "Group", "UserGroup", "Ballot", "Option", "UserOption", "Message", "Conversation", "ConversationMessage", "Announcement", "Reminder", "LastUpdateOnChannelsList",
                                          "Settings", "NotificationSettings", "OrderOptions", "LanguageSettings"};
                    for (int i = 0; i < tableNames.Length; i++)
                    {
                        // Drop tables.
                        string sql = "DROP TABLE IF EXISTS \"" + tableNames[i] + "\";";
                        using (var statement = conn.Prepare(sql))
                        {
                            statement.Step();
                        }
                    }

                    conn.Dispose();
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Failed to upgrade database.");
                    Debug.WriteLine("Exception e: " + e.Message + " and HResult: " + e.HResult + "source: " + e.Source + " stack trace: " + e.StackTrace);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            
            // Recreate the database scheme.
            LoadDatabase();      
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
                                        DeletionNoticedFlag INTEGER DEFAULT 0,     
                                        NotificationSettings_NotifierId INTEGER, 
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(NotificationSettings_NotifierId) REFERENCES NotificationSettings(NotifierId)
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
                                        NotificationSettings_NotifierId INTEGER,
                                        IsDirty             INTEGER,
                                        HasNewEvent         INTEGER DEFAULT 0,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(GroupAdmin_User_Id) REFERENCES User(Id),
                                        FOREIGN KEY(NotificationSettings_NotifierId) REFERENCES NotificationSettings(NotifierId)
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
                                        FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id) ON DELETE CASCADE,
                                        FOREIGN KEY(User_Id) REFERENCES User(Id) ON DELETE CASCADE
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
                                    FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id) ON DELETE CASCADE,
                                    FOREIGN KEY(BallotAdmin_User_Id) REFERENCES User(Id) ON DELETE CASCADE
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
                                        FOREIGN KEY(Ballot_Id) REFERENCES Ballot(Id) ON DELETE CASCADE
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
                                        FOREIGN KEY(Option_Id) REFERENCES ""Option""(Id) ON DELETE CASCADE,
                                        FOREIGN KEY(User_Id) REFERENCES User(Id) ON DELETE CASCADE
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
                                            FOREIGN KEY(Message_Id) REFERENCES Message(Id) ON DELETE CASCADE
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
                                            FOREIGN KEY(Group_Id) REFERENCES ""Group""(Id) ON DELETE CASCADE,
                                            FOREIGN KEY(ConversationAdmin_User_Id) REFERENCES User(Id) ON DELETE CASCADE
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
                                                FOREIGN KEY(Conversation_Id) REFERENCES Conversation(Id) ON DELETE CASCADE,
                                                FOREIGN KEY(Author_User_Id) REFERENCES User(Id) ON DELETE CASCADE,
                                                FOREIGN KEY(Message_Id) REFERENCES Message(Id) ON DELETE CASCADE
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
                                        Active              INTEGER,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(Channel_Id) REFERENCES Channel(Id) ON DELETE CASCADE,
                                        FOREIGN KEY(Author_Moderator_Id) REFERENCES Moderator(Id)                                        
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle Settings.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void createSettingsTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            Settings    (Id                                           INTEGER NOT NULL,
                                        ChannelSettings_OrderOptions_OrderId          INTEGER,
                                        ConversationSettings_OrderOptions_OrderId     INTEGER,
                                        GroupSettings_OrderOptions_OrderId            INTEGER,
                                        BallotSettings_OrderOptions_OrderId           INTEGER,
                                        AnnouncementSettings_OrderOptions_OrderId     INTEGER,
                                        GeneralListOrder_OrderOptions_OrderId         INTEGER,
                                        LanguageSettings_LanguageId                   INTEGER,
                                        NotificationSettings_NotifierId               INTEGER,
                                        PRIMARY KEY(Id),
                                        FOREIGN KEY(ChannelSettings_OrderOptions_OrderId ) REFERENCES OrderOptions(OrderId),
                                        FOREIGN KEY(ConversationSettings_OrderOptions_OrderId) REFERENCES OrderOptions(OrderId),
                                        FOREIGN KEY(GroupSettings_OrderOptions_OrderId) REFERENCES OrderOptions(OrderId), 
                                        FOREIGN KEY(BallotSettings_OrderOptions_OrderId) REFERENCES OrderOptions(OrderId),
                                        FOREIGN KEY(AnnouncementSettings_OrderOptions_OrderId) REFERENCES OrderOptions(OrderId),
                                        FOREIGN KEY(GeneralListOrder_OrderOptions_OrderId) REFERENCES OrderOptions(OrderId),
                                        FOREIGN KEY(NotificationSettings_NotifierId) REFERENCES NotificationSettings(NotifierId),
                                        FOREIGN KEY(LanguageSettings_LanguageId) REFERENCES LanguageSettings(LanguageId)
                            )";
            using (var statment = conn.Prepare(sql))
            {
                statment.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle OrderOptions.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void createOrderOptionsTable(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            OrderOptions    (OrderId    INTEGER NOT NULL,
                                            Description TEXT,
                                            PRIMARY KEY(OrderId)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle LanguageSettings.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void createLanguageSettings(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            LanguageSettings    (LanguageId         INTEGER NOT NULL,
                                                Description         TEXT,
                                                PRIMARY KEY(LanguageId)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }

        /// <summary>
        /// Erstellt die Tabelle NotificationSettings.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void createNotificationSettings(SQLiteConnection conn)
        {
            string sql = @"CREATE TABLE IF NOT EXISTS 
                            NotificationSettings    (NotifierId      INTEGER NOT NULL,
                                                    Description      TEXT,
                                                    PRIMARY KEY(NotifierId)
                            );";
            using (var statement = conn.Prepare(sql))
            {
                statement.Step();
            }
        }
        
        /// <summary>
        /// Füllt die Tabelle LanguageSettings mit den von der App unterstützten Sprachen.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void fillLanguageSettings(SQLiteConnection conn){
            try{
                string checkQuery = @"SELECT COUNT(*) AS NrOfRecords FROM LanguageSettings;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Step();

                    int amountOfRecords = Convert.ToInt32(checkStmt["NrOfRecords"]);

                    if(amountOfRecords == 0)
                    {
                        // Füge die Werte ein.
                        string sql = @"INSERT INTO LanguageSettings (LanguageId, Description)
                            VALUES (?, ?);";
                        using(var statement = conn.Prepare(sql))
                        {
                            statement.Bind(1, 0);
                            statement.Bind(2, "English");       // Englisch mit Index 0.

                            statement.Step();
                            statement.Reset();

                            statement.Bind(1, 1);
                            statement.Bind(2, "German");       // Deutsch mit Index 1.

                            statement.Step();
                            Debug.WriteLine("Inserted the language settings information.");
                        }
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("Error while filling the LanguageSettings table.");
                Debug.WriteLine("The message is {0}.", sqlEx.Message);
            }
        }

        /// <summary>
        /// Füllt die Tabelle OrderOptions mit den von der App unterstützten Sortier- und Anordnungsparameter.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void fillOrderOptionsTable(SQLiteConnection conn)
        {
            try
            {
                string checkQuery = @"SELECT COUNT(*) AS NrOfRecords FROM OrderOptions;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Step();

                    int amountOfRecords = Convert.ToInt32(checkStmt["NrOfRecords"]);

                    if(amountOfRecords == 0)
                    {
                        // Füge die Werte ein.
                        string sql = @"INSERT INTO OrderOptions (OrderId, Description) 
                            VALUES  (0, 'Descending'),
                                    (1, 'Ascending'),
                                    (2, 'Sort alphabetical'),
                                    (3, 'Sort by type'),
                                    (4, 'Sort by amount of new messages'),
                                    (5, 'Sort by latest date'),
                                    (6, 'Sort by latest vote'),
                                    (7, 'Sort by type first and then by amount of new messages');";
                        using (var statement = conn.Prepare(sql))
                        {
                            statement.Step();
                            Debug.WriteLine("Inserted OrderOptions entries.");
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("Error while filling the OrderOptions table.");
                Debug.WriteLine("The message is {0}.", sqlEx.Message);
            }
        }

        /// <summary>
        /// Füllt die Tabelle NotificationSettings mit den von der App unterstützten Benachrichtigungsparametern.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void fillNotificationSettings(SQLiteConnection conn)
        {
            try
            {
                string checkQuery = @"SELECT COUNT(*) AS NrOfRecords FROM NotificationSettings;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Step();

                    int amountOfRecords = Convert.ToInt32(checkStmt["NrOfRecords"]);

                    if(amountOfRecords == 0)
                    {
                        // Füge die Werte ein.
                        string sql = @"INSERT INTO NotificationSettings (NotifierId, Description) 
                            VALUES  (0, 'Announce only messages with high priority'),
                                    (1, 'Announce all incoming messages'),
                                    (2, 'Announce no incoming message at all'),
                                    (3, 'Application default');";
                        using (var statement = conn.Prepare(sql))
                        {
                            statement.Step();
                            Debug.WriteLine("Inserted NotificationSettings entries.");
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("Error while filling the NotificationSettings table.");
                Debug.WriteLine("The message is {0}.", sqlEx.Message);
            }
        }

        /// <summary>
        /// Setze die Default-Werte für die Einstellungen.
        /// </summary>
        /// <param name="conn">Eine aktive Verbindung zur Datenbank.</param>
        private static void setDefaultSettings(SQLiteConnection conn)
        {
            try
            {
                string checkQuery = @"SELECT COUNT(*) AS NrOfRecords FROM Settings;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Step();

                    int amountOfRecords = Convert.ToInt32(checkStmt["NrOfRecords"]);

                    if (amountOfRecords == 0)
                    {
                        // Füge Default Settings-Werte ein.
                        string sql = @"INSERT INTO Settings (Id,
                                                            ChannelSettings_OrderOptions_OrderId,
                                                            ConversationSettings_OrderOptions_OrderId, 
                                                            GroupSettings_OrderOptions_OrderId,
                                                            BallotSettings_OrderOptions_OrderId,
                                                            AnnouncementSettings_OrderOptions_OrderId,
                                                            GeneralListOrder_OrderOptions_OrderId,
                                                            LanguageSettings_LanguageId,
                                                            NotificationSettings_NotifierId)
                                      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?);";
                        using(var statement = conn.Prepare(sql))
                        {
                            statement.Bind(1, 0);       // ID 0
                            statement.Bind(2, 2);    // ChannelSettings
                            statement.Bind(3, 2);    // ConversationSettings
                            statement.Bind(4, 2);    // GroupSettings
                            statement.Bind(5, 2);    // BallotSettings
                            statement.Bind(6, 1);    // AnnouncementSettings
                            statement.Bind(7, 1);    // GeneralListOrder

                            // Frage bevorzugte Sprache ab.
                            CultureInfo ci = new CultureInfo(Windows.System.UserProfile.GlobalizationPreferences.Languages[0]);
                            if(ci.TwoLetterISOLanguageName == "en")
                            {
                                statement.Bind(8, 0);   // Englisch als Default.
                            }
                            else
                            {
                                statement.Bind(8, 1);   // Deutsch als Default.
                            }

                            statement.Bind(9, 1);    // NotificationSettings

                            statement.Step();
                            Debug.WriteLine("Inserted default values.");
                        }
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("Error while setting the default settings.");
                Debug.WriteLine("The message is {0}.", sqlEx.Message);
            }
        }

        /// <summary>
        /// Methode, um einen Dummy-Moderator Datensatz in die Moderatoren-Tabelle
        /// einzügen. Dieser dient dazu fehlende Autorenreferenzen bei Announcements abzufangen.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void addDummyModerator(SQLiteConnection conn)
        {
            try
            {
                string checkQuery = @"SELECT * FROM Moderator WHERE Id=?;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Bind(1, 0);       // Prüfe auf einen Eintrag mit Id=0.

                    if (checkStmt.Step() != SQLiteResult.ROW)
                    {
                        // Füge einen Dummy-Moderator ein, um Announcements mit einem fehlenden Autor
                        // auf diesen Dummy-Moderator abbilden zu können.
                        string sql = @"INSERT INTO Moderator (id, FirstName, LastName, Email) 
                            VALUES (0, 'Unknown', 'Author', 'not specified')";
                        using (var statement = conn.Prepare(sql))
                        {
                            statement.Step();
                            Debug.WriteLine("Inserted the dummy moderator object.");
                        }
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("Adding the dummy moderator has failed. Message is {0}.", sqlEx.Message);
            }
        }

        /// <summary>
        /// Methode, um einen Dummy-Nutzer Datensatz in die User-Tabelle
        /// einzügen. Dieser dient dazu fehlende Autorenreferenzen bei Messages abzufangen. 
        /// Ebenso können fehlende Admin-Nutzer notfalls auf diesen Nutzer abgebildet werden.
        /// </summary>
        /// <param name="conn">Aktive Verbindung zur Datenbank.</param>
        private static void addDummyUser(SQLiteConnection conn)
        {
            try
            {
                string checkQuery = @"SELECT * FROM User WHERE Id=?;";
                using (var checkStmt = conn.Prepare(checkQuery))
                {
                    checkStmt.Bind(1, 0);  // Prüfe auf einen Eintrag mit Id=0.

                    if (checkStmt.Step() != SQLiteResult.ROW)
                    {
                        // Füge Dummy-Nutzer ein.
                        string sql = @"INSERT INTO User (Id, Name) 
                            VALUES (0, 'UnknownUser')";
                        using (var statement = conn.Prepare(sql))
                        {
                            statement.Step();
                            Debug.WriteLine("Inserted the dummy user object");
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("Adding the dummy user has failed. Message is {0}.", sqlEx.Message);
            }
        }
    }
}
