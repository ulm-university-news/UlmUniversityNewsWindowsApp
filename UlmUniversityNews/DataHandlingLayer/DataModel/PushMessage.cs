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
    /// Die Klasse PushMessage repräsentiert eine durch den WNS empfangene Push Nachricht.
    /// Die Push Nachrichten werden vom REST Server versandt, um bestimmte Ereignisse oder Änderungen
    /// zu signalisieren. Es gibt verschiedene Typen von Push Nachrichten von denen auch die Semantik 
    /// der weiteren Parameter abhängt.
    /// </summary>
    public class PushMessage
    {
        #region Properties
        private PushType pushType;
        /// <summary>
        /// Der Typ der Push Nachricht.
        /// </summary>
        [JsonProperty("pushType"), JsonConverter(typeof(StringEnumConverter))]
        public PushType PushType
        {
            get { return pushType; }
            set { pushType = value; }
        }

        private int id1;
        /// <summary>
        /// Der erste Id Parameter der Push Nachricht. Die Semantik dieses
        /// Parameters hängt vom Typ der Push Nachricht ab.
        /// </summary>
        [JsonProperty("id1", NullValueHandling = NullValueHandling.Ignore)]
        public int Id1
        {
            get { return id1; }
            set { id1 = value; }
        }

        private int id2;
        /// <summary>
        /// Der zweite Id Parameter der Push Nachricht. Die Semantik dieses
        /// Parameters hängt vom Typ der Push Nachricht ab.
        /// </summary>
        [JsonProperty("id2", NullValueHandling = NullValueHandling.Ignore)]
        public int Id2
        {
            get { return id2; }
            set { id2 = value; }
        }

        private int id3;
        /// <summary>
        /// Der dritte Id Parameter der Push Nachricht. Die Semantik dieses
        /// Parameters hängt vom Typ der Push Nachricht ab.
        /// </summary>
        [JsonProperty("id3", NullValueHandling = NullValueHandling.Ignore)]
        public int Id3
        {
            get { return id3; }
            set { id3 = value; }
        }     
        #endregion Properties

        /// <summary>
        ///  Erzeugt eine Instanz der Klasse PushMessage.
        /// </summary>
        public PushMessage()
        {

        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("Push Message: ");
            sb.AppendLine("     -> Push Type: " + PushType);
            sb.AppendLine("     -> Id1: " + Id1);
            sb.AppendLine("     -> Id2: " + Id2);
            sb.AppendLine("     -> Id3: " + Id3);

            return sb.ToString();
        }
    }
}
