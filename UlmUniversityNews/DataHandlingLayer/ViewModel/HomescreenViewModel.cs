using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DataHandlingLayer.CommandRelays;
using System.Diagnostics;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.Common;

namespace DataHandlingLayer.ViewModel
{
    public class HomescreenViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Speichert die aktuell im ViewModel gehaltenen Kanäle in einem Lookup Verzeichnis.
        /// </summary>
        private Dictionary<int, Channel> currentChannels;

        // Speichert die AppSettings, die zum Zeitpunkt des Ladens der Kanäle aktuell gültig sind.
        // Wird benötigt, um zu prüfen, ob bei geänderten Einstellungen die Liste der Kanäle neu 
        // sortiert werden muss.
        private OrderOption cachedGeneralListSettings;
        private OrderOption cachedChannelOrderSettings;
        #endregion Fields
        
        #region Properties
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
                //Debug.WriteLine("In setter method of selected pivot item index. The new index is: " + value);
                selectedPivotItemIndex = value;
                checkCommandExecution();
            }
        }

        private ObservableCollection<Channel> myChannels;
        /// <summary>
        /// Liste von Kanalobjekten, die der lokale Nutzer abonniert hat.
        /// </summary>
        public ObservableCollection<Channel> MyChannels
        {
            get { return myChannels; }
            set { this.setProperty(ref this.myChannels, value); }
        }

        #endregion Properties

        #region Commands
        private RelayCommand searchChannelsCommand;
        /// <summary>
        /// Kommando, das den Übergang auf die Kanalsuche auslöst.
        /// </summary>
        public RelayCommand SearchChannelsCommand
        {
            get { return searchChannelsCommand; }
            set { searchChannelsCommand = value; }
        }

        private RelayCommand addGroupCommand;
        /// <summary>
        /// Kommando, das den Übergang zum Dialog für das Hinzufügen einer Gruppe auslöst.
        /// </summary>
        public RelayCommand AddGroupCommand
        {
            get { return addGroupCommand; }
            set { addGroupCommand = value; }
        }

        private RelayCommand searchGroupsCommand;
        /// <summary>
        /// Kommando, das den Übergang auf die Gruppensuche auslöst.
        /// </summary>
        public RelayCommand SearchGroupsCommand
        {
            get { return searchGroupsCommand; }
            set { searchGroupsCommand = value; }
        }

        private RelayCommand channelSelected;
        /// <summary>
        /// Es wurde ein Kanal ausgewähhlt, zu dem nun die Kanaldetails angezeigt werden sollen.
        /// </summary>
        public RelayCommand ChannelSelected
        {
            get { return channelSelected; }
            set { channelSelected = value; }
        }
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der HomescreenViewModel Klasse.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public HomescreenViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base(navService, errorMapper)
        {
            // Erzeuge Controller Objekt.
            channelController = new ChannelController(this);

            currentChannels = new Dictionary<int, Channel>();

            // Initialisiere die Kommandos.
            searchChannelsCommand = new RelayCommand(param => executeSearchChannelsCommand(), param => canSearchChannels());
            addGroupCommand = new RelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
            searchGroupsCommand = new RelayCommand(param => executeSearchGroupsCommand(), param => canSearchGroups());
            channelSelected = new RelayCommand(param => executeChannelSelected(param), param => canSelectChannel());
        }

        /// <summary>
        /// Lädt die Kanäle, die der lokale Nutzer abonniert hat und macht diese über die MyChannels Property abrufbar.
        /// </summary>
        public async Task LoadMyChannelsAsync()
        {
            List<Channel> channels;
            try
            {
                if (MyChannels == null || MyChannels.Count == 0)
                {
                    Debug.WriteLine("Loading subscribed channels from DB.");

                    channels = await Task.Run(() => channelController.GetMyChannels());
                    // Sortiere Liste.
                    channels = sortChannelsByApplicationSetting(channels);

                    // Mache Kanäle über Property abrufbar.
                    MyChannels = new ObservableCollection<Channel>(channels);

                    // Speichere die aktuell gültigen Anwendungseinstellungen zwischen.
                    AppSettings currentSettings = channelController.GetApplicationSettings();
                    cachedGeneralListSettings = currentSettings.GeneralListOrderSetting;
                    cachedChannelOrderSettings = currentSettings.ChannelOderSetting;

                    // Speichere die Kanäle im Lookup-Verzeichnis.
                    foreach (Channel channel in channels)
                    {
                        currentChannels.Add(channel.Id, channel);
                    }
                }
                else
                {
                    Debug.WriteLine("It seems the homescreen view was taken from the cache. We check for local updates");

                    // Prüfe zunächst, ob die Einstellungen aktualisiert wurden.
                    AppSettings currentAppSettings = channelController.GetApplicationSettings();
                    if (currentAppSettings.GeneralListOrderSetting != cachedGeneralListSettings ||
                        currentAppSettings.ChannelOderSetting != cachedChannelOrderSettings)
                    {
                        // Restrukturierung der MyChannels Liste.
                        await updateViewModelChannelListOnSettingsChange(currentAppSettings);
                    }
                    else
                    {
                        // Führe nur lokale Synchronisation durch.
                        await updateViewModelChannelList();
                    }             

                    // Führe Aktualisierung von Anzahl ungelesener Nachrichten Properties der Kanäle aus.
                    await UpdateNumberOfUnreadAnnouncements();
                }
            }catch(ClientException e)
            {
                displayError(e.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiere die Werte für die noch ungelesenen Announcements in jedem Kanal aus 
        /// der "MyChannels" Liste.
        /// </summary>
        public async Task UpdateNumberOfUnreadAnnouncements()
        {
            // Delegiere an Hintergrundthread.
            Dictionary<int, int> numberOfUnreadAnnouncements =
                await Task.Run(() => channelController.GetAmountOfUnreadAnnouncementsForMyChannels());

            //Debug.WriteLine("Im on thread with id {0}. ", Environment.CurrentManagedThreadId);
            //Debug.WriteLine("NumberOfUnreadAnnouncements dictionary is {0}.", numberOfUnreadAnnouncements);
            if (numberOfUnreadAnnouncements != null)
            {
                Debug.WriteLine("NumberOfUnreadAnnouncements contains {0} entries.", numberOfUnreadAnnouncements.Count);

                foreach (Channel channel in MyChannels)
                {
                    if (numberOfUnreadAnnouncements.ContainsKey(channel.Id))
                    {
                        // Speichere den Wert aus dem Verzeichnis als neue Anzahl an ungelesenen Announcements.
                        channel.NumberOfUnreadAnnouncements = numberOfUnreadAnnouncements[channel.Id];

                        // Debug.WriteLine("The new value for unreadMsg for channel with id {0} is {1}.",
                        //    channel.Id, channel.NumberOfUnreadAnnouncements);
                    }
                    else
                    {
                        // Debug.WriteLine("Channel with id {0} did not appear in NumberOfUnreadAnnouncements, set nrOfUnreadMsg to 0.", channel.Id);
                        channel.NumberOfUnreadAnnouncements = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Aktualisiert den Zustand der View im Falle eines eingehenden Events, welches
        /// über die Löschung eines Kanals informiert. Schaut, ob der betroffene Kanal in der 
        /// Auflistung ist und löst eine Aktualisierung der Anzeige aus.
        /// </summary>
        /// <param name="channelId">Die Id des betroffenen Kanals.</param>
        public void PerformViewUpdateOnChannelDeletedEvent(int channelId)
        {
            // Redraw herbeiführen durch Löschen und Wiedereinfügen. TODO: Gibt es hier nichts besseres?
            Channel affectedChannel = MyChannels.Where(channel => channel.Id ==channelId).FirstOrDefault();
            if (affectedChannel != null)
            {
                int index = MyChannels.IndexOf(affectedChannel);
                Debug.WriteLine("It seems that the channel with id {0} is deleted on the server.", affectedChannel.Id);

                MyChannels.RemoveAt(index);
                affectedChannel.Deleted = true;
                MyChannels.Insert(index, affectedChannel);
            }
        }

        /// <summary>
        /// Aktualisiert den Zustand der View im Falle eines eingehenden Events, welches über die
        /// Änderung eines Kanals informiert. Schaut, ob der betroffene Kanal in der Auflistung ist und löst
        /// eine Aktualisierung der Anzeige aus.
        /// </summary>
        /// <param name="channelId">Die Id des betroffenen Kanals.</param>
        public async Task PerformViewUpdateOnChannelChangedEvent(int channelId)
        {
            // Aktualisiere die für die View relevanten Properties.
            Channel affectedChannel = MyChannels.Where(channel => channel.Id == channelId).FirstOrDefault();
            if (affectedChannel != null)
            {
                try
                {
                    Channel latestLocalChannel = await Task.Run(() => channelController.GetChannel(channelId));
                    updateViewRelatedChannelProperties(affectedChannel, latestLocalChannel);
                }
                catch (ClientException ex)
                {
                    // Gebe Fehler nicht weiter.
                    Debug.WriteLine("PerformViewUpdateOnChannelChangedEvent: Error occurred." +
                        "Message is: {0}.", ex.Message);
                }
            }
        }

        /// <summary>
        /// Führt eine Synchronisation der im ViewModel aktuell gehaltenen "abonnierten" Kanalressourcen
        /// und der von der Anwendung gehaltenen "abonnierten" Kanalressourcen durch. Prüft, ob durch Änderungen
        /// neue Kanäle hinzugekommen (abonniert) oder entfernt wurden (deabonniert). Prüft
        /// zudem, ob aktualisierte Daten für die bereits verwalteten Kanalressourcen vorliegen.
        /// </summary>
        private async Task updateViewModelChannelList()
        {
            // Hole die aktuelleste Liste der abonnierten Kanäle aus der Datenbank.
            List<Channel> channels = await Task.Run(() => channelController.GetMyChannels());
            // Sortiere Liste.
            channels = sortChannelsByApplicationSetting(channels);

            // Prüfe, ob inzwischen ein neuer Kanal abonniert wurde.
            // Füge fehlende Kanäle der Liste hinzu, an der Position, an der sie laut Sortierung stehen sollten.
            foreach (Channel localChannel in channels)
            {
                if (!currentChannels.ContainsKey(localChannel.Id))
                {
                    Debug.WriteLine("Need to insert channel with id {0} into MyChannels.", localChannel.Id);
                    MyChannels.Insert(channels.IndexOf(localChannel), localChannel);
                    currentChannels.Add(localChannel.Id, localChannel);
                }
                else
                {
                    // Prüfe, ob die Kanaldaten aktualisiert werden müssen.
                    Channel currentChannel = currentChannels[localChannel.Id];
                    if (currentChannel.ModificationDate.CompareTo(localChannel.ModificationDate) < 0)
                    {
                        // Aktualisiere Objektinstanz.
                        Debug.WriteLine("Need to update local properties of channel object with id {0}.", localChannel.Id);
                        updateViewRelatedChannelProperties(currentChannel, localChannel);
                    }

                    // Prüfe, ob Kanal als gelöscht markiert wurde.
                    if (localChannel.Deleted && !currentChannel.Deleted)
                    {
                        // Aktualisiere View bei gelöschtem Kanal.
                        PerformViewUpdateOnChannelDeletedEvent(currentChannel.Id);
                    }
                }
            }

            // Prüfe andererseits, ob ein Kanal aus MyChannels nicht mehr in der Liste der abonnierten Kanäle steht.
            for (int i = 0; i < MyChannels.Count; i++)
            {
                Channel channel = MyChannels[i];
                bool isContained = false;

                foreach (Channel loadedChannel in channels)
                {
                    if (loadedChannel.Id == channel.Id)
                    {
                        isContained = true;
                        break;
                    }
                }

                if (!isContained)
                {
                    Debug.WriteLine("Need to remove channel with id {0} from MyChannels list.", i);
                    MyChannels.RemoveAt(i);
                    currentChannels.Remove(channel.Id);
                }
            }
        }

        /// <summary>
        /// Führt eine Aktualisierung der im ViewModel gehaltenen "abonnierten" Kanalressourcen nach einer
        /// Änderung der kanalspezifischen oder listenspezifischen Anwendungseinstellungen. Die Einträge der Liste
        /// müssen entsprechend der neuen Einstellungen neu angeordnet werden. In diesem Fall werden die Einträge neu
        /// geladen und die Liste neu initialisiert.
        /// </summary>
        /// <param name="currentAppSettings">Die aktuellen Anwendungseinstellungen, die nach der Änderung gelten.</param>
        private async Task updateViewModelChannelListOnSettingsChange(AppSettings currentAppSettings)
        {
            // Hole die aktuelleste Liste der abonnierten Kanäle aus der Datenbank.
            List<Channel> channels = await Task.Run(() => channelController.GetMyChannels());
            // Sortiere Liste.
            channels = sortChannelsByApplicationSetting(channels);

            // Wenn Einstellungen geändert wurden, lade Liste einfach komplett neu.
            // Mache Kanäle über Property abrufbar.
            MyChannels = new ObservableCollection<Channel>(channels);

            // Speichere die aktuell gültigen Anwendungseinstellungen zwischen.
            cachedGeneralListSettings = currentAppSettings.GeneralListOrderSetting;
            cachedChannelOrderSettings = currentAppSettings.ChannelOderSetting;

            // Speichere die Kanäle im Lookup-Verzeichnis.
            currentChannels.Clear();
            foreach (Channel channel in channels)
            {
                currentChannels.Add(channel.Id, channel);
            }
        }

        /// <summary>
        /// Sortiert die Liste der Kanäle nach dem aktuell in den Anwendungseinstellungen festgelegten Kriterium.
        /// </summary>
        /// <param name="channels">Die Liste an zu sortierenden Kanälen.</param>
        /// <returns>Eine sortierte Liste der Kanäle.</returns>
        private List<Channel> sortChannelsByApplicationSetting(List<Channel> channels)
        {
            // Hole die Anwendungseinstellungen.
            AppSettings appSettings = channelController.GetApplicationSettings();

            switch (appSettings.ChannelOderSetting)
            {
                case OrderOption.ALPHABETICAL:
                    if (appSettings.GeneralListOrderSetting == OrderOption.ASCENDING)
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.Name ascending
                            select item);
                    }
                    else
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.Name descending
                            select item);
                    }
                    break;
                case OrderOption.BY_TYPE:
                    if (appSettings.GeneralListOrderSetting == OrderOption.ASCENDING)
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.Type ascending, item.Name ascending
                            select item);
                    }
                    else
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.Type descending, item.Name descending
                            select item);
                    }
                    break;
                case OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    if (appSettings.GeneralListOrderSetting == OrderOption.ASCENDING)
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.NumberOfUnreadAnnouncements ascending, item.Name ascending
                            select item);
                    }
                    else
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.NumberOfUnreadAnnouncements descending, item.Name descending
                            select item);
                    }
                    break;
                default:
                    channels = new List<Channel>(
                        from item in channels
                        orderby item.Name ascending
                        select item);
                    break;
            }
            return channels;
        }

        /// <summary>
        /// Aktualisiert für einen Kanal die Properties, die für die Anzeige relvant sind.
        /// <param name="oldChannel">Das Channel Objekt vor der Aktualisierung.</param>
        /// <param name="updatedChannel">Das Channel Objekt nach der Aktualisierung.</param>
        /// </summary>
        private void updateViewRelatedChannelProperties(Channel oldChannel, Channel updatedChannel)
        {
            oldChannel.Name = updatedChannel.Name;
            oldChannel.Term = updatedChannel.Term;
        }

        /// <summary>
        /// Eine Hilfsmethode, die nach einer Statusänderung des Pivot Elements prüft,
        /// ob noch alle Kommandos ausgeführt werden können.
        /// </summary>
        private void checkCommandExecution()
        {
            searchChannelsCommand.RaiseCanExecuteChanged();
            addGroupCommand.RaiseCanExecuteChanged();
            searchGroupsCommand.RaiseCanExecuteChanged();
            channelSelected.RaiseCanExecuteChanged();
        }

        /// <summary>
        /// Zeigt an, ob aktuell auf die Suchseite für Kanäle gewechselt werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn das Kommando ausgeführt werden kann, ansonsten false.</returns>
        private bool canSearchChannels()
        {
            if(selectedPivotItemIndex == 0)     // Aktiv, wenn "Meine Kanäle" Pivotitem aktiv ist.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Führt das Kommando für die Suche nach Kanälen aus. Es wird auf die Suchseite navigiert.
        /// </summary>
        private void executeSearchChannelsCommand()
        {
            _navService.Navigate("ChannelSearch");
        }

        /// <summary>
        /// Zeigt an, ob aktuell eine Gruppe hinzugefügt werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn das Kommando ausgeführt werden kann, ansonsten false.</returns>
        private bool canAddGroup()
        {
            if(selectedPivotItemIndex == 1)     // Aktiv, wenn "Meine Gruppen" PivotItem aktiv ist.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Starte den Dialog zum Hinzufügen einer Gruppe.
        /// </summary>
        private void executeAddGroupCommand()
        {
            // TODO
        }

        /// <summary>
        /// Zeigt an, ob aktuell nach Gruppen gesucht werden kann.
        /// </summary>
        /// <returns>Liefert true zurück, wenn das Kommando ausgeführt werden kann, ansonsten false.</returns>
        private bool canSearchGroups()
        {
            if (selectedPivotItemIndex == 1)    // Aktiv, wenn "Meine Gruppen" PivotItem aktiv ist.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Wechsle auf die Gruppensuche.
        /// </summary>
        private void executeSearchGroupsCommand()
        {
            // TODO
        }

        /// <summary>
        /// Zeigt an, ob aktuell ein Kanal ausgewählt werden kann.
        /// </summary>
        /// <returns></returns>
        private bool canSelectChannel()
        {
            if (selectedPivotItemIndex == 0)     // Aktiv, wenn "Meine Kanäle" Pivotitem aktiv ist.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Bereite Anzeige der Kanaldetails für den ausgwählten Kanal vor und löse Übergang
        /// auf Kanaldetails View aus.
        /// </summary>
        /// <param name="selectedChannelObj">Der ausgewählte Kanal als Objekt.</param>
        private void executeChannelSelected(object selectedChannelObj)
        {
            //Debug.WriteLine("ChannelSelected command executed. The passed object is of type: " + selectedChannelObj.GetType());
            //Debug.WriteLine("Currently on thread with id: {0} in executeChannelSelected.", Environment.CurrentManagedThreadId);

            Channel selectedChannel = selectedChannelObj as Channel;
            if (selectedChannel != null)
            {
                _navService.Navigate("ChannelDetails", selectedChannel.Id);
            }   
        }
    }
}
