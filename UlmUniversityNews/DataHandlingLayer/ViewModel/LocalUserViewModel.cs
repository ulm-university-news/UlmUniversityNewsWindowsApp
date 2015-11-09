using DataHandlingLayer.API;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class LocalUserViewModel
    {
        /// <summary>
        /// Verweis auf eine Instanz der LocalUserDatabaseManager Klasse.
        /// </summary>
        private LocalUserDatabaseManager localUserDB;
        /// <summary>
        /// Verweis auf eine Instanz der UserAPI Klasse.
        /// </summary>
        private UserAPI userAPI;

        /// <summary>
        /// Erzeugt eine Instanz der LocalUserViewModel Klasse.
        /// </summary>
        public LocalUserViewModel()
        {
            localUserDB = new LocalUserDatabaseManager();
            userAPI = new UserAPI();
        }

        /// <summary>
        /// Erstelle einen lokalen Nutzeraccount.
        /// </summary>
        /// <param name="name">Der Name für den Nutzer.</param>
        /// <param name="pushAccessToken">Das push access token, mittels dem Push Nachrichten an den lokalen Nutzer gesendet werden können.</param>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn die Erstellung des Nutzeraccounts fehlschlägt.</exception>
        public async Task CreateLocalUserAsync(string name, string pushAccessToken)
        {
            Debug.WriteLine("Starting createLocalUser().");

            User localUser = new User();
            localUser.Name = name;
            localUser.PushAccessToken = pushAccessToken;
            localUser.Platform = DataHandlingLayer.DataModel.Enums.Platform.WINDOWS;

            // Generiere Json String aus dem Objekt.
            string jsonContent = JsonConvert.SerializeObject(localUser);
            Debug.WriteLine("The json String is: " + jsonContent);

            // Sende einen Request zum Erstellen eines Nutzeraccounts.
            string responseString;
            try
            {
                responseString = await userAPI.SendCreateUserRequest(jsonContent);
            }
            catch (APIException e) {
                Debug.WriteLine("Error occured. The creation of the user account has failed.");
                Debug.WriteLine("The error code is: " + e.ErrorCode + " and the status code of the response is: " + e.ResponseStatusCode);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(e.ErrorCode, "Creation of user account has failed.");
            }

            // Deserialisiere den Antwort String des Servers.
            localUser = parseUserObjectFromJSON(responseString);

            // Speichere den erstellten Nutzeraccount in der Datenbank ab.
            if(localUser != null){
                try
                {
                    localUserDB.StoreLocalUser(localUser);
                }
                catch(DatabaseException ex)
                {
                    Debug.WriteLine("A exception occurred in the local database. Exception message is: " + ex.Message);
                    // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Creation of user account has failed.");
                }
            }

            Debug.WriteLine("Finished createLocalUser().");
        }

        /// <summary>
        /// Liefert die Kanal-URI des Kanals für eingehende Push Nachrichten zurück, die dem
        /// lokalen Nutzer aktuell zugeordnet ist. Die Kanal-URI wird im Folgenden auch als 
        /// Push Access Token. 
        /// </summary>
        /// <returns></returns>
        public String GetPushChannelURIOfLocalUser()
        {
            Debug.WriteLine("Retrieve the push access token of the local user.");
            String pushToken = null;

            User localUser = localUserDB.GetLocalUser();
            if(localUser != null){
                pushToken = localUser.PushAccessToken;
            }

            return pushToken;
        }

        /// <summary>
        /// Gibt den lokalen Nutzer zurück. Liefert null zurück wenn kein lokaler
        /// Nutzer definiert ist.
        /// </summary>
        /// <returns>Instanz der User Klasse, oder null wenn kein lokaler Nutzer definiert ist.</returns>
        public User GetLocalUser()
        {
            Debug.WriteLine("Get the local user.");
            User localUser = localUserDB.GetLocalUser();
            return localUser;
        }

        /// <summary>
        /// Aktualisiert den Datensatz des lokalen Nutzeraccounts falls Änderungen notwendig sind. 
        /// Es können die Attribute Name und Push Access Token aktualisiert werden. Ist eines 
        /// der Attribute geändert worden, so wird ein Request an den Server geschickt.
        /// </summary>
        /// <param name="name">Der neue Name des Nutzers.</param>
        /// <param name="pushAccessToken">Das Push Access Token, d.h. die Kanal-URI des Benachrichtigungskanals.</param>
        /// <exception cref="ClientException">Wenn der Request an den Server und somit die Aktualisierung fehlschlägt.</exception>
        public async Task UpdateLocalUserAsync(string name, string pushAccessToken)
        {
            bool doUpdate = false;
            User localUser = localUserDB.GetLocalUser();
            if (localUserDB != null){
                // Prüfe, ob der Name aktualisiert werden muss.
                if(!(String.Compare(localUser.Name, name) == 0)){
                    Debug.WriteLine("Need to update the user name.");
                    doUpdate = true;
                    localUser.Name = name;
                }

                // Prüfe, ob das PushToken aktualisiert werden muss.
                if (!(String.Compare(localUser.PushAccessToken, pushAccessToken) == 0)){
                    Debug.WriteLine("Need to update the push access token.");
                    doUpdate = true;
                    localUser.PushAccessToken = pushAccessToken;
                }

                // Führe Aktualisierung durch falls notwendig.
                if (doUpdate){
                    // Generiere Json String aus dem Objekt.
                    string jsonContent = JsonConvert.SerializeObject(localUser);
                    Debug.WriteLine("The json String is: " + jsonContent);

                    string responseString;
                    try
                    {
                        responseString = await userAPI.SendUpdateUserRequest(localUser.Id, localUser.ServerAccessToken, jsonContent);
                    }
                    catch (APIException e)
                    {
                        Debug.WriteLine("Error occured. The creation of the user account has failed.");
                        Debug.WriteLine("The error code is: " + e.ErrorCode + " and the status code of the response is: " + e.ResponseStatusCode);
                        // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                        throw new ClientException(e.ErrorCode, "Creation of user account has failed.");
                    }

                    // Deserialisiere den Antwort String des Servers.
                    localUser = parseUserObjectFromJSON(responseString);
                    
                    // Aktualisiere Nutzeraccount in der Datenbank.
                    if(localUser != null){
                        try
                        {
                            localUserDB.UpdateLocalUser(localUser);
                        }
                        catch(DatabaseException ex)
                        {
                            Debug.WriteLine("A exception occurred in the local database. Exception message is: " + ex.Message);
                            // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                            throw new ClientException(ErrorCodes.LocalDatabaseException, "Update of user account has failed.");
                        }
                    }
                }            
            }
        }

        /// <summary>
        /// Erzeugt ein User Objekt aus dem übergebenen JSON String. Ist eine
        /// Umwandlung des JSON Strings nicht möglich, so wird eine ClientException 
        /// geworfen.
        /// </summary>
        /// <param name="jsonString">Der JSON String, der in ein User Objekt umgewandelt werden soll.</param>
        /// <returns>Eine Instanz der Klasse User.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException wenn kein User Objekt aus dem JSON String übergeben werden kann.</exception>
        private User parseUserObjectFromJSON(string jsonString)
        {
            User localUser = null;
            try
            {
                localUser = JsonConvert.DeserializeObject<User>(jsonString);
            }
            catch (JsonException ex)
            {
                Debug.WriteLine("Error during deserialization. Exception is: " + ex.Message);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Parsing of JSON object has failed.");
            }
            return localUser;
        }
        
    }
}
