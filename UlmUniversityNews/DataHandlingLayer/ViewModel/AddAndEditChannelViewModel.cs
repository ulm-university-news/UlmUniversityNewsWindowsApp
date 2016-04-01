using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.DataModel.Enums;
using System.Diagnostics;
using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;

namespace DataHandlingLayer.ViewModel
{
    public class AddAndEditChannelViewModel : DialogBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf eine Instanz der ChannelController Klasse.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        #region Properties
        private bool isAddChannelDialog;
        /// <summary>
        /// Gibt an, ob es sich aktuell um einen Dialog zum Anlegen eines Kanals handelt.
        /// </summary>
        public bool IsAddChannelDialog
        {
            get { return isAddChannelDialog; }
            set { this.setProperty(ref this.isAddChannelDialog, value); }
        }

        private bool isEditChannelDialog;
        /// <summary>
        /// Gibt an, ob es sich aktuell um einen Dialog zum Bearbeiten eines bestehenden Kanals handelt.
        /// </summary>
        public bool IsEditChannelDialog
        {
            get { return isEditChannelDialog; }
            set { this.setProperty(ref this.isEditChannelDialog, value); }
        }

        private string channelName;
        /// <summary>
        /// Der eingegebene Name des Kanals.
        /// </summary>
        public string ChannelName
        {
            get { return channelName; }
            set { this.setProperty(ref this.channelName, value); }
        }

        private ChannelType selectedChannelType;
        /// <summary>
        /// Der gewählte Typ des Kanals.
        /// </summary>
        public ChannelType SelectedChannelType
        {
            get { return selectedChannelType; }
            set 
            { 
                this.setProperty(ref this.selectedChannelType, value);
                updateFieldsOnTypeChanged();
            }
        }

        #region TermProperties
        private bool isSummerTermSelected;
        /// <summary>
        /// Gibt an, ob der Eintrag "Sommersemester" gewählt wurde.
        /// </summary>
        public bool IsSummerTermSelected
        {
            get { return isSummerTermSelected; }
            set { this.setProperty(ref this.isSummerTermSelected, value); }
        }

        private bool isWinterTermSelected;
        /// <summary>
        /// Gibt an, ob der Eintrag "Wintersemester" gewählt wurde.
        /// </summary>
        public bool IsWinterTermSelected
        {
            get { return isWinterTermSelected; }
            set { this.setProperty(ref this.isWinterTermSelected, value); }
        }

        private string termYear;
        /// <summary>
        /// Das eingegebene Jahresdatum beim Semster.
        /// </summary>
        public string TermYear
        {
            get { return termYear; }
            set { this.setProperty(ref this.termYear, value); }
        }
        #endregion TermProperties

        private string channelDescription;
        /// <summary>
        /// Die eingegebene Beschreibung des Kanals.
        /// </summary>
        public string ChannelDescription
        {
            get { return channelDescription; }
            set { this.setProperty(ref this.channelDescription, value); }
        }

        #region LectureSpecificProperties
        private Faculty selectedFaculty;
        /// <summary>
        /// Die gewählte Fakultät eines Kanals des Typs Vorlesung.
        /// </summary>
        public Faculty SelectedFaculty
        {
            get { return selectedFaculty; }
            set { this.setProperty(ref this.selectedFaculty, value); }
        }

        private string lecturer;
        /// <summary>
        /// Der für den Vorlesungskanal eingetragene Dozent.
        /// </summary>
        public string Lecturer
        {
            get { return lecturer; }
            set { this.setProperty(ref this.lecturer, value); }
        }

        private string assistant;
        /// <summary>
        /// Der für den Vorlesungskanal eingetragene Übungsleiter.
        /// </summary>
        public string Assistant
        {
            get { return assistant; }
            set { this.setProperty(ref this.assistant, value); }
        }

        private string lectureStartDate;
        /// <summary>
        /// Der eingetragene Termin für den Vorlesungsbeginn der Vorlesung.
        /// </summary>
        public string LectureStartDate
        {
            get { return lectureStartDate; }
            set { this.setProperty(ref this.lectureStartDate, value); }
        }

        private string lectureEndDate;
        /// <summary>
        /// Der eingetragene Termin für das Vorlesungsende der Vorlesung.
        /// </summary>
        public string LectureEndDate
        {
            get { return lectureEndDate; }
            set { this.setProperty(ref this.lectureEndDate, value); }
        }     
        #endregion LectureSpecificProperties

        #region EventSpecificProperties
        private string eventCost;
        /// <summary>
        /// Der eingetragene Eintrittspreis für das Event.
        /// </summary>
        public string EventCost
        {
            get { return eventCost; }
            set { this.setProperty(ref this.eventCost, value); }
        }

        private string eventOrganizer;
        /// <summary>
        /// Der eingetragene Organisator des Events.
        /// </summary>
        public string EventOrganizer
        {
            get { return eventOrganizer; }
            set { this.setProperty(ref this.eventOrganizer, value); }
        }
        #endregion EventSpecificProperties

        #region SportsSpecificProperties
        private string sportsCost;
        /// <summary>
        /// Die eingetragenen Kosten für die Teilnahme an der Sportgruppe/Sportveranstaltung.
        /// </summary>
        public string SportsCost
        {
            get { return sportsCost; }
            set { this.setProperty(ref this.sportsCost, value); }
        }

        private string amountOfParticipants;
        /// <summary>
        /// Die eingetragene maximale Anzahl an Teilnehmern für eine Sportgruppe. 
        /// </summary>
        public string AmountOfParticipants
        {
            get { return amountOfParticipants; }
            set { this.setProperty(ref this.amountOfParticipants, value); }
        }      
        #endregion SportsSpecificProperties

        private string dates;
        /// <summary>
        /// Die eingetragenen Termine für den Kanal.
        /// </summary>
        public string Dates
        {
            get { return dates; }
            set { this.setProperty(ref this.dates, value); }
        }

        private string locations;
        /// <summary>
        /// Die eingetragenen Orte für den Kanal.
        /// </summary>
        public string Locations
        {
            get { return locations; }
            set { this.setProperty(ref this.locations, value); }
        }

        private string website;
        /// <summary>
        /// Die eingetragenen Web-Adressen für den Kanal.
        /// </summary>
        public string Website
        {
            get { return website; }
            set { this.setProperty(ref this.website, value); }
        }

        private string contacts;
        /// <summary>
        /// Die eingetragenen Kontaktdaten der Kanalverantwortlichen.
        /// </summary>
        public string Contacts
        {
            get { return contacts; }
            set { this.setProperty(ref this.contacts, value); }
        }

        #region VisibilityProperties
        private bool lectureSpecificFieldsVisible;
        /// <summary>
        /// Gibt an, ob die Datenfelder bezüglich des Kanaltyps Vorlesung aktuell sichtbar sind.
        /// </summary>
        public bool LectureSpecificFieldsVisible
        {
            get { return lectureSpecificFieldsVisible; }
            set { this.setProperty(ref this.lectureSpecificFieldsVisible, value); }
        }

        private bool eventSpecificFieldsVisible;
        /// <summary>
        /// Gibt an, ob die Datenfelder bezüglich des Kanaltyps Event aktuell sichtbar sind.
        /// </summary>
        public bool EventSpecificFieldsVisible
        {
            get { return eventSpecificFieldsVisible; }
            set { this.setProperty(ref this.eventSpecificFieldsVisible, value); }
        }

        private bool sportsSpecificFieldsVisible;
        /// <summary>
        /// Gibt an, ob die Datenfelder bezüglich des Kanaltyps Sport aktuell sichtbar sind.
        /// </summary>
        public bool SportsSpecificFieldsVisible
        {
            get { return sportsSpecificFieldsVisible; }
            set { this.setProperty(ref this.sportsSpecificFieldsVisible, value); }
        }   
        #endregion VisibilityProperties

        private Channel editableChannel;
        /// <summary>
        /// Der Kanal, der zur Änderung ausgewählt wurde.
        /// </summary>
        public Channel EditableChannel
        {
            get { return editableChannel; }
            set { this.setProperty(ref this.editableChannel, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand addChannelCommand;
        /// <summary>
        /// Befehl zum Anlegen eines neuen Kanals.
        /// </summary>
        public AsyncRelayCommand AddChannelCommand
        {
            get { return addChannelCommand; }
            set { addChannelCommand = value; }
        }

        private AsyncRelayCommand editChannelCommand;
        /// <summary>
        /// Befehl zum Bearbeiten eines Kanals.
        /// </summary>
        public AsyncRelayCommand EditChannelCommand
        {
            get { return editChannelCommand; }
            set { editChannelCommand = value; }
        }    
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAndEditChannelViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditChannelViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController(this);

            // Lege Commands an.
            AddChannelCommand = new AsyncRelayCommand(param => executeAddChannelCommand());
            EditChannelCommand = new AsyncRelayCommand(param => executeEditChannelCommand(), param => canEditChannel());
        }

        /// <summary>
        /// Lade den Dialog zum Hinzufügen eines neuen Kanals.
        /// </summary>
        public void LoadAddChannelDialog()
        {
            // Initialisiere View-Properties.
            IsAddChannelDialog = true;
            IsEditChannelDialog = false;

            SelectedChannelType = ChannelType.LECTURE;
            EventSpecificFieldsVisible = false;
            SportsSpecificFieldsVisible = false;
            LectureSpecificFieldsVisible = true;

            SelectedFaculty = Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY;
        }

        /// <summary>
        /// Lädt den Dialog zum Bearbeiten des Kanals mit der angegebenen Id.
        /// </summary>
        /// <param name="channelId">Die Id des zu bearbeitenden Kanals.</param>
        public async Task LoadEditChannelDialogAsync(int channelId)
        {
            // Initialisiere View-Properties.
            IsAddChannelDialog = false;
            IsEditChannelDialog = true;

            try
            {
                // Lade gewählten Kanal.
                Channel selectedChannel = await Task.Run(() => channelController.GetChannel(channelId));
                EditableChannel = selectedChannel;
            }
            catch (ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
     
            // Lade View-Properties mit Daten des gewählten Kanals.
            if (EditableChannel != null)
            {
                ChannelName = EditableChannel.Name;
                SelectedChannelType = EditableChannel.Type;

                if (EditableChannel.Term.StartsWith("S"))
                {
                    IsSummerTermSelected = true;
                }
                else if (EditableChannel.Term.StartsWith("W"))
                {
                    IsWinterTermSelected = true;
                }

                // Extrahiere die Jahreszahl aus dem String.
                TermYear = EditableChannel.Term.Substring(EditableChannel.Term.Length - 4, 4);

                ChannelDescription = EditableChannel.Description;
                Dates = EditableChannel.Dates;
                Locations = EditableChannel.Locations;
                Website = EditableChannel.Website;
                Contacts = EditableChannel.Contacts;

                EventSpecificFieldsVisible = false;
                SportsSpecificFieldsVisible = false;
                LectureSpecificFieldsVisible = false;

                // Lade kanalspezifische Properties.
                switch (SelectedChannelType)
                {
                    case ChannelType.LECTURE:
                        Lecture lecture = EditableChannel as Lecture;
                        if (lecture != null)
                        {
                            SelectedFaculty = lecture.Faculty;
                            Lecturer = lecture.Lecturer;
                            Assistant = lecture.Assistant;
                            LectureStartDate = lecture.StartDate;
                            LectureEndDate = lecture.EndDate;

                            LectureSpecificFieldsVisible = true;
                        }
                        break;
                    case ChannelType.EVENT:
                        Event eventObj = EditableChannel as Event;
                        if (eventObj != null)
                        {
                            EventCost = eventObj.Cost;
                            EventOrganizer = eventObj.Organizer;

                            EventSpecificFieldsVisible = true;
                        }
                        break;
                    case ChannelType.SPORTS:
                        Sports sportsObj = EditableChannel as Sports;
                        if (sportsObj != null)
                        {
                            SportsCost = sportsObj.Cost;
                            AmountOfParticipants = sportsObj.NumberOfParticipants;

                            SportsSpecificFieldsVisible = true;
                        }
                        break;
                }
            }

            // Prüfe Befehlsausführung.
            EditChannelCommand.OnCanExecuteChanged();
        }

        /// <summary>
        /// Aktualisiert den Zustand der View bei einer Änderung des gewählten
        /// Kanaltyps. Aktualisiert insbesondere die Sichtbarkeiten der spezifischen
        /// Datenfelder.
        /// </summary>
        private void updateFieldsOnTypeChanged()
        {
            EventSpecificFieldsVisible = false;
            SportsSpecificFieldsVisible = false;
            LectureSpecificFieldsVisible = false;

            Debug.WriteLine("In updateFieldsOnTypeChanged. The selected channel type is: {0}.", SelectedChannelType);

            switch (SelectedChannelType)
            {
                case ChannelType.LECTURE:
                    LectureSpecificFieldsVisible = true;
                    break;
                case ChannelType.EVENT:
                    EventSpecificFieldsVisible = true;
                    break;
                case ChannelType.SPORTS:
                    SportsSpecificFieldsVisible = true;
                    break;
            }
        }

        /// <summary>
        /// Führt den Befehl zum Anlegen eines neuen Kanals aus.
        /// </summary>
        private async Task executeAddChannelCommand()
        {  
            // Erzeuge Instanz aus den eingegebenen Daten.
            Channel newChannel = createChannelObjectFromEnteredData();

            try
            {
                displayIndeterminateProgressIndicator();
                bool successful = await channelController.CreateChannelAsync(newChannel);

                if (successful)
                {
                    // Navigiere zurück auf den Homescreen der Moderatorenansicht.
                    if (_navService.CanGoBack())
                    {
                        _navService.GoBack();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during creation process of channel. Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob ein gültiger Kanal geladen wurde, der geändert werden kann.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canEditChannel()
        {
            if (EditableChannel != null)
                return true;
            return false;
        }

        /// <summary>
        /// Führt den Befehl zum Bearbeiten eines Kanals aus.
        /// </summary>
        private async Task executeEditChannelCommand()
        {
            if (EditableChannel == null)
                return;

            Channel oldChannel = EditableChannel;
            Channel newChannel = createChannelObjectFromEnteredData();
            try
            {
                displayIndeterminateProgressIndicator();
                bool successful = await channelController.UpdateChannelAsync(oldChannel, newChannel);

                if (successful)
                {
                    // Navigiere zurück auf die Detailsicht des Kanals..
                    if (_navService.CanGoBack())
                        _navService.GoBack();
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during update process of channel. Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Hilfsmethode, welche aus den eingegebenen Daten ein Objekt
        /// vom Typ Channel erstellt. Eventuelle Subklassen-Attribute
        /// werden ebenfalls gesetzt.
        /// </summary>
        /// <returns>Ein Objekt der Klasse Channel, welches die vom Nutzer eingegebenen Daten enthält.</returns>
        private Channel createChannelObjectFromEnteredData()
        {
            // Setze den String für das Semester zusammen.
            string termString = string.Empty;
            if (TermYear != null)
            {
                if (IsSummerTermSelected)
                {
                    termString += "S" + TermYear;
                }
                else if (IsWinterTermSelected)
                {
                    termString += "W" + TermYear;
                }
            }

            // Erzeuge Instanz aus den eingegebenen Daten.
            Channel newChannel = null;
            switch (SelectedChannelType)
            {
                case ChannelType.LECTURE:
                    Lecture lecture = new Lecture()
                    {
                        Name = ChannelName,
                        Description = ChannelDescription,
                        Type = SelectedChannelType,
                        Term = termString,
                        Locations = this.Locations,
                        Dates = this.Dates,
                        Contacts = this.Contacts,
                        Website = this.Website,
                        Faculty = SelectedFaculty,
                        StartDate = LectureStartDate,
                        EndDate = LectureEndDate,
                        Lecturer = this.Lecturer,
                        Assistant = this.Assistant
                    };
                    newChannel = lecture;
                    break;
                case ChannelType.EVENT:
                    Event eventObj = new Event()
                    {
                        Name = ChannelName,
                        Description = ChannelDescription,
                        Type = SelectedChannelType,
                        Term = termString,
                        Locations = this.Locations,
                        Dates = this.Dates,
                        Contacts = this.Contacts,
                        Website = this.Website,
                        Cost = EventCost,
                        Organizer = EventOrganizer
                    };
                    newChannel = eventObj;
                    break;
                case ChannelType.SPORTS:
                    Sports sportsObj = new Sports()
                    {
                        Name = ChannelName,
                        Description = ChannelDescription,
                        Type = SelectedChannelType,
                        Term = termString,
                        Locations = this.Locations,
                        Dates = this.Dates,
                        Contacts = this.Contacts,
                        Website = this.Website,
                        Cost = SportsCost,
                        NumberOfParticipants = AmountOfParticipants
                    };
                    newChannel = sportsObj;
                    break;
                default:
                    newChannel = new Channel()
                    {
                        Name = ChannelName,
                        Description = ChannelDescription,
                        Type = SelectedChannelType,
                        Term = termString,
                        Locations = this.Locations,
                        Dates = this.Dates,
                        Contacts = this.Contacts,
                        Website = this.Website
                    };
                    break;
            }

            return newChannel;
        }
    }
}
