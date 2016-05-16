using DataHandlingLayer.DataModel.Enums;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    public class Group
    {
        #region Properties
        private int id;
        /// <summary>
        /// Die eindeutige Id der Gruppe.
        /// </summary>
        [JsonProperty("id")]
        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        private string name;
        /// <summary>
        /// Der Name der Gruppe.
        /// </summary>
        [JsonProperty("name", NullValueHandling = NullValueHandling.Ignore)]
        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        private string description;
        /// <summary>
        /// Die der Gruppe zugeordnete Beschreibung.
        /// </summary>
        [JsonProperty("description", NullValueHandling = NullValueHandling.Ignore)]
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        private GroupType groupType;
        /// <summary>
        /// Der Typ der Gruppe.
        /// </summary>
        [JsonProperty("groupType"), JsonConverter(typeof(StringEnumConverter))]
        public GroupType GroupType
        {
            get { return groupType; }
            set { groupType = value; }
        }

        private DateTimeOffset creationDate;
        /// <summary>
        /// Das Erstellungsdatum der Gruppe.
        /// </summary>
        [JsonProperty("creationDate", DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset CreationDate
        {
            get { return creationDate; }
            set { creationDate = value; }
        }

        private DateTimeOffset modificationDate;
        /// <summary>
        /// Das Datum der letzten Änderung des Gruppendatensatzes.
        /// </summary>
        [JsonProperty("modificationDate", DefaultValueHandling = DefaultValueHandling.Ignore), JsonConverter(typeof(IsoDateTimeConverter))]
        public DateTimeOffset ModificationDate
        {
            get { return modificationDate; }
            set { modificationDate = value; }
        }

        private string term;
        /// <summary>
        /// Das Semester, das der Gruppe zugeordnet ist.
        /// </summary>
        [JsonProperty("term", NullValueHandling = NullValueHandling.Ignore)]
        public string Term
        {
            get { return term; }
            set { term = value; }
        }

        private string password;
        /// <summary>
        /// Das Passwort, das für den Beitritt zu einer Gruppe benötigt wird.
        /// </summary>
        [JsonProperty("password", NullValueHandling = NullValueHandling.Ignore)]
        public string Password
        {
            get { return password; }
            set { password = value; }
        }

        private int groupAdmin;
        /// <summary>
        /// Die Id des Nutzers, der als Administrator dieser Gruppe dient.
        /// </summary>
        [JsonProperty("groupAdmin", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public int GroupAdmin
        {
            get { return groupAdmin; }
            set { groupAdmin = value; }
        }

        private NotificationSetting groupNotificationSetting;
        /// <summary>
        /// Die akutell für die Gruppe gewählten Benachrichtigungseinstellungen.
        /// </summary>
        public NotificationSetting GroupNotificationSetting
        {
            get { return groupNotificationSetting; }
            set { groupNotificationSetting = value; }
        }
        
        private bool deleted;
        /// <summary>
        /// Gibt an, ob die Gruppe nur noch lokal existiert.
        /// </summary>
        [JsonIgnore]
        public bool Deleted
        {
            get { return deleted; }
            set { deleted = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse Group.
        /// </summary>
        public Group()
        {

        }
    }
}
