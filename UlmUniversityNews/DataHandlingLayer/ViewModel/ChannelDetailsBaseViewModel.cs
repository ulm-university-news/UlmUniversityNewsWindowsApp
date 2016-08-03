using DataHandlingLayer.Common;
using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    /// <summary>
    /// Die Klasse ChannelDetailsBaseViewModel stellt Basis-Properties und Basis-Funktionalität
    /// bereit, die von spezfifischeren ViewModels genutzt werden können. Hierbei geht es um Properties
    /// und Funktionalität bezüglich der Anzeige von Kanaldetails eines Kanals.
    /// </summary>
    public class ChannelDetailsBaseViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ChannelController.
        /// </summary>
        protected ChannelController channelController;
        #endregion Fields

        #region Properties
        private Channel channel;
        /// <summary>
        /// Der Kanal, dessen Details angezeigt werden sollen.
        /// </summary>
        public Channel Channel
        {
            get { return channel; }
            set { this.setProperty(ref this.channel, value); }
        }

        private Lecture lecture;
        /// <summary>
        /// Eine Instanz der Klasse Lecture, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Lecture hat. 
        /// </summary>
        public Lecture Lecture
        {
            get { return lecture; }
            set { this.setProperty(ref this.lecture, value); }
        }

        private Sports sports;
        /// <summary>
        /// Eine Instanz der Klasse Sports, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Sports hat.
        /// </summary>
        public Sports Sports
        {
            get { return sports; }
            set { this.setProperty(ref this.sports, value); }
        }

        private Event eventObj;
        /// <summary>
        /// Eine Instanz der Klasse Sports, die gesetzt wird, falls Details zu einem Kanal angezeigt werden sollen,
        /// der den Typ Event hat.
        /// </summary>
        public Event EventObj
        {
            get { return eventObj; }
            set { this.setProperty(ref this.eventObj, value); }
        }

        private string moderators;
        /// <summary>
        /// Die Namen der für den Kanal verantwortlichen Moderatoren.
        /// </summary>
        public string Moderators
        {
            get { return moderators; }
            set { this.setProperty(ref this.moderators, value); }
        }

        private int listRotationAngle;
        /// <summary>
        /// Der Winkel, um den die Liste mit den Announcements gedreht wird.
        /// Der Winkel wird verwendet, um die Anordnung der Announcements (von oben nach untern, von unten nach oben)
        /// zu realisieren.
        /// </summary>
        public int ListRotationAngle
        {
            get { return listRotationAngle; }
            set { this.setProperty(ref this.listRotationAngle, value); }
        }

        private bool showScrollBar;
        /// <summary>
        /// Gibt an, ob die ScrollBar Leiste eingeblendet werden soll, oder ob sie ausgeblendet werden soll.
        /// </summary>
        public bool ShowScrollBar
        {
            get { return showScrollBar; }
            set { showScrollBar = value; }
        }

        private IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement> announcements = null;
        /// <summary>
        /// Die zum Kanal gehörenden Announcements in einer Collection. Hierbei handelt es sich um eine Collection,
        /// die dynamisches Laden von Announcements ermöglicht.
        /// </summary>
        public IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement> Announcements
        {
            get { return announcements; }
            set { this.setProperty(ref this.announcements, value); }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelDetailsBaseViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        protected ChannelDetailsBaseViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController(this);

            // Lade Anwendungseinstellungen und passe View Parameter entsprechend an.
            AppSettings appSettings = channelController.GetApplicationSettings();
            if (appSettings.AnnouncementOrderSetting == OrderOption.ASCENDING)
            {
                ListRotationAngle = 0;
                ShowScrollBar = true;
            }
            else if (appSettings.AnnouncementOrderSetting == OrderOption.DESCENDING)
            {
                ListRotationAngle = 180;
                ShowScrollBar = false;
            }
        }

        /// <summary>
        /// Lädt die Moderatoren des gewählten Kanals.
        /// </summary>
        public async Task LoadModeratorsOfChannelAsync()
        {
            if (Channel == null)
                return;

            string moderatorString = string.Empty;
            try
            {
                List<Moderator> moderators = await Task.Run(() => channelController.GetModeratorsOfChannel(Channel.Id));

                foreach (Moderator moderator in moderators)
                {
                    moderatorString += moderator.FirstName + " " + moderator.LastName + "\n";
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error during loading of moderators. Error code is: {0}.", ex.ErrorCode);
            }

            Moderators = moderatorString;
        }

        /// <summary>
        /// Aktualisiert die Announcements des Kanals. Führt Online
        /// Aktualisierung der Announcements durch, d.h. es wird geprüft, ob neuere Benachrichtigungen
        /// für den Kanal auf dem Server vorliegen.
        /// </summary>
        public async Task PerformAnnouncementUpdateAsync()
        {
            Debug.WriteLine("PerformAnnouncementUpdateAsync: Method started.");

            if (Channel != null && !Channel.Deleted)
            {
                try
                {
                    displayIndeterminateProgressIndicator();
                    // Führe Online Aktualisierung durch. Caching hier erlaubt, d.h. wurde die Abfrage innerhalb eines Zeitraums bereits ausgeführt,
                    // so kann das System entscheiden den Request an den Server nicht abzusetzen.
                    await updateAnnouncementsAsync(true);
                }
                catch (ClientException ex)
                {
                    // bei Fehler keine Nachricht an Nutzer, da Operation im Hintergrund ausgeführt wird.
                    Debug.WriteLine("ClientException occurred during updateAnnouncements. Error code is: {0}.", ex.ErrorCode);
                }
                finally
                {
                    hideIndeterminateProgressIndicator();
                }
            }
        }

        /// <summary>
        /// Aktualisiert den View Zustand, wenn eine neue Announcement per PushNachricht empfangen wurde.
        /// </summary>
        public async Task UpdateAnnouncementsOnAnnouncementReceivedAsync()
        {
            Debug.WriteLine("Update announcements on ReceivedAnnouncement event.");
            if (Channel != null)
            {
                Announcement receivedAnnouncement = await Task.Run(() => channelController.GetLastReceivedAnnouncement(Channel.Id));
                if (Announcements != null && receivedAnnouncement != null
                    && Announcements.Count > 0)
                {
                    // Prüfe, ob die Announcement schon in der Liste ist.
                    // Prüfe hier nur die ersten paar Einträge (die neusten).
                    int maxIndex = 5;
                    if (Announcements.Count < 5)
                        maxIndex = Announcements.Count;

                    for (int i = 0; i < maxIndex; i++)
                    {
                        if (receivedAnnouncement.Id == Announcements[i].Id)
                        {
                            // Beende die Methode. Einfügen nicht notwendig.
                            return;
                        }
                    }

                    // Füge die Announcement der Liste hinzu.
                    Announcements.Insert(0, receivedAnnouncement);
                }
            }
        }

        /// <summary>
        /// Aktualisiere den View-Zustand. Es wurd ein ChannelChanged
        /// Event empfangen, d.h. die Daten des im ViewModel gehaltenen 
        /// Kanals werden aktualisiert.
        /// </summary>
        public async Task PerformViewUpdateOnChannelChangedEventAsync()
        {
            if (Channel == null)
                return;

            try
            {
                // Rufe neusten lokalen Datensatz ab und aktualisiere.
                Channel latestChannelObj = await Task.Run(() => channelController.GetChannel(Channel.Id));
                updateViewRelatedChannelProperties(Channel, latestChannelObj);
            }
            catch (ClientException ex)
            {
                // Keine Fehlermeldung anzeigen.
                Debug.WriteLine("Failed to perform view update on channel changed event." +
                    "Message is: {0}.", ex.Message);
            }
        }

        /// <summary>
        /// Stößt eine Synchronisation der Kanal- und Moderatoreninformationen
        /// des gewählten Kanals an. Fragt entsprechende Informationen vom Server ab
        /// und stößt die Aktualisierung der lokalen Datensätze an.
        /// </summary>
        protected async Task synchroniseChannelInformationAsync()
        {
            if (Channel == null)
                return;

            // Synchronisiere verantwortliche Moderatoren.
            List<Moderator> responsibleModerators = await Task.Run(() => channelController.GetResponsibleModeratorsAsync(Channel.Id));

            // Stoße lokale Synchronisation an.
            await Task.Run(() => channelController.SynchronizeResponsibleModerators(Channel.Id, responsibleModerators));

            // Lade Moderatoren View Property neu.
            await LoadModeratorsOfChannelAsync();

            // Synchronisiere Kanalinformationen.
            Channel referenceChannel = await Task.Run(() => channelController.GetChannelInfoAsync(Channel.Id));
            if (referenceChannel != null)
            {
                if (DateTimeOffset.Compare(Channel.ModificationDate, referenceChannel.ModificationDate) < 0)
                {
                    // Aktualisierung erforderlich.
                    channelController.ReplaceLocalChannelWhileKeepingNotificationSettings(referenceChannel);
                    // Ändere für View relevante Properties, so dass View aktualisiert wird.
                    updateViewRelatedChannelProperties(Channel, referenceChannel);
                }
            }
        }

        /// <summary>
        /// Eine Hilfsmethode, die die Aktualisierung der Announcements des aktuellen Kanals ausführt.
        /// </summary>
        /// <param name="withCaching">Gibt an, ob der Request bei mehrfachen gleichen Requests innerhalb eines Zeitraums erneut ausgeführt werden soll,
        ///     oder ob der Eintrag aus dem Cache verwendet werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn die Aktualisierung der Announcements fehlschlägt.</exception>
        protected async Task updateAnnouncementsAsync(bool withCaching)
        {
            if (Channel == null)
                return;

            // Stoße Synchronisation an.
            await channelController.SynchronizeAnnouncementsWithServerAsync(Channel.Id, withCaching);

            // Aktualisiere die Anzeige.
            // Bestimme zunächst die höchste Nachrichtennummer, die aktuell in der Collection steht.
            int highestMsgNr = 0;
            if (Announcements != null && Announcements.Count > 0)
                highestMsgNr = Announcements.Max(item => item.MessageNumber);

            Debug.WriteLine("updateAnnouncementsAsync: The current max msg number is: {0}.", highestMsgNr);

            // Rufe die fehlenden Nachrichten ab.
            List<Announcement> missingAnnouncements = channelController.GetAnnouncementsOfChannel(Channel.Id, highestMsgNr);
            // Füge der Collection hinzu.
            foreach (Announcement announcement in missingAnnouncements)
            {
                if (Announcements != null)
                {
                    Announcements.Insert(0, announcement);
                }
            }
        }

        /// <summary>
        /// Aktualisiert die für die View relevanten Properties eines aktuell vom ViewModel gehaltenen
        /// Kanal-Objekts.
        /// </summary>
        /// <param name="currentChannel">Das aktuell vom ViewModel gehaltene Channel-Objekt.</param>
        /// <param name="newChannel">Das Channel-Objekt mit den aktualisierten Daten.</param>
        private void updateViewRelatedChannelProperties(Channel currentChannel, Channel newChannel)
        {
            currentChannel.Name = newChannel.Name;
            currentChannel.Description = newChannel.Description;
            currentChannel.Term = newChannel.Term;
            currentChannel.CreationDate = newChannel.CreationDate;
            currentChannel.ModificationDate = newChannel.ModificationDate;
            currentChannel.Locations = newChannel.Locations;
            currentChannel.Dates = newChannel.Dates;
            currentChannel.Contacts = newChannel.Contacts;
            currentChannel.Website = newChannel.Website;

            switch (currentChannel.Type)
            {

                case ChannelType.LECTURE:
                    Lecture currentLecture = currentChannel as Lecture;
                    Lecture newLecture = newChannel as Lecture;
                    currentLecture.StartDate = newLecture.StartDate;
                    currentLecture.EndDate = newLecture.EndDate;
                    currentLecture.Lecturer = newLecture.Lecturer;
                    currentLecture.Assistant = newLecture.Assistant;
                    break;
                case ChannelType.EVENT:
                    Event currentEvent = currentChannel as Event;
                    Event newEvent = newChannel as Event;
                    currentEvent.Cost = newEvent.Cost;
                    currentEvent.Organizer = newEvent.Organizer;
                    break;
                case ChannelType.SPORTS:
                    Sports currentSportsObj = currentChannel as Sports;
                    Sports newSportsObj = newChannel as Sports;
                    currentSportsObj.Cost = newSportsObj.Cost;
                    currentSportsObj.NumberOfParticipants = newSportsObj.NumberOfParticipants;
                    break;
            }
        }
    }
}
