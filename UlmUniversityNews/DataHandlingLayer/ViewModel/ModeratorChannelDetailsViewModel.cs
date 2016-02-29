using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.Common;
using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Exceptions;
using System.Diagnostics;

namespace DataHandlingLayer.ViewModel
{
    public class ModeratorChannelDetailsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ChannelController.
        /// </summary>
        private ChannelController channelController;
        #endregion Fields

        #region Properties
        private Channel channel;
        /// <summary>
        /// Der Kanal, dessen Details angezeigt werden sollen.
        /// </summary>
        public Channel Channel
        {
            get { return channel; }
            set
            {
                this.setProperty(ref this.channel, value);
                checkCommandExecution();
            }
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

        private int selectedPivotItemIndex;
        /// <summary>
        /// Gibt den Index des aktuell ausgewählten PivotItems an.
        /// Index 0 ist "Meine Kanäle", Index 1 ist "Meine Gruppen".
        /// </summary>
        public int SelectedPivotItemIndex
        {
            get { return selectedPivotItemIndex; }
            set
            {
                selectedPivotItemIndex = value;
                checkCommandExecution();
            }
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

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorChannelDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper"></param>
        public ModeratorChannelDetailsViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            channelController = new ChannelController();

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
        /// Lädt die Daten des Kanals mit der übergebenen Id in das ViewModel
        /// und macht die Daten über die Properties verfügbar.
        /// </summary>
        /// <param name="selectedChannelId">Die Id des Kanals, der geladen werden soll.</param>
        public void LoadSelectedChannel(int selectedChannelId)
        {
            try
            {
                Channel = channelController.GetChannel(selectedChannelId);
            }
            catch (ClientException ex)
            {
                // Zeige Fehlernachricht an.
                displayError(ex.ErrorCode);
            }

            if (Channel != null)
            {
                switch (Channel.Type)
                {
                    case ChannelType.LECTURE:
                        Debug.WriteLine("Selected channel is of type Lecture.");
                        Lecture = Channel as Lecture;
                        break;
                    case ChannelType.EVENT:
                        Debug.WriteLine("Selected channel is of type Event.");
                        EventObj = Channel as Event;
                        break;
                    case ChannelType.SPORTS:
                        Debug.WriteLine("Selected channel is of type Sports.");
                        Sports = Channel as Sports;
                        break;
                    default:
                        Debug.WriteLine("Selected channel is of type Student_Group or Other with no special properties.");
                        break;
                }
            }

            // Initialisiere asynchrones Announcement Laden.
            Announcements = new IncrementalLoadingCollection<IncrementalAnnouncementLoaderController, Announcement>(selectedChannelId, 20);
        }

        /// <summary>
        /// Führe eine Aktualisierung der Announcements durch. Es wird beim
        /// Server nach neuen Announcements für den Kanal angefragt.
        /// </summary>
        public async Task PerformAnnouncementUpdate()
        {
            try
            {
                displayIndeterminateProgressIndicator();
                await this.updateAnnouncements(false);
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

        /// <summary>
        /// Eine Hilfsmethode, die die Aktualisierung der Announcements des aktuellen Kanals ausführt.
        /// </summary>
        /// <param name="withCaching">Gibt an, ob der Request bei mehrfachen gleichen Requests innerhalb eines Zeitraums erneut ausgeführt werden soll,
        ///     oder ob der Eintrag aus dem Cache verwendet werden soll.</param>
        /// <exception cref="ClientException">Wirft ClientException, wenn die Aktualisierung der Announcements fehlschlägt.</exception>
        private async Task updateAnnouncements(bool withCaching)
        {
            // Extrahiere als erstes die aktuell höchste MessageNr einer Announcement in diesem Kanal.
            int maxMsgNr = 0;
            maxMsgNr = channelController.GetHighestMsgNrForChannel(Channel.Id);
            Debug.WriteLine("Perform update announcement operation with max messageNumber of {0}.", maxMsgNr);

            // Frage die Announcements ab.
            List<Announcement> receivedAnnouncements = await channelController.GetAnnouncementsOfChannelAsync(Channel.Id, maxMsgNr, withCaching);

            if (receivedAnnouncements != null && receivedAnnouncements.Count > 0)
            {
                await Task.Run(() => channelController.StoreReceivedAnnouncementsAsync(receivedAnnouncements));

                foreach (Announcement announcement in receivedAnnouncements)
                {
                    Announcements.Insert(0, announcement);
                }
            }
        }

        private void checkCommandExecution()
        {
        }
    }
}
