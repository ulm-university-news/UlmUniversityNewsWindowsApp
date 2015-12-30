using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ViewModel
{
    public class SearchChannelsViewModel : ViewModel
    {
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Liste aller Channel Objekte, die aktuell in der Anwendung verwaltet werden.
        /// </summary>
        private List<Channel> allChannels;

        #region Properties
        private ObservableCollection<Channel> channels;
        /// <summary>
        /// Eine Liste von Instanzen der Klasse Channel. Die Liste enthält die Objekte, die als Ergebnis der 
        /// Suche zurückgeliefert werden.
        /// </summary>
        public ObservableCollection<Channel> Channels
        {
            get { return channels; }
            set { channels = value; }
        }
        
        #endregion Properties

        #region Commands
        #endregion Commands

        /// <summary>
        /// Erzeuge eine Instanz der Klasse SearchChannelsViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorReporter">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public SearchChannelsViewModel(INavigationService navService, IErrorMapper errorReporter)
            : base(navService, errorReporter)
        {
            channelController = new ChannelController();
            allChannels = new List<Channel>();

        }

        /// <summary>
        /// Lade die Kanäle aus der Datenbank und mache sie über die Property Channels verfügbar.
        /// </summary>
        public async Task LoadChannelsAsync()
        {
            try
            {
                allChannels = await Task.Run(() => channelController.GetAllChannels());

                // Mache Kanäle über Property abrufbar.
                Channels = new ObservableCollection<Channel>(channels);
            }
            catch (ClientException e)
            {
                displayError(e.ErrorCode);
            }
        }
    }
}
