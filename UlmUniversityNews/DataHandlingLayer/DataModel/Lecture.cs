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
    /// Eine Vorlesung ist ein Kanal mit zusätzlichen vorlesungsspezifischen Properties.
    /// </summary>
    public class Lecture : Channel
    {
        #region Properties
        private Faculty faculty;
        /// <summary>
        /// Die Fakultität, der die Vorlesung zugeordnet ist.
        /// </summary>
        [JsonProperty("faculty", NullValueHandling = NullValueHandling.Ignore), JsonConverter(typeof(StringEnumConverter))]
        public Faculty Faculty
        {
            get { return faculty; }
            set { faculty = value; }
        }
        
        private string startDate;
        /// <summary>
        /// Das Datum, an dem die Vorlesung beginnt.
        /// </summary>
        [JsonProperty("startDate", NullValueHandling = NullValueHandling.Ignore)]
        public string StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        private string endDate;
        /// <summary>
        /// Das Datum, an dem die Vorlesung endet.
        /// </summary>
        [JsonProperty("endDate", NullValueHandling = NullValueHandling.Ignore)]
        public string EndDate
        {
            get { return endDate; }
            set { endDate = value; }
        }

        private string lecturer;
        /// <summary>
        /// Der Dozent der Vorlesung.
        /// </summary>
        [JsonProperty("lecturer", NullValueHandling = NullValueHandling.Ignore)]
        public string Lecturer
        {
            get { return lecturer; }
            set { lecturer = value; }
        }

        private string assistant;
        /// <summary>
        /// Der Übungsleiter der Vorlesung.
        /// </summary>
        [JsonProperty("assistant", NullValueHandling = NullValueHandling.Ignore)]
        public string Assistant
        {
            get { return assistant; }
            set { assistant = value; }
        }    
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz von Lecture.
        /// </summary>
        public Lecture()
        {

        }

        public Lecture(int id, string name, string description, ChannelType type, DateTime creationDate, 
            DateTime modificationDate, string term, string locations, string dates, string contacts,
            string website, bool deleted, Faculty faculty, string startDate, string endDate, string lecturer, string assistant)
            : base(id, name, description, type, creationDate, modificationDate, term, locations, dates, contacts, website, deleted)
        {
            this.faculty = faculty;
            this.startDate = startDate;
            this.endDate = endDate;
            this.lecturer = lecturer;
            this.assistant = assistant;
        }
    }
}
