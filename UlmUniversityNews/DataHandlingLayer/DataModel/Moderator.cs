using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse Moderator repräsentiert einen Moderatoraccount.  
    /// </summary>
    public class Moderator
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id des Moderators.
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// Der eindeutige Nutzername, der dem Moderatorenaccount zugeordnet ist.
        /// </summary>
        [JsonProperty("name")]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string firstName;
        /// <summary>
        /// Der Vorname des Moderators.
        /// </summary>
        [JsonProperty("firstName", NullValueHandling = NullValueHandling.Ignore)]
        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }

        private string lastName;
        /// <summary>
        /// Der Nachname des Moderators.
        /// </summary>
        [JsonProperty("lastName", NullValueHandling = NullValueHandling.Ignore)]
        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }

        private string email;
        /// <summary>
        /// Die E-Mail Adresse des Moderators.
        /// </summary>
        [JsonProperty("email", NullValueHandling = NullValueHandling.Ignore)]
        public string Email
        {
            get { return email; }
            set { email = value; }
        }

        private string serverAccessToken;
        /// <summary>
        /// Das eindeutige Zugriffstoken, das den Moderator eindeutig auf dem Server identifiziert.
        /// </summary>
        [JsonProperty("serverAccessToken", NullValueHandling = NullValueHandling.Ignore)]
        public string ServerAccessToken
        {
            get { return serverAccessToken; }
            set { serverAccessToken = value; }
        }

        private Language preferredLanguage;
        /// <summary>
        /// Die vom Moderator bevorzugte Sprache.
        /// </summary>
        [JsonProperty("language", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(StringEnumConverter))]
        public Language PreferredLanguage
        {
            get { return preferredLanguage; }
            set { preferredLanguage = value; }
        }

        private bool isAdmin;
        /// <summary>
        /// Gibt an, ob der Moderator auch über Administratorrechte verfügt.
        /// </summary>
        [JsonProperty("admin", NullValueHandling = NullValueHandling.Ignore)]
        public bool IsAdmin
        {
            get { return isAdmin; }
            set { isAdmin = value; }
        }     
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Moderator.
        /// </summary>
        public Moderator()
        {

        }

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Moderator.
        /// </summary>
        /// <param name="id">Die eindeutige Id des Moderators.</param>
        /// <param name="name">Der eindeutige Name, der dem Moderatorenaccount zugeordnet ist.</param>
        /// <param name="firstName">Der Vorname des Moderators.</param>
        /// <param name="lastName">Der Nachname des Moderators.</param>
        /// <param name="email">Die E-mail Adresse des Moderators.</param>
        /// <param name="serverAccessToken">Das eindeutige Zugriffstoken, über das der Moderator identifizierbar ist.</param>
        /// <param name="preferredLanguage">Die bevorzugte Sprache des Moderators.</param>
        /// <param name="isAdmin">Gibt an, ob der Moderator über Administratorrechte verfügt.</param>
        public Moderator(int id, string name, string firstName, string lastName, string email, 
            string serverAccessToken, Language preferredLanguage, bool isAdmin)
        {
            this.id = id;
            this.name = name;
            this.firstName = firstName;
            this.lastName = lastName;
            this.email = email;
            this.serverAccessToken = serverAccessToken;
            this.preferredLanguage = preferredLanguage;
            this.isAdmin = isAdmin;
        }
    }
}
