﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Sullinger.ValidatableBase.Models;
using Sullinger.ValidatableBase.Models.ValidationRules;
using System.Reflection;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse User repräsentiert einen Nutzer der Anwendung.
    /// </summary>
    public class User : Sullinger.ValidatableBase.Models.ValidatableBase
    {
        #region properties

        private int id;
        /// <summary>
        /// Die eindeutige Id des Nutzers.
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// Der Name des Nutzers.
        /// </summary>
        [ValidateStringIsGreaterThan(
            GreaterThanValue = 3,
            FailureMessage = "Name needs to be at least 3 characters long.",
            ValidationMessageType = typeof(ValidationErrorMessage))]
        [ValidateStringIsLessThan(
            LessThanValue = 35,
            FailureMessage = "Name must not exceed 35 characters.",
            ValidationMessageType = typeof(ValidationErrorMessage))]
        [ValidateWithCustomHandler(
            DelegateName = "IsNameCorrectlyFormatted",
            FailureMessage = "Name does not match the expected format. Please avoid special characters.",
            ValidationMessageType = typeof(ValidationErrorMessage))]
        [JsonProperty("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private String serverAccessToken;
        /// <summary>
        /// Das Zugriffstoken, mit dem der Nutzer auf dem Server identifiziert werden kann.
        /// </summary>
        [JsonProperty("serverAccessToken", NullValueHandling = NullValueHandling.Ignore)]
        public String ServerAccessToken
        {
            get { return serverAccessToken; }
            set { serverAccessToken = value; }
        }
       
        private String pushAccessToken;
        /// <summary>
        /// Das Token mittels dem das Gerät eines Nutzer im WNS Dienst identifiziert wird.
        /// Der Anwendung, d.h. dem lokalen Nutzer, ist eine Kanal-URI des entsprechenden 
        /// Push Nachrichten Kanals zugeordnet, über den die Push Nachrichten eingehen.
        /// Die Begriffe Kanal-URI und Push Access Token werden hier synonym verwendet.
        /// </summary>
        [JsonProperty("pushAccessToken", NullValueHandling=NullValueHandling.Ignore)]
        public String PushAccessToken
        {
            get { return pushAccessToken; }
            set { pushAccessToken = value; }
        }

        private Enums.Platform platform;
        /// <summary>
        /// Die Platform, d.h. das Betriebssystem des Geräts des Nutzers.
        /// </summary>
        [JsonProperty("platform"), JsonConverter(typeof(StringEnumConverter))]
        public Enums.Platform Platform
        {
            get { return platform; }
            set { platform = value; }
        }

        private Boolean active;
        /// <summary>
        /// Gibt an, ob der Nutzer im aktuellen Kontext aktiv ist oder nicht.
        /// </summary>
        [JsonProperty("active")]
        public Boolean Active
        {
            get { return active; }
            set { active = value; }
        }

        #endregion properties

        /// <summary>
        /// Legt ein neues Nutzer Objekt an.
        /// </summary>
        public User()
        {
        }

        /// <summary>
        /// Legt ein neues Nutzer Objekt an.
        /// </summary>
        /// <param name="id">Die eindeutige Id des Nutzers.</param>
        /// <param name="name">Der Name des Nutzers.</param>
        /// <param name="serverAccessToken">Das Zugriffstoken, mit dem der Nutzer auf dem Server identifiziert werden kann.</param>
        /// <param name="pushAccessToken">Das Token mittels dem das Gerät eines Nutzer im WNS Dienst identifiziert wird.</param>
        /// <param name="platform">Die Platform, d.h. das Betriebssystem des Geräts des Nutzers.</param>
        /// <param name="active">Gibt an, ob der Nutzer im aktuellen Kontext aktiv ist oder nicht.</param>
        public User(int id, string name, String serverAccessToken, String pushAccessToken, Enums.Platform platform, Boolean active)
        {
            this.id = id;
            this.name = name;
            this.serverAccessToken = serverAccessToken;
            this.pushAccessToken = pushAccessToken;
            this.platform = platform;
            this.active = active;
        }

        /// <summary>
        /// Validierungsmethode, die prüft, ob ein Name dem gegebenen Format entspricht.
        /// </summary>
        /// <param name="failureMessage">Eine Fehlernachricht, die ausgegeben werden soll, falls das Format nicht erfüllt wird.</param>
        /// <param name="property">Die zu validierende Property.</param>
        /// <returns>Instanz vom Typ IValidationMessage. Liefert null falls Format eingehalten wird, ansonsten die Fehlermeldung.</returns>
        [ValidationCustomHandlerDelegate(DelegateName = "IsNameCorrectlyFormatted")]
        private IValidationMessage ValidateNameFormat(IValidationMessage failureMessage, PropertyInfo property)
        {
            string regExp = @"^[-_a-zA-Z0-9]+$";

            return System.Text.RegularExpressions.Regex.IsMatch(this.Name, regExp) ? null : failureMessage;
        }
    }
}
