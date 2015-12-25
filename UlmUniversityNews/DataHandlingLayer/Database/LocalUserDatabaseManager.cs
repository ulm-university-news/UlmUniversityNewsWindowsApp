using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using SQLitePCL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace DataHandlingLayer.Database
{
    public class LocalUserDatabaseManager
    {
        /// <summary>
        /// Legt einen Nutzer in der Tabelle localUser an. Die im Objekt übergebene Daten werden in der Datenbank abgespeichert.
        /// </summary>
        /// <param name="localUser">Das Nutzer Objekt mit den Nutzerdaten.</param>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn bei der Ausführung der Operation ein Fehler auftritt.</exception>
        public void StoreLocalUser(User localUser)
        {
            if(localUser == null){
                Debug.WriteLine("No valid user object passed to the storeLocalUser method.");
                return;
            }

            SQLiteConnection conn = DatabaseManager.GetConnection();

            try
            {
                using (var insertLocalUser = conn.Prepare("INSERT INTO LocalUser (Id, Name, ServerAccessToken, PushAccessToken, Platform) VALUES (?,?,?,?,?);"))
                {
                    insertLocalUser.Bind(1, localUser.Id);
                    insertLocalUser.Bind(2, localUser.Name);
                    insertLocalUser.Bind(3, localUser.ServerAccessToken);
                    insertLocalUser.Bind(4, localUser.PushAccessToken);
                    insertLocalUser.Bind(5, (int) localUser.Platform);

                    insertLocalUser.Step();
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLException occured in storeLocalUser.");
                Debug.WriteLine("SQLException: " + sqlEx.HResult + " and message: " + sqlEx.Message);

                throw new DatabaseException("Storing local user account in database has failed.");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception occured in storeLocalUser.");
                Debug.WriteLine("Exception: " + e.HResult + " and message: " + e.Message);

                throw new DatabaseException("Storing local user account in database has failed.");
            }
        }

        /// <summary>
        /// Liefert den lokalen Nutzer aus der Datenbank zurück. 
        /// </summary>
        /// <returns>Ein Objekt der Klasse User.</returns>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn bei der Ausführung der Operation ein Fehler auftritt.</exception>
        public User GetLocalUser()
        {
            SQLiteConnection conn = DatabaseManager.GetConnection();

            User localUser = null;
            try
            {
                using (var statement = conn.Prepare("SELECT * FROM LocalUser;"))
                {
                    if (SQLiteResult.ROW == statement.Step())
                    {
                        // Erstelle das Nutzerobjekt aus den Daten.
                        localUser = new User();
                        localUser.Id = Convert.ToInt32(statement["Id"]);
                        localUser.Name = (string)statement["Name"];
                        localUser.ServerAccessToken = (string)statement["ServerAccessToken"];
                        localUser.PushAccessToken = (string)statement["PushAccessToken"];
                        localUser.Platform = (DataModel.Enums.Platform)Enum.ToObject(typeof(DataModel.Enums.Platform), statement["Platform"]);
                    }
                }
            }
            catch(SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLException occured in getLocalUser.");
                Debug.WriteLine("SQLException: " + sqlEx.HResult + " and message: " + sqlEx.Message);

                throw new DatabaseException("Retrieving local user data from database has failed.");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception occured in getLocalUser.");
                Debug.WriteLine("Exception: " + e.HResult + " and message: " + e.Message);

                throw new DatabaseException("Retrieving local user data from database has failed.");
            }

            return localUser;
        }

        /// <summary>
        /// Aktualisiere den Datensatz des lokalen Nutzers in der Datenbank. Es werden dabei nur die Felder 
        /// für den Namen und das PushAccessToken aktualisiert.
        /// </summary>
        /// <param name="localUser">Das User Objekt mit den aktualisierten Daten des lokalen Nutzers.</param>
        /// <exception cref="DatabaseException">Wirft eine DatabaseException, wenn bei der Ausführung der Operation ein Fehler auftritt.</exception>
        public void UpdateLocalUser(User localUser)
        {
            if(localUser == null){
                Debug.WriteLine("No valid user object passed to the updateLocalUser method.");
                return;
            }

            SQLiteConnection conn = DatabaseManager.GetConnection();

            try
            {
                using (var updateStmt = conn.Prepare("UPDATE LocalUser SET Name = ?, PushAccessToken = ? WHERE Id = ?;"))
                {
                    updateStmt.Bind(1, localUser.Name);
                    updateStmt.Bind(2, localUser.PushAccessToken);
                    updateStmt.Bind(3, localUser.Id);

                    // Update ausführen.
                    updateStmt.Step();
                }
            }
            catch (SQLiteException sqlEx)
            {
                Debug.WriteLine("SQLException occured in updateLocalUser.");
                Debug.WriteLine("SQLException: " + sqlEx.HResult + " and message: " + sqlEx.Message);

                throw new DatabaseException("Updating local user data in database has failed.");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception occured in updateLocalUser.");
                Debug.WriteLine("Exception: " + e.HResult + " and message: " + e.Message);

                throw new DatabaseException("Updating local user data in database has failed.");
            }

        }

        /// <summary>
        /// Ausschließlich für Testzwecke!
        /// </summary>
        public static void DeleteLocalUser()
        {
            SQLiteConnection conn = DatabaseManager.GetConnection();

            try
            {
                using (var deleteStmt = conn.Prepare("DELETE FROM LocalUser"))
                {
                    deleteStmt.Step();
                }
            }
            catch (SQLiteException sqlEx){
                Debug.WriteLine("SQLException occured in DeleteLocalUser.");
                Debug.WriteLine("SQLException: " + sqlEx.HResult + " and message: " + sqlEx.Message);
            }
        }

        /// <summary>
        /// Ausschließlich für Testzwecke!
        /// </summary>
        public static void InsertTestLocalUser()
        {
            SQLiteConnection conn = DatabaseManager.GetConnection();

            try
            {
                using (var insertStmt = conn.Prepare("INSERT INTO LocalUser (Id, Name, ServerAccessToken, PushAccessToken, Platform) VALUES (?,?,?,?,?);"))
                {
                    insertStmt.Bind(1, 40);
                    insertStmt.Bind(2, "PhilippTestUser");
                    insertStmt.Bind(3, "b9bafa4d28938fde58a764c9a4428a896b3f161ab4906dc9f5e3391e");
                    insertStmt.Bind(4, "https://db5.notify.windows.com/?token=AwYAAAAywJk9ydbphz7nG%2fEUTU09P5CMOeLYKMSCTyl81uUx0FbQolUhvVeQGNFRpvuHkk4jCP0zEJqlytbjbXVklj7atyMWZIaoFYc%2fOycixTbnjDzOBmTVLrNV9Sa1wDdKSEk%3d");
                    insertStmt.Bind(5, (int) DataModel.Enums.Platform.WINDOWS);

                    insertStmt.Step();
                }
            }
            catch (SQLiteException sqlEx)
            {

            }
        }
    }
}
