using DataHandlingLayer.Controller;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using System.Diagnostics;

namespace DataHandlingLayer.ViewModel
{
    public class ChannelDetailsViewModel : ViewModel
    {
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

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
        
        private bool channelSubscribedStatus;
        /// <summary>
        /// Gibt an, ob der Kanal vom lokalen Nutzer abonniert ist oder nicht.
        /// </summary>
        public bool ChannelSubscribedStatus
        {
            get { return channelSubscribedStatus; }
            set { this.setProperty(ref this.channelSubscribedStatus, value); }
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
                Debug.WriteLine("In setter method of selected pivot item index. The new index is: " + value);
                selectedPivotItemIndex = value;
                checkCommandExecution();
            }
        }
        #endregion Properties

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeuge eine Instanz von der Klasse ChannelDetailsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorReporter">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ChannelDetailsViewModel(INavigationService navService, IErrorMapper errorReporter)
            : base(navService, errorReporter)
        {
            channelController = new ChannelController();
        }

        /// <summary>
        /// Lädt die Daten des gewählten Kanals in die Properties der ViewModel Instanz.
        /// </summary>
        /// <param name="selectedChannel">Der gewählte Kanal als Objekt.</param>
        public void LoadSelectedChannel(object selectedChannel)
        {
            if (selectedChannel != null)
            {
                Channel = selectedChannel as Channel;

                if(Channel != null)
                {
                    switch (Channel.Type)
                    {
                        case ChannelType.LECTURE:
                            Lecture = selectedChannel as Lecture;
                            break;
                        case ChannelType.EVENT:
                            EventObj = selectedChannel as Event;
                            break;
                        case ChannelType.SPORTS:
                            Sports = selectedChannel as Sports;
                            break;
                        default:
                            Debug.WriteLine("It is a channel of type Student_Group or Other with no special properties.");
                            break;
                    }

                    // Prüfe, ob Kanal bereits abonniert wurde.
                    ChannelSubscribedStatus = channelController.IsChannelSubscribed(Channel.Id);
                }
            }
        }

        /// <summary>
        /// Eine Hilfsmethode, die nach einer Statusänderung des Pivot Elements prüft,
        /// ob noch alle Kommandos ausgeführt werden können.
        /// </summary>
        private void checkCommandExecution()
        {
            // TODO
        }
    }
}
