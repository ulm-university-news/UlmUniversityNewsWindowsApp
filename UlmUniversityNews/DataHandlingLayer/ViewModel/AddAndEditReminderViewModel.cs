using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    public class AddAndEditReminderViewModel : DialogBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        #region Properties

        private bool isAddReminderDialog;
        /// <summary>
        /// Gibt an, ob es sich beim akutellen Dialog um einen 
        /// "Reminder hinzufügen" Dialog handelt.
        /// </summary>
        public bool IsAddReminderDialog
        {
            get { return isAddReminderDialog; }
            set { this.setProperty(ref this.isAddReminderDialog, value); }
        }

        private bool isEditReminderDialog;
        /// <summary>
        /// Gibt an, ob es sich beim aktuellen Dialog um einen
        /// "Reminder bearbeiten" Dialog handelt.
        /// </summary>
        public bool IsEditReminderDialog
        {
            get { return isEditReminderDialog; }
            set { this.setProperty(ref this.isEditReminderDialog, value); }
        }
        
        private DateTimeOffset selectedStartDate;
        /// <summary>
        /// Das vom Nutzer gewählte Start-Datum.
        /// </summary>
        public DateTimeOffset SelectedStartDate
        {
            get { return selectedStartDate; }
            set 
            { 
                this.setProperty(ref this.selectedStartDate, value);
                updateNextReminderDate();
            }
        }

        private DateTimeOffset selectedEndDate;
        /// <summary>
        /// Das vom Nutzer gewählte Ende-Datum.
        /// </summary>
        public DateTimeOffset SelectedEndDate
        {
            get { return selectedEndDate; }
            set 
            { 
                this.setProperty(ref this.selectedEndDate, value);
                updateNextReminderDate();
            }
        }

        private TimeSpan selctedTime;
        /// <summary>
        /// Die vom Nutzer gewählte Zeit.
        /// </summary>
        public TimeSpan SelectedTime
        {
            get { return selctedTime; }
            set 
            { 
                this.setProperty(ref this.selctedTime, value);
                updateNextReminderDate();
            }
        }

        private bool isReminderExpired;
        /// <summary>
        /// Gibt an, ob der Reminder mit den aktuellen Einstellungen bereits abgelaufen ist.
        /// </summary>
        public bool IsReminderExpired
        {
            get { return isReminderExpired; }
            set { this.setProperty(ref this.isReminderExpired, value); }
        }
        
        #region IntervalBlock
        private bool isDailyIntervalSelected;
        /// <summary>
        /// Gibt an, ob es sich beim Intervall um eines vom Typ "Täglich" handelt.
        /// </summary>
        public bool IsDailyIntervalSelected
        {
            get { return isDailyIntervalSelected; }
            set
            {
                this.setProperty(ref this.isDailyIntervalSelected, value);
            }
        }

        private bool isWeeklyIntervalSelected;
        /// <summary>
        /// Gibt an, ob es sich beim Intervall um eines vom Typ "Wöchentlich" handelt.
        /// </summary>
        public bool IsWeeklyIntervalSelected
        {
            get { return isWeeklyIntervalSelected; }
            set
            {
                this.setProperty(ref this.isWeeklyIntervalSelected, value);
            }
        }

        private bool isIntervalOneTimeSelected;
        /// <summary>
        /// Gibt an, ob es sich beim Intervall um eines vom Typ "Einmalig" handelt.
        /// </summary>
        public bool IsIntervalOneTimeSelected
        {
            get { return isIntervalOneTimeSelected; }
            set
            {
                this.setProperty(ref this.isIntervalOneTimeSelected, value);
            }
        }

        private int selectedIntervalTypeComboBoxIndex;
        /// <summary>
        /// Der Index des ComboBox gewählten ComboBox Eintrags.
        /// </summary>
        public int SelectedIntervalTypeComboBoxIndex
        {
            get { return selectedIntervalTypeComboBoxIndex; }
            set
            {
                this.setProperty(ref this.selectedIntervalTypeComboBoxIndex, value);
                calculateIntervalValue();
            }
        }

        private int selectedIntervalInDays = 1;
        /// <summary>
        /// Gibt das Intervall an, wenn das Intervall nach dem Typ "Täglich" gewählt wird.
        /// </summary>
        public int SelectedIntervalInDays
        {
            get { return selectedIntervalInDays; }
            set 
            { 
                this.setProperty(ref this.selectedIntervalInDays, value);
                calculateIntervalValue();
            }
        }

        private int selectedIntervalInWeeks = 1;
        /// <summary>
        /// Gibt das Intervall an, wenn das Intervall nach dem Typ "Wöchentlich" gewählt wird.
        /// </summary>
        public int SelectedIntervalInWeeks
        {
            get { return selectedIntervalInWeeks; }
            set 
            { 
                this.setProperty(ref this.selectedIntervalInWeeks, value);
                calculateIntervalValue();
            }
        }

        private bool skipNextReminderDate;
        /// <summary>
        /// Gibt an, ob der nächste Reminder-Termin ausgesetzt werden soll.
        /// </summary>
        public bool SkipNextReminderDate
        {
            get { return skipNextReminderDate; }
            set 
            { 
                this.setProperty(ref this.skipNextReminderDate, value);
                updateNextReminderDate();
            }
        }

        private DateTimeOffset nextReminderDate;
        /// <summary>
        /// Das Datum und die Uhrzeit, an der der Reminder das nächste mal feuert.
        /// </summary>
        public DateTimeOffset NextReminderDate
        {
            get { return nextReminderDate; }
            set { this.setProperty(ref this.nextReminderDate, value); }
        }

        private int intervalValue;
        /// <summary>
        /// Der Wert des Intervalls in Sekunden.
        /// </summary>
        public int IntervalValue
        {
            get { return intervalValue; }
            set { this.setProperty(ref this.intervalValue, value); }
        }        
        #endregion IntervalBlock

        private string title;
        /// <summary>
        /// Der Titel für den Reminder. Die von dem Reminder erzeugten 
        /// Announcements werden diesen Titel besitzen.
        /// </summary>
        public string Title
        {
            get { return title; }
            set { this.setProperty(ref this.title, value); }
        }

        private string text;
        /// <summary>
        /// Der Text für den Reminder. Die von dem Reminder erzeugten 
        /// Announcements werden diesen Inhalt besitzen.
        /// </summary>
        public string Text
        {
            get { return text; }
            set { this.setProperty(ref this.text, value); }
        }

        private bool isPriorityNormalSelected;
        /// <summary>
        /// Ist aktuell die Priorität "Normal" gewählt.
        /// </summary>
        public bool IsPriorityNormalSelcted
        {
            get { return isPriorityNormalSelected; }
            set { this.setProperty(ref this.isPriorityNormalSelected, value); }
        }

        private bool isPriorityHighSelected;
        /// <summary>
        /// Ist aktuell die Priorität "Hoch" gewählt.
        /// </summary>
        public bool IsPriorityHighSelected
        {
            get { return isPriorityHighSelected; }
            set { this.setProperty(ref this.isPriorityHighSelected, value); }
        }

        private Channel selectedChannel;
        /// <summary>
        /// Der gewählte Kanal, für den der Reminder hinzugefügt werden soll, oder
        /// für den ein bestehender Reminder geändert werden soll.
        /// </summary>
        public Channel SelectedChannel
        {
            get { return selectedChannel; }
            set { this.setProperty(ref this.selectedChannel, value); }
        }

        private Reminder selectedReminder;
        /// <summary>
        /// Der gewählte Reminder, der bei einem Änderungsdialog geändert werden soll.
        /// </summary>
        public Reminder SelectedReminder
        {
            get { return selectedReminder; }
            set { this.setProperty(ref this.selectedReminder, value); }
        }
        #endregion Properties

        #region Commands
        private AsyncRelayCommand createReminderCommand;
        /// <summary>
        /// Befehl zum Erzeugen eines neuen Reminder.
        /// </summary>
        public AsyncRelayCommand CreateReminderCommand
        {
            get { return createReminderCommand; }
            set { createReminderCommand = value; }
        }

        private AsyncRelayCommand editReminderCommand;
        /// <summary>
        /// Befehl zum Speichern der erfolgten Bearbeitung eines Reminder.
        /// </summary>
        public AsyncRelayCommand EditReminderCommand
        {
            get { return editReminderCommand; }
            set { editReminderCommand = value; }
        } 
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse AddAndEditReminderViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public AddAndEditReminderViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController(this);

            // Erzeuge Befehle.
            CreateReminderCommand = new AsyncRelayCommand(param => executeCreateReminderCommand());
            EditReminderCommand = new AsyncRelayCommand(param => executeEditReminderCommand());
        }

        /// <summary>
        /// Lädt den Kanal mit der angegebenen Id.
        /// </summary>
        /// <param name="channelId">Die Id des zu ladenden Kanals.</param>
        public async Task LoadSelectedChannelAsync(int channelId)
        {
            try
            {
                SelectedChannel = await Task.Run(() => channelController.GetChannel(channelId));
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Couldn't load channel with id {0}.", channelId);
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Lade einen Dialog zum Erstellen eines Reminders.
        /// </summary>
        public void LoadAddReminderDialog()
        {
            // Initialisiere ViewModel Parameter. 
            IsAddReminderDialog = true;
            IsEditReminderDialog = false;

            SelectedStartDate = DateTimeOffset.Now;
            SelectedEndDate = DateTimeOffset.Now;
            SelectedTime = TimeSpan.FromHours(12.0f);

            IsDailyIntervalSelected = true;
            IsWeeklyIntervalSelected = false;
            IsIntervalOneTimeSelected = false;

            calculateIntervalValue();
            updateNextReminderDate();
        }

        /// <summary>
        /// Lädt den Dialog zur Bearbeitung des Reminders mit der angegebenen Id.
        /// </summary>
        /// <param name="reminderId">Die Id des Reminder, der bearbeitet werden soll.</param>
        public async Task LoadEditReminderDialogAsync(int reminderId)
        {
            // Initialisiere ViewModel Parameter. 
            IsAddReminderDialog = false;
            IsEditReminderDialog = true;

            try 
            {
                SelectedReminder = await Task.Run(() => channelController.GetReminder(reminderId));
            }
            catch (ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
            
            if (SelectedReminder != null)
            {
                SelectedStartDate = SelectedReminder.StartDate;
                SelectedEndDate = SelectedReminder.EndDate;
                SelectedTime = SelectedReminder.StartDate.TimeOfDay;
                IntervalValue = SelectedReminder.Interval;
                int intervalInDays = IntervalValue / (60 * 60 * 24);

                // Lade Intervall Parameter.
                IsIntervalOneTimeSelected = false;
                IsDailyIntervalSelected = false;
                IsWeeklyIntervalSelected = false;
                if (intervalInDays == 0)
                {
                    IsIntervalOneTimeSelected = true;
                    SelectedIntervalTypeComboBoxIndex = 2;  // Eintrag "Einmalig".
                }
                else if ((intervalInDays % 7) == 0)
                {
                    IsWeeklyIntervalSelected = true;
                    SelectedIntervalTypeComboBoxIndex = 1;  // Eintrag "Wöchentlich".
                    SelectedIntervalInWeeks = intervalInDays / 7;
                }
                else
                {
                    IsDailyIntervalSelected = true;
                    selectedIntervalTypeComboBoxIndex = 0;  // Eintrag "Täglich".
                    SelectedIntervalInDays = intervalInDays;
                }

                SkipNextReminderDate = SelectedReminder.Ignore;
                Title = SelectedReminder.Title;
                Text = SelectedReminder.Text;
                if (SelectedReminder.MessagePriority == Priority.NORMAL)
                {
                    IsPriorityNormalSelcted = true;
                    IsPriorityHighSelected = false;
                }
                else if (SelectedReminder.MessagePriority == Priority.HIGH)
                {
                    IsPriorityNormalSelcted = false;
                    IsPriorityHighSelected = true;
                }
            }
        }

        /// <summary>
        /// Aktualisiert das Datum des nächsten Termins, an dem der Reminder feuert.
        /// </summary>
        private void updateNextReminderDate()
        {
            Debug.WriteLine("In updateNextReminderDate.");

            if (SelectedStartDate == DateTimeOffset.MinValue || SelectedEndDate == DateTimeOffset.MinValue)
                return;

            DateTimeOffset reminderStartDate = new DateTimeOffset(SelectedStartDate.Year, SelectedStartDate.Month, SelectedStartDate.Day,
                SelectedTime.Hours, SelectedTime.Minutes, SelectedTime.Seconds, TimeZoneInfo.Local.BaseUtcOffset);
            DateTimeOffset reminderEndDate = new DateTimeOffset(SelectedEndDate.Year, SelectedEndDate.Month, SelectedEndDate.Day,
                SelectedTime.Hours, SelectedTime.Minutes, SelectedTime.Seconds, TimeZoneInfo.Local.BaseUtcOffset);

            // Erzeuge Reminder Objekt mit aktuellen Daten und lasse den nächsten
            // Reminder Zeitpunkt bestimmen.
            Reminder reminderTmp = new Reminder()
            {
                StartDate = reminderStartDate,
                EndDate = reminderEndDate,
                Ignore = SkipNextReminderDate,
                Interval = IntervalValue
            };

            reminderTmp.ComputeFirstNextDate();
            reminderTmp.EvaluateIsExpired();

            if (reminderTmp.IsExpired)
            {
                Debug.WriteLine("Reminder is expired.");
                IsReminderExpired = true;
            }
            else
            {
                IsReminderExpired = false;
            }
            
            NextReminderDate = reminderTmp.NextDate;
        }

        /// <summary>
        /// Hilfsmethode, welche den View Zustand aktualisiert bei einer Änderung des
        /// gewählten Intervalltyps.
        /// </summary>
        private void calculateIntervalValue()
        {
            if (IsDailyIntervalSelected)
            {
                IntervalValue = SelectedIntervalInDays * 60 * 60 * 24;
            }
            else if (IsWeeklyIntervalSelected)
            {
                IntervalValue = SelectedIntervalInWeeks * 60 * 60 * 24 * 7;
            }
            else if (IsIntervalOneTimeSelected)
            {
                IntervalValue = 0;
            }

            Debug.WriteLine("In calculateIntervalValue. Calculated interval value is: {0}.", IntervalValue);
            updateNextReminderDate();
        }

        /// <summary>
        /// Erzeugt eine Instanz der Reminder Klasse, welche mit den Daten gefüllt
        /// wird, die der Nutzer eingegeben hat.
        /// </summary>
        /// <returns>Eine Instanz von Reminder, oder null, falls Erzeugung fehlschlägt.</returns>
        private Reminder createReminderFromEnteredData()
        {
            Moderator activeModerator = channelController.GetLocalModerator();
            if (activeModerator == null)
            {
                Debug.WriteLine("No active moderator. Cannot continue.");
                return null;
            }
            
            // Start und Ende-Datum
            DateTimeOffset reminderStartDate = new DateTimeOffset(SelectedStartDate.Year, SelectedStartDate.Month, SelectedStartDate.Day,
                SelectedTime.Hours, SelectedTime.Minutes, SelectedTime.Seconds, TimeZoneInfo.Local.BaseUtcOffset);
            DateTimeOffset reminderEndDate = new DateTimeOffset(SelectedEndDate.Year, SelectedEndDate.Month, SelectedEndDate.Day,
                SelectedTime.Hours, SelectedTime.Minutes, SelectedTime.Seconds, TimeZoneInfo.Local.BaseUtcOffset);

            int chosenInterval = IntervalValue;
            int channelId = SelectedChannel.Id;
            int authorId = activeModerator.Id;
            string enteredTitle = Title;
            string enteredContent = Text;
            Priority priority = Priority.NORMAL;
            if (IsPriorityHighSelected)
            {
                priority = Priority.HIGH;
            }
            bool ignoreFlag = SkipNextReminderDate;

            Reminder newReminder = new Reminder()
            {
                StartDate = reminderStartDate,
                EndDate = reminderEndDate,
                Interval = chosenInterval,
                ChannelId = channelId,
                AuthorId = authorId,
                Title = enteredTitle,
                Text = enteredContent,
                MessagePriority = priority,
                Ignore = ignoreFlag
            };

            return newReminder;
        }

        /// <summary>
        /// Führt den Befehl CreateReminderCommand aus. Legt einen neuen Reminder an.
        /// </summary>
        private async Task executeCreateReminderCommand()
        {
            // Fülle Reminder Objekt mit eingegebenen Daten.
            Reminder newReminder = createReminderFromEnteredData();
            if (newReminder != null)
            {
                try
                {
                    displayIndeterminateProgressIndicator();

                    bool successful = await channelController.CreateReminderAsync(newReminder);

                    if (successful)
                    {
                        // Gehe zurück auf ModeratorChannelDetails Seite.
                        if (_navService.CanGoBack())
                        {
                            _navService.GoBack();
                        }
                    }
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("Failed to create reminder. Msg is: {0}.", ex.Message);
                    displayError(ex.ErrorCode);
                }
                finally
                {
                    hideIndeterminateProgressIndicator();
                }
            }
        }

        /// <summary>
        /// Führt den Befehl EditReminderCommand aus. Führt die Bearbeitung
        /// des Reminders aus.
        /// </summary>
        private async Task executeEditReminderCommand()
        {
            Reminder oldReminder = SelectedReminder;
            Reminder newReminder = createReminderFromEnteredData();

            if (oldReminder == null || newReminder == null)
                return;

            try
            {
                displayIndeterminateProgressIndicator();

                bool successful = await channelController.UpdateReminderAsync(oldReminder, newReminder);

                if (successful)
                {
                    // Gehe zurück auf ModeratorChannelDetails Seite.
                    if (_navService.CanGoBack())
                    {
                        _navService.GoBack();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Failed to edit reminder. Msg is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
    }
}
