using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class SearchChannelsViewModel : ViewModel
    {
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Verzeichnis aller Channel Objekte, die aktuell in der Anwendung verwaltet werden.
        /// Die Kanäle werden im Verzeichnis mittels ihrere Id referenziert.
        /// </summary>
        private Dictionary<int, Channel> allChannels;

        #region Properties
        private ObservableCollection<Channel> channels;
        /// <summary>
        /// Eine Liste von Instanzen der Klasse Channel. Die Liste enthält die Objekte, die als Ergebnis der 
        /// Suche zurückgeliefert werden.
        /// </summary>
        public ObservableCollection<Channel> Channels
        {
            get { return channels; }
            set { this.setProperty(ref this.channels, value); }
        }

        private string searchTerm;
        /// <summary>
        /// Der aktuell im Textfeld eingegebene Suchbegriff.
        /// </summary>
        public string SearchTerm
        {
            get { return searchTerm; }
            set { this.setProperty(ref this.searchTerm, value); }
        }        
        #endregion Properties

        #region Commands
        private AsyncRelayCommand startChannelSearchCommand;
        /// <summary>
        /// Kommando, zum Starten der Kanalsuche.
        /// </summary>
        public AsyncRelayCommand StartChannelSearchCommand
        {
            get { return startChannelSearchCommand; }
            set { startChannelSearchCommand = value; }
        }
        
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
            allChannels = new Dictionary<int, Channel>();

            // Initialisiere Kommandos.
            StartChannelSearchCommand = new AsyncRelayCommand(param => executeChannelSearchAsync(), param => canExecuteSearch());
        }

        /// <summary>
        /// Stößt einen Abruf von aktualisierten Kanal-Ressourcen an und aktualisiert
        /// die Kanaldaten, falls notwendig.
        /// </summary>
        public async Task UpdateLocalChannelList()
        {
            displayProgressBar();
            DateTime currentDate = DateTime.Now;

            List<Channel> updatedChannels = null;
            try
            {
                // Frage Liste von geänderten Kanal-Ressourcen ab.
                updatedChannels = await channelController.RetrieveUpdatedChannelsFromServerAsync();

                // Führe Aktualisierungen auf den lokalen Datensätzen aus.
                await Task.Run(() => channelController.UpdateChannels(updatedChannels));

                // Setze Aktualisierungsdatum neu.
                channelController.SetDateOfLastChannelListUpdate(currentDate);
            }
            catch(ClientException ex)
            {
                // Fehler wird nicht an View weitergereicht.
                Debug.WriteLine("ClientException occurred. The message is {0} and the error code is: {1}.", ex.Message, ex.ErrorCode);
                return;
            }
            finally
            {
                hideProgressBar();
            }

            // Aktualisiere Liste im ViewController.
            if(updatedChannels != null && updatedChannels.Count > 0)
            {
                foreach(Channel channel in updatedChannels)
                {
                    // Prüfe, ob der Kanal schon in der Liste enthalten ist.
                    if(allChannels.ContainsKey(channel.Id))
                    {
                        // Ersetze Channel im Verzeichnis durch aktualisierte Version.
                        allChannels[channel.Id] = channel;
                    }
                    else
                    {
                        // Kanal noch nicht in Liste vorhanden, also füge den Kanal hinzu.
                        allChannels.Add(channel.Id, channel);
                    }
                }

                // Mache neue Kanalliste als Property verfügbar.
                Channels = new ObservableCollection<Channel>(allChannels.Values.ToList<Channel>());
            }
        }

        /// <summary>
        /// Lade die Kanäle aus der Datenbank und mache sie über die Property Channels verfügbar.
        /// </summary>
        public async Task LoadChannelsAsync()
        {
            try
            {
                List<Channel> channels = await Task.Run(() => channelController.GetAllChannels());

                // Mache Kanäle über Property abrufbar.
                Channels = new ObservableCollection<Channel>(channels);

                // Trage Kanäle ins Verzeichnis ein.
                foreach(Channel channel in channels)
                {
                    allChannels.Add(channel.Id, channel);
                }
            }
            catch (ClientException ex)
            {
                displayError(ex.ErrorCode);
            }
        }

        /// <summary>
        /// Gibt an, ob das Kommando zur Suche nach Kanälen mit dem aktuellen Zustand ausgeführt werden kann.
        /// </summary>
        /// <returns>Liefert true, wenn das Kommando zur Verfügung steht, ansonsten false.</returns>
        private bool canExecuteSearch()
        {
            return true;
        }

        /// <summary>
        /// Führt die Suche nach einem Kanal aus. Es werden die Kanäle aus der Liste extrahiert, 
        /// die den angegebenen SearchTerm im Namen enthalten. Die gefundenen Kanäle werden über
        /// die Property Channels verfügbar gemacht.
        /// </summary>
        private async Task executeChannelSearchAsync()
        {
            List<Channel> resultChannels = await Task.Run(() => extractChannelsByName(SearchTerm));

            // Mache Ergebnis-Kanalliste als Property verfügbar.
            Channels = new ObservableCollection<Channel>(resultChannels);
        }

        /// <summary>
        /// Hilfsmethode, die aus der Liste aller Kanalressourcen die Kanäle extrahiert, die 
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private List<Channel> extractChannelsByName(string name)
        {
            List<Channel> allChannelsList = allChannels.Values.ToList<Channel>();
            List<Channel> results = new List<Channel>( 
                from item in allChannelsList
                where item.Name.ToLower().Contains(name.ToLower()) || item.Name.ToLower().Contains(name.Trim().ToLower())
                select item
                );
            return results;
        }
    }
}
