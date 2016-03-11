using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.Controller;
using System.Diagnostics;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class ReminderDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Eine Referenz auf eine Instanz des ModeratorController.
        /// </summary>
        private ModeratorController moderatorController;
        #endregion Fields

        #region Properties
        private Reminder selectedReminder;
        /// <summary>
        /// Das Reminder Objekt, von dem die Details angezeigt werden sollen.
        /// </summary>
        public Reminder SelectedReminder
        {
            get { return selectedReminder; }
            set { this.setProperty(ref this.selectedReminder, value); }
        }

        private Channel selectedChannel;
        /// <summary>
        /// Das Kanal Objekt des Kanals, zu dem der Reminder gehört.
        /// </summary>
        public Channel SelectedChannel
        {
            get { return selectedChannel; }
            set { this.setProperty(ref this.selectedChannel, value); }
        }

        private string lastModifiedBy;
        /// <summary>
        /// Der Name des Moderators, der den Reminder als letztes geändert hat.
        /// </summary>
        public string LastModifiedBy
        {
            get { return lastModifiedBy; }
            set { this.setProperty(ref this.lastModifiedBy, value); }
        }
        #endregion Properties

        #region Commands
        private RelayCommand switchToEditReminderDialogCommand;
        /// <summary>
        /// Befehl zum Wechseln auf den Dialog zum bearbeiten des Reminder.
        /// </summary>
        public RelayCommand SwitchToEditReminderDialogCommand
        {
            get { return switchToEditReminderDialogCommand; }
            set { switchToEditReminderDialogCommand = value; }
        }     
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ReminderDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ReminderDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController();
            moderatorController = new ModeratorController();

            // Befehle anlegen.
            SwitchToEditReminderDialogCommand = new RelayCommand(
                param => executeSwitchToEditReminderCommand());
        }

        /// <summary>
        /// Lädt den Reminder mit der angegebenen Id und weitere für die View
        /// relavante Parameter.
        /// </summary>
        /// <param name="reminderId">Die Id des Reminders, der geladen werden soll.</param>
        public async Task LoadSelectedReminderAsync(int reminderId)
        {
            try
            {
                SelectedReminder = await Task.Run(() => channelController.GetReminder(reminderId));

                if (SelectedReminder != null)
                {
                    // Berechne nächsten Reminder Termin.
                    SelectedReminder.ComputeFirstNextDate();
                    // Prüfe ob Reminder abgelaufen ist.
                    SelectedReminder.EvaluateIsExpired();

                    SelectedChannel = await Task.Run(() => channelController.GetChannel(SelectedReminder.ChannelId));

                    // Lade Moderator, der Reminder zuletzt geändert hat.
                    // Dieser ist vom Server als Autor eingetragen worden.
                    Moderator author = await Task.Run(() => moderatorController.GetModerator(SelectedReminder.AuthorId));
                    Debug.WriteLine("Author is: {0}.", author);
                    if (author != null)
                        LastModifiedBy = author.FirstName + " " + author.LastName;
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error during loading process. Cannot continue." + 
                    " Message is: {0}.", ex.Message);
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Führt den Befehl SwitchToEditReminderCommand aus. Wechselt auf den Dialog
        /// zum Bearbeiten des Reminder.
        /// </summary>
        private void executeSwitchToEditReminderCommand()
        {
            if (SelectedReminder != null && SelectedChannel != null)
            {
                string navigationParameter = 
                    "navParam?channelId=" + SelectedChannel.Id + "?reminderId=" + SelectedReminder.Id;
                _navService.Navigate("AddAndEditReminder", navigationParameter);
            }
        }
    }
}
