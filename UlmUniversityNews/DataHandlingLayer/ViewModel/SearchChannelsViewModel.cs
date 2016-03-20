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
using DataHandlingLayer.DataModel.Enums;

namespace DataHandlingLayer.ViewModel
{
    /// <summary>
    /// Die Klasse SearchChannelsViewModel stellt eine Implementierung des ViewModels dar, dass für
    /// die View SearchChannel verwendet wird.
    /// </summary>
    public class SearchChannelsViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Verzeichnis aller Channel Objekte, die aktuell im ViewModel gehalten werden.
        /// Die Kanäle werden im Verzeichnis mittels ihrere Id referenziert.
        /// </summary>
        private Dictionary<int, Channel> allChannels;

        /// <summary>
        /// Feld, das angibt, ob online auf dem Server nach aktualisierten Kanalressourcen gesucht
        /// werden soll.
        /// </summary>
        private bool performOnlineUpdate;
        #endregion Fields

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

        private bool orderByTypeChecked;
        /// <summary>
        /// Gibt den aktuellen Zustand des ToggleButtons OrderByType an. 
        /// </summary>
        public bool OrderByTypeChecked
        {
            get { return orderByTypeChecked; }
            set { this.setProperty(ref this.orderByTypeChecked, value); }
        }  
        #endregion Properties

        #region Commands
        private AsyncRelayCommand startChannelSearchCommand;
        /// <summary>
        /// Befehl, zum Starten der Kanalsuche.
        /// </summary>
        public AsyncRelayCommand StartChannelSearchCommand
        {
            get { return startChannelSearchCommand; }
            set { startChannelSearchCommand = value; }
        }

        private AsyncRelayCommand reorderChannelsCommand;
        /// <summary>
        /// Befehl, um die Anordnung der Kanalressourcen in der Liste zu beeinflussen.
        /// </summary>
        public AsyncRelayCommand ReorderChannelsCommand
        {
            get { return reorderChannelsCommand; }
            set { reorderChannelsCommand = value; }
        }

        private AsyncRelayCommand synchronizeChannelsCommand;
        /// <summary>
        /// Befehl, der eine Synchronisation der Kanaldaten mit den 
        /// neusten Daten des Servers anstößt.
        /// </summary>
        public AsyncRelayCommand SynchronizeChannelsCommand
        {
            get { return synchronizeChannelsCommand; }
            set { synchronizeChannelsCommand = value; }
        }
        
        private RelayCommand channelSelectedCommand;
        /// <summary>
        /// Befehl, welcher den Klick auf ein Kanal in der Auflistung behandelt.
        /// </summary>
        public RelayCommand ChannelSelectedCommand
        {
            get { return channelSelectedCommand; }
            set { channelSelectedCommand = value; }
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

            // Führe Online Update bei nächster Aktualisierung aus.
            performOnlineUpdate = true;

            // Initialisiere Befehle.
            StartChannelSearchCommand = new AsyncRelayCommand(
                param => executeChannelSearchAsync(),
                param => canExecuteSearch());
            ReorderChannelsCommand = new AsyncRelayCommand(
                param => executeReorderChannelsCommandAsync());
            ChannelSelectedCommand = new RelayCommand(
                param => executeChannelSelected(param),
                param => canSelectChannel());
            SynchronizeChannelsCommand = new AsyncRelayCommand(
                param => executeSynchronizeChannelsCommand(),
                param => canSynchronizeChannels());
        }

        /// <summary>
        /// Stößt einen Abruf von aktualisierten Kanal-Ressourcen an und aktualisiert
        /// die Kanaldaten, falls notwendig.
        /// </summary>
        /// <param name="displayErrors">Gibt an, ob ein Fehler, der während der Ausführung auftreten könnte,
        ///     dem Nutzer angezeigt werden soll, oder nicht.</param>
        public async Task UpdateLocalChannelList(bool displayErrors)
        {
            bool channelRemoved = false;
            List<Channel> updatedChannels = null;
            if (performOnlineUpdate)
            {
                DateTimeOffset currentDate = DateTimeOffset.Now;
                try
                {
                    displayIndeterminateProgressIndicator();
                    // Frage Liste von geänderten Kanal-Ressourcen ab.
                    updatedChannels = await channelController.RetrieveUpdatedChannelsFromServerAsync();

                    // Führe Aktualisierungen auf den lokalen Datensätzen aus. Delegiere den Aufruf an einen Hintergrundthread.
                    Debug.WriteLine("Started updating the channel resources.");
                    await Task.Run(() => channelController.AddOrReplaceLocalChannels(updatedChannels));
                    Debug.WriteLine("Finished updating the channel resources.");

                    // Setze Aktualisierungsdatum neu.
                    channelController.SetDateOfLastChannelListUpdate(currentDate);
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("ClientException occurred. The message is {0} and the error code is: {1}.", ex.Message, ex.ErrorCode);
                    // Fehler anzeigen.
                    if (displayErrors)
                    {
                        displayError(ex.ErrorCode);
                    }
                    return;
                }
                finally
                {
                    hideIndeterminateProgressIndicator();
                }

                // Deaktiviere Online Update für den nächsten Aktualisierungsvorgang.
                performOnlineUpdate = false;
            }
            else
            {
                // Führe Offline Aktualisierung durch. Gebe Ausführung an Hintergrundthread.
                updatedChannels = await Task.Run(() => checkForLocallyUpdatedChannelsAsync());
                channelRemoved = await Task.Run(() => removeLocallyDeletedChannelsAsync());
            }

            // Aktualisiere Liste im ViewController.
            if(updatedChannels != null && (updatedChannels.Count > 0 || channelRemoved))
            {
                foreach(Channel channel in updatedChannels)
                {
                    // Prüfe, ob der Kanal schon in der Liste enthalten ist.
                    if(allChannels.ContainsKey(channel.Id))
                    {
                        if (!channel.Equals(allChannels[channel.Id]))
                        {
                            // Ersetze Channel im Verzeichnis durch aktualisierte Version.
                            allChannels[channel.Id] = channel;
                        }
                    }
                    else
                    {
                        // Kanal noch nicht in Liste vorhanden, also füge den Kanal hinzu.
                        allChannels.Add(channel.Id, channel);
                    }
                }

                // Mache neue Kanalliste als Property verfügbar. Beachte hierbei einen 
                // möglicherweiße bereits eingegebenen Suchbegriff und die Sortierung.
                List<Channel> channelList = extractChannelsByName(SearchTerm);
                channelList = reorderListByCurrentViewState(channelList);
                updateObservableCollectionChannels(channelList);
            }
        }

        /// <summary>
        /// Lade die Kanäle aus der Datenbank und mache sie über die Property Channels verfügbar.
        /// </summary>
        public async Task LoadChannelsAsync()
        {
            // Teste zuerst, ob Werte aus dem Cache geladen wurden.
            if(allChannels.Count != 0)
            {
                return;
            }

            // Wenn Werte nicht aus dem Cache geladen wurden, lade sie aus der Datenbank.
            try
            {
                List<Channel> channels = await Task.Run(() => channelController.GetAllChannels());
                channels = reorderListByCurrentViewState(channels);

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
        /// Prüft, ob es Änderungen in den lokalen Datensätzen gibt. Falls Aktualisierungen erforderlich sind
        /// werden die für die View relevanten Properties der im ViewModel verwalteten Kanäle aktualisiert. Falls neue
        /// Datensätze hinzugekommen sind, werden diese in Form einer Liste zurückgegeben.
        /// </summary>
        /// <returns>Eine Liste von neu hinzugekommenen Kanalressourcen.</returns>
        private async Task<List<Channel>> checkForLocallyUpdatedChannelsAsync()
        {
            List<Channel> updatableChannels = new List<Channel>();
            List<Channel> localChannels = await Task.Run(() => channelController.GetAllChannels());

            foreach (Channel localChannel in localChannels)
            {
                if (allChannels.ContainsKey(localChannel.Id))
                {
                    Channel currentChannel = allChannels[localChannel.Id];
                    // Prüfe, ob Aktualisierung des Kanals erforderlich.
                    if (DateTimeOffset.Compare(currentChannel.ModificationDate, localChannel.ModificationDate) < 0)
                    {
                        updateViewRelatedPropeties(currentChannel, localChannel);
                    }

                    // Prüfe, ob ein Kanal als gelöscht markiert wurde.
                    if (currentChannel.Deleted != localChannel.Deleted)
                    {
                        // Füge den Kanal der Liste zu aktualisierender Kanäle hinzu.
                        updatableChannels.Add(localChannel);
                    }
                }
                else
                {
                    // Kanal hinzugekommen.
                    updatableChannels.Add(localChannel);
                }
            }

            return updatableChannels;
        }

        /// <summary>
        /// Prüft, ob es Kanäle in der aktuellen Auflistung gibt, die in der Zwischenzeit
        /// lokal gelöscht wurden. Diese Kanäle werden aus der Auflistung genommen. Liefert zudem zurück,
        /// ob ein Kanal aus der Auflistung genommen wurde, oder nicht.
        /// </summary>
        /// <returns>Liefert true, wenn ein Kanal aus der Auflistung genommen wurde, ansonsten false.</returns>
        private async Task<bool> removeLocallyDeletedChannelsAsync()
        {
            bool channelRemoved = false;
            List<Channel> localChannels = await Task.Run(() => channelController.GetAllChannels());

            List<Channel> currentChannels = allChannels.Values.ToList<Channel>();
            foreach (Channel currentChannel in currentChannels)
            {
                bool isContained = false;

                foreach (Channel localChannel in localChannels)
                {
                    if (localChannel.Id == currentChannel.Id)
                    {
                        isContained = true;
                    }
                }

                if (!isContained)
                {
                    // Entferne aus der Auflistung aller Kanäle.
                    allChannels.Remove(currentChannel.Id);
                    channelRemoved = true;
                }
            }

            return channelRemoved;
        }

        /// <summary>
        /// Aktualisiert die für die View relevanten Properties des im ViewModel verwalteten Kanals.
        /// </summary>
        /// <param name="currentChannel">Der aktuell im ViewModel verwaltete Kanal.</param>
        /// <param name="newChannel">Der Kanal mit den aktualisierten Daten.</param>
        private void updateViewRelatedPropeties(Channel currentChannel, Channel newChannel)
        {
            currentChannel.Name = newChannel.Name;
            currentChannel.Term = newChannel.Term;
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
            // Generiere Ergebnisliste der Suche. Aufgabe wird an Hintergrund-Threads übergebem.
            List<Channel> resultChannels = await Task.Run(() => extractChannelsByName(SearchTerm));

            // Führe noch Sortierung der Liste aus, nach aktuellem View Zustand.
            resultChannels = await Task.Run(() => reorderListByCurrentViewState(resultChannels));

            // Mache Ergebnis-Kanalliste als Property verfügbar.
            updateObservableCollectionChannels(resultChannels);
        }

        /// <summary>
        /// Stoße eine Sortierung der Kanalliste an. Die Liste wird sortiert nach dem aktuellen
        /// Zustand innerhalb der View. Der Zustand kann dabei eine Sortierung nach dem Typ des Kanals
        /// oder eine alphabetische Sortierung nach dem Namen eines Kanals auslösen.
        /// </summary>
        private async Task executeReorderChannelsCommandAsync()
        {
            List<Channel> currentChannelList = Channels.ToList<Channel>();
            // Führe Sortierung der aktuelle angezeigten Liste aus, nach aktuellem View Zustand.
            currentChannelList = await Task.Run(() => reorderListByCurrentViewState(currentChannelList));
            
            // Sortiere Elemente in ObservableCollection um, entsprechend der neuen Sortierung.
            updateObservableCollectionChannels(currentChannelList);
        }

        /// <summary>
        /// Ändert die ObservableCollection Channels, welche die aktuell anzuzeigenden Kanalressourcen enthält, entsprechend
        /// der übergebenen Liste an Kanälen ab. Die Änderungen werden über die ObservableCollection direkt der View
        /// mitgeteilt, so dass sich die Anzeige entsprechend der neuen Daten anpassen kann.
        /// </summary>
        /// <param name="updatedList">Die Liste von Kanälen, auf die die ObservableCollection geändert werden soll.</param>
        private void updateObservableCollectionChannels(List<Channel> updatedList)
        {
            // Lösche aktuell dargestellte Kanäle.
            Channels.Clear();

            foreach(Channel channel in updatedList)
            {
                Channels.Insert(updatedList.IndexOf(channel), channel);
            }
        }

        /// <summary>
        /// Eine Hilfsmethode, die eine übergebene Liste abhängig vom aktuellen Zustand innerhalb der View
        /// sortiert. Der Zustand kann wechseln zwischen einer Sortierung nach dem Typ des Kanals,
        /// oder einer alphabetischen Sortierung nach dem Namen des Kanals.
        /// </summary>
        /// <returns>Eine sortierte Liste mit Kanälen.</returns>
        /// <param name="channelList">Die zu sortierende Liste von Kanälen.</param>
        private List<Channel> reorderListByCurrentViewState(List<Channel> channelList)
        {
            if(OrderByTypeChecked)
            {
                // Ändere Anordnung, so dass Kanäle nach Typ sortiert werden.
                // Extrahiere nur die Vorlesungen.
                IEnumerable<Lecture> lectures = channelList.Where(channel => channel.Type == ChannelType.LECTURE).Cast<Lecture>();
                // Extrahiere die Kanäle anderer Kanaltypen.
                IEnumerable<Channel> otherChannels = channelList.Where(channel => channel.Type != ChannelType.LECTURE);

                // Sortiere die Vorlesungen.
                lectures =
                    from lecture in lectures
                    orderby lecture.Faculty ascending, lecture.Name ascending
                    select lecture;

                // Sortiere die anderen Kanaltypen.
                otherChannels =
                    from channel in otherChannels
                    orderby channel.Type ascending, channel.Name ascending
                    select channel;  

                
                channelList = new List<Channel>();
                // Füge die beiden Listen zusammen.
                foreach (Lecture lecture in lectures)
                {
                    channelList.Add(lecture);
                }
                foreach (Channel channel in otherChannels)
                {
                    channelList.Add(channel);
                }
            }
            else
            {
                // Ändere Anordnung, so dass Kanäle alphabetisch sortiert werden.
                channelList = new List<Channel>(
                    from item in channelList
                    orderby item.Name
                    select item
                    );
            }
            return channelList;
        }

        /// <summary>
        /// Hilfsmethode, die aus der Liste aller Kanalressourcen die Kanäle extrahiert, die den
        /// gegebenen Suchbegriff in ihrem Namen enthalten.
        /// </summary>
        /// <param name="name">Der Suchbegriff, d.h. der komplette Name oder ein Teil des Namens des gesuchten Kanals.</param>
        /// <returns>Eine Liste von Kanalressourcen, die den gegebenen Suchbegriff im Namen enthalten.</returns>
        private List<Channel> extractChannelsByName(string name)
        {
            if(name == null)
            {
                name = string.Empty;
            }

            List<Channel> allChannelsList = allChannels.Values.ToList<Channel>();
            List<Channel> results = new List<Channel>( 
                from item in allChannelsList
                where item.Name.ToLower().Contains(name.ToLower()) || item.Name.ToLower().Contains(name.Trim().ToLower())
                select item
                );
            return results;
        }

        /// <summary>
        /// Gibt an, ob der Befehl SynchronizeChannelsCommand zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeChannels()
        {
            if (allChannels != null)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeChannelsCommand aus. Stößt den Abruf der Informationen
        /// über geänderte Kanäle vom Server an.
        /// </summary>
        private async Task executeSynchronizeChannelsCommand()
        {
            // Setze Wert für Online-Aktualisierung wieder auf true.
            performOnlineUpdate = true;

            // Stoße Aktualisierung der Kanaldaten an.
            await UpdateLocalChannelList(true);
        }

        /// <summary>
        /// Gibt an, ob anhand des aktuellen Zustands ein Kanal ausgewählt werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn die Kanalauswahl zulässig ist, ansonsten false.</returns>
        private bool canSelectChannel()
        {
            return true;
        }

        /// <summary>
        /// Behandelt den Klick auf einen Kanal in der Auflistung. Löst einen Übergang auf die Kanaldetails Seite 
        /// für den geklickten Kanal aus.
        /// </summary>
        /// <param name="selectedChannelObj">Das in der Auflistung gewählte Kanalobjekt.</param>
        private void executeChannelSelected(object selectedChannelObj)
        {
            Debug.WriteLine("ChannelSelectedCommand executed. The passed object is of type: " + selectedChannelObj.GetType());
            Debug.WriteLine("Currently on thread with id: {0} in executeChannelSelected: ", Environment.CurrentManagedThreadId);

            Channel selectedChannel = selectedChannelObj as Channel;
            if(selectedChannel != null)
            {
                _navService.Navigate("ChannelDetails", selectedChannel.Id);
            }     
        }
    }
}
