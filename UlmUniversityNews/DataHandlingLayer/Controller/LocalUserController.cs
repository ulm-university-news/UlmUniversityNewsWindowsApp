﻿using DataHandlingLayer.API;
using DataHandlingLayer.Controller.ValidationErrorReportInterface;
using DataHandlingLayer.Database;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking.PushNotifications;

namespace DataHandlingLayer.Controller
{
    public class LocalUserController : MainController
    {
        /// <summary>
        ///  Eine Referenz auf den DatenbankManager für das lokale Nutzerobjekt.
        /// </summary>
        private LocalUserDatabaseManager localUserDB;

        /// <summary>
        /// Verweis auf eine Instanz der UserAPI Klasse.
        /// </summary>
        private UserAPI userAPI;

        /// <summary>
        /// Erzeuge eine Instanz der Klasse LocalUserController Klasse.
        /// </summary>
        public LocalUserController() 
            : base()
        {
            localUserDB = new LocalUserDatabaseManager();
            userAPI = new UserAPI();
        }

        /// <summary>
        /// Erzeuge eine Instanz der Klasse LocalUserController Klasse mit Validierungsbehandlung.
        /// </summary>
        /// <param name="validationErrorReporter">Eine Referenz auf die Realsierung des IValidationErrorReport Interfaces.</param>
        public LocalUserController(IValidationErrorReport validationErrorReporter)
            : base(validationErrorReporter)
        {
            localUserDB = new LocalUserDatabaseManager();
            userAPI = new UserAPI();
        }

        /// <summary>
        /// Erstelle einen lokalen Nutzeraccount.
        /// </summary>
        /// <param name="name">Der Name für den Nutzer.</param>
        /// <returns>Liefert true zurück, wenn der Account erfolgreich angelegt wurde. False, wenn die Validierung fehlgeschlagen hat.</returns>
        /// <exception cref="ClientException">Wirft eine ClientException, wenn die Erstellung des Nutzeraccounts wegen eines aufgetretenen Fehlers fehlschlägt.</exception>
        public async Task<bool> CreateLocalUserAsync(string name)
        {
            Debug.WriteLine("Starting createLocalUser().");

            // TODO: Platziere diesen Code woanders und gebe ihn über einen Parameter an diese Methode.
            // Frage eine Kanal-URI vom WNS ab, die dann als PushAccessToken für diesen Nutzer dient.
            PushNotificationChannel pushChannel = null;
            try {
                pushChannel = await PushNotificationChannelManager.CreatePushNotificationChannelForApplicationAsync();
                Debug.WriteLine("Received a channel URI: " + pushChannel.Uri);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception occurred in CreateLocalUserAsync during WNS initialization. The message is: {0}.", ex.Message);
                // Abbilden auf ClientException.
                throw new ClientException(ErrorCodes.WnsChannelInitializationFailed, "Initialization of channel to WNS has failed.");
            }

            User localUser = new User();
            localUser.Name = name;
            localUser.PushAccessToken = pushChannel.Uri;
            localUser.Platform = DataHandlingLayer.DataModel.Enums.Platform.WINDOWS;

            // Führe Datenvalidierung auf Property Name aus.
            localUser.ClearValidationErrors();  
            clearValidationErrorForProperty("Name");

            localUser.ValidateNameProperty();
            if(localUser.HasValidationError("Name"))
            {
                reportValidationErrors(localUser.GetValidationErrors());
                return false;
            }

            // Generiere Json String aus dem Objekt.
            string jsonContent = jsonParser.ParseUserToJsonString(localUser);
            Debug.WriteLine("The json String is: " + jsonContent);
            if (jsonContent == null)
            {
                Debug.WriteLine("Error during serialization to json. Cannot continue.");
                throw new ClientException(ErrorCodes.JsonParserError, "Creation of user account has failed.");
            }

            // Sende einen Request zum Erstellen eines Nutzeraccounts.
            string responseString;
            try
            {
                responseString = await userAPI.SendCreateUserRequestAsync(jsonContent);
            }
            catch (APIException e)
            {
                Debug.WriteLine("Error occured. The creation of the user account has failed.");
                Debug.WriteLine("The error code is: " + e.ErrorCode + " and the status code of the response is: " + e.ResponseStatusCode);
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(e.ErrorCode, "Creation of user account has failed.");
            }

            // Deserialisiere den Antwort String des Servers.
            localUser = jsonParser.ParseUserFromJson(responseString);

            // Speichere den erstellten Nutzeraccount in der Datenbank ab.
            if (localUser != null)
            {
                try
                {
                    localUserDB.StoreLocalUser(localUser);
                }
                catch (DatabaseException ex)
                {
                    Debug.WriteLine("A exception occurred in the local database. Exception message is: " + ex.Message);
                    // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                    throw new ClientException(ErrorCodes.LocalDatabaseException, "Creation of user account has failed.");
                }
            }
            else
            {
                // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                throw new ClientException(ErrorCodes.JsonParserError, "Creation of user account has failed.");
            }

            Debug.WriteLine("Finished createLocalUser().");
            return true;
        }

        /// <summary>
        /// Liefert das lokale Nutzerobjekt zurück.
        /// </summary>
        /// <returns>Eine Instanz der Klasse User. Liefert null zurück, wenn noch kein lokaler Nutzer angelegt wurde.</returns>
        public User GetLocalUser()
        {
            return base.getLocalUser();
        }

        /// <summary>
        /// Liefert die Kanal-URI des Kanals für eingehende Push Nachrichten zurück, die dem
        /// lokalen Nutzer aktuell zugeordnet ist. Die Kanal-URI wird im Folgenden auch als 
        /// Push Access Token bezeichnet. 
        /// </summary>
        /// <returns>Die URI des aktuellen Kanals vom WNS als String. Null, wenn gerade kein Kanal offen ist.</returns>
        public String GetPushChannelURIOfLocalUser()
        {
            Debug.WriteLine("Retrieve the push access token of the local user.");
            String pushToken = null;

            User localUser = base.getLocalUser();
            if (localUser != null)
            {
                pushToken = localUser.PushAccessToken;
            }

            return pushToken;
        }

        /// <summary>
        /// Aktualisiert den Datensatz des lokalen Nutzeraccounts falls Änderungen notwendig sind. 
        /// Es können die Attribute Name und Push Access Token aktualisiert werden. Ist eines 
        /// der Attribute geändert worden, so wird ein Request an den Server geschickt. Will man nur eines
        /// der beiden Attribute ändern, so kann für das andere ein leerer String übergeben werden oder null. Ein 
        /// Attribut, das nur einen leeren String oder null enthält wird bei der Aktualisierung ignoriert.
        /// </summary>
        /// <param name="name">Der neue Name des Nutzers.</param>
        /// <param name="pushAccessToken">Das Push Access Token, d.h. die Kanal-URI des Benachrichtigungskanals.</param>
        /// <exception cref="ClientException">Wenn der Request an den Server und somit die Aktualisierung fehlschlägt.</exception>
        public async Task UpdateLocalUserAsync(string name, string pushAccessToken)
        {
            bool doUpdate = false;
            User localUser = base.getLocalUser();
            if (localUserDB != null && localUser != null)
            {
                // Prüfe, ob der Name aktualisiert werden muss.
                if (name != null && name != string.Empty && 
                    (String.Compare(localUser.Name, name) != 0))
                {
                    Debug.WriteLine("Name differs from current username, need to update the user name.");
                    doUpdate = true;
                    localUser.Name = name;
                }

                // Prüfe, ob das PushToken aktualisiert werden muss.
                if (pushAccessToken != null && pushAccessToken != string.Empty && 
                    (String.Compare(localUser.PushAccessToken, pushAccessToken) != 0))
                {
                    Debug.WriteLine("Push token differs from current push token, need to update the push access token.");
                    doUpdate = true;
                    localUser.PushAccessToken = pushAccessToken;
                }

                // Führe Aktualisierung durch falls notwendig.
                if (doUpdate)
                {
                    // Generiere Json String aus dem Objekt.
                    string jsonContent = jsonParser.ParseUserToJsonString(localUser);
                    Debug.WriteLine("The json String is: " + jsonContent);
                    if (jsonContent == null)
                        return;

                    string responseString;
                    try
                    {
                        // Sende Aktualisierungs-Request an den Server.
                        responseString = await userAPI.SendUpdateUserRequestAsync(localUser.Id, localUser.ServerAccessToken, jsonContent);
                    }
                    catch (APIException e)
                    {
                        Debug.WriteLine("Error occured. The creation of the user account has failed.");
                        Debug.WriteLine("The error code is: " + e.ErrorCode + " and the status code of the response is: " + e.ResponseStatusCode);
                        // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                        throw new ClientException(e.ErrorCode, "Creation of user account has failed.");
                    }

                    // Deserialisiere den Antwort String des Servers.
                    localUser = jsonParser.ParseUserFromJson(responseString);

                    // Aktualisiere Nutzeraccount in der Datenbank.
                    if (localUser != null)
                    {
                        try
                        {
                            localUserDB.UpdateLocalUser(localUser);
                        }
                        catch (DatabaseException ex)
                        {
                            Debug.WriteLine("A exception occurred in the local database. Exception message is: " + ex.Message);
                            // Abbilden des aufgetretenen Fehlers auf eine ClientException.
                            throw new ClientException(ErrorCodes.LocalDatabaseException, "Update of user account has failed.");
                        }
                    }
                }
            }
        }
    }
}
