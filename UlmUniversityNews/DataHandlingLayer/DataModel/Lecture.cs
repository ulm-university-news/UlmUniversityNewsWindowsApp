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

        #region ValidationRules
        /// <summary>
        /// Validiert das Property "EndDate".
        /// </summary>
        public void ValidateEndDateProperty()
        {
            if (EndDate == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelEndDateInfoLength, EndDate))
            {
                SetValidationError("EndDate", "AddAndEditChannelEndDateInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Validiert das Property "StartDate".
        /// </summary>
        public void ValidateStartDateProperty()
        {
            if (StartDate == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelStartDateInfoLength, StartDate))
            {
                SetValidationError("StartDate", "AddAndEditChannelStartDateInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Validiert das Property "Assistant".
        /// </summary>
        public void ValidateAssistantProperty()
        {
            if (Assistant == null)
                return;

            if (!checkStringRange(0, Constants.Constants.MaxChannelAssistantInfoLength, Assistant))
            {
                SetValidationError("Assistant", "AddAndEditChannelAssistantInfoTooLongValidationError");
            }
        }

        /// <summary>
        /// Validiert das Property "Lecturer".
        /// </summary>
        public void ValidateLecturerProperty()
        {
            if (Lecturer == null || Lecturer.Trim().Length == 0)
            {
                SetValidationError("Lecturer", "AddAndEditChannelLecturerIsNullValidationError");
                return;
            }
            if (!checkStringRange(0, Constants.Constants.MaxChannelLecturerInfoLength, Lecturer))
            {
                SetValidationError("Lecturer", "AddAndEditChannelLecturerTooLongValidatonError");
            }
        }

        /// <summary>
        /// Evaluiert alle Validierungsregeln für die Properties, denen eine solche Regel zugwiesen wurde.
        /// </summary>
        public override void ValidateAll()
        {
            System.Diagnostics.Debug.WriteLine("In ValidateAll of Lecture class.");

            ValidateAssistantProperty();
            ValidateLecturerProperty();
            ValidateStartDateProperty();
            ValidateEndDateProperty();

            base.ValidateAll();
        }
        #endregion ValidationRules
    }
}
