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
                string sql = @"CREATE TABLE IF NOT EXISTS 
                            LocalUser   (Id                 INTEGER NOT NULL,
                                        Name                VARCHAR(35),
                                        ServerAccessToken   VARCHAR(56),
                                        PushAccessToken     VARCHAR(1024),
                                        Platform            INTEGER,
                                        PRIMARY KEY(Id)
                            );";
                using (var statement = conn.Prepare(sql))
                {
                    statement.Step();
                }

                // Schalte Foreign-Key Constraints ein.
                sql = @"PRAGMA foreign_keys = ON";
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
    }
}
