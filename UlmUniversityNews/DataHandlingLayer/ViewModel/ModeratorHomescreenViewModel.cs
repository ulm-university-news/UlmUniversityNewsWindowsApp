using DataHandlingLayer.ErrorMapperInterface;
using DataHandlingLayer.NavigationService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
using DataHandlingLayer.Controller;
using System.Diagnostics;
using DataHandlingLayer.Exceptions;
using DataHandlingLayer.CommandRelays;

namespace DataHandlingLayer.ViewModel
{
    public class ModeratorHomescreenViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf eine Instanz der ChannelController Klasse.
        /// </summary>
        private ChannelController channelController;

        /// <summary>
        /// Lookup Tabelle für Kanäle, in der alle aktuell im ViewModel verwalteten Kanäle gespeichert werden.
        /// </summary>
        private Dictionary<int, Channel> currentChannels;

        // Speichert die AppSettings, die zum Zeitpunkt des Ladens der Kanäle aktuell gültig sind.
        // Wird benötigt, um zu prüfen, ob bei geänderten Einstellungen die Liste der Kanäle neu 
        // sortiert werden muss.
        private OrderOption cachedGeneralListSettings;
        private OrderOption cachedChannelOrderSettings;
        #endregion Fields

        #region Properties
        private ObservableCollection<Channel> managedChannels;
        /// <summary>
        /// Kanäle, die vom Moderator verwaltet werden.
        /// </summary>
        public ObservableCollection<Channel> ManagedChannels
        {
            get { return managedChannels; }
            set { this.setProperty(ref this.managedChannels, value); }
        }
        #endregion Properties

        #region Commands
        private RelayCommand channelSelected;
        /// <summary>
        /// Es wurde ein Kanal ausgewähhlt, zu dem nun die Kanaldetails angezeigt werden sollen.
        /// </summary>
        public RelayCommand ChannelSelected
        {
            get { return channelSelected; }
            set { channelSelected = value; }
        }

        private RelayCommand switchToAddChannelDialogCommand;
        /// <summary>
        /// Befehl, um zum Dialog zur Erzeugung eines neuen Kanals zu wechseln.
        /// </summary>
        public RelayCommand SwitchToAddChannelDialogCommand
        {
            get { return switchToAddChannelDialogCommand; }
            set { switchToAddChannelDialogCommand = value; }
        }     
        #endregion Commands

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ModeratorHomescreenViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        public ModeratorHomescreenViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            channelController = new ChannelController();
            currentChannels = new Dictionary<int, Channel>();

            // Erzeuge Befehle.
            ChannelSelected = new RelayCommand(param => executeChannelSelected(param));
            SwitchToAddChannelDialogCommand = new RelayCommand(param => executeSwitchToChannelAddDialogCommand());
        }

        /// <summary>
        /// Lade die Kanäle, für die der eingeloggte Moderator als Verantwortlicher eingetragen ist.
        /// </summary>
        public async Task LoadManagedChannels()
        {
            Moderator activeModerator = channelController.GetLocalModerator();
            if (activeModerator == null)
            {
                Debug.WriteLine("No active moderator found. Can't perform loading.");
                return;
            }

            try
            {
                // Prüfe, ob Seite aus dem Cache kommt oder nicht.
                if (ManagedChannels == null || ManagedChannels.Count == 0)
                {
                    Debug.WriteLine("The managed channels need to be loaded completely.");

                    List<Channel> managedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

                    // Sortiere nach aktuellen Anwendungseinstellungen.
                    managedChannelList = sortChannelsByApplicationSetting(managedChannelList);

                    ManagedChannels = new ObservableCollection<Channel>(managedChannelList);

                    // Speichere die aktuell gültigen Anwendungseinstellungen zwischen.
                    AppSettings currentSettings = channelController.GetApplicationSettings();
                    cachedGeneralListSettings = currentSettings.GeneralListOrderSetting;
                    cachedChannelOrderSettings = currentSettings.ChannelOderSetting;

                    // Füge Kanäle noch dem Lookup-Verzeichnis hinzu.
                    foreach (Channel channel in managedChannelList)
                    {
                        currentChannels.Add(channel.Id, channel);
                    }

                    // Führe noch Aktualisierung mit Daten des REST Servers aus, so dass der lokale Datensatz auf den neuesten Stand kommt. 
                    await updateChannelModeratorRelationships();
                }
                else
                {
                    // Seite kommt aus dem Cache.
                    Debug.WriteLine("We already have managed channels from cache.");
                    
                    // Prüfe zunächst, ob die Einstellungen aktualisiert wurden.
                    AppSettings currentAppSettings = channelController.GetApplicationSettings();
                    if (currentAppSettings.GeneralListOrderSetting != cachedGeneralListSettings ||
                        currentAppSettings.ChannelOderSetting != cachedChannelOrderSettings)
                    {
                        // Aktualisiere Liste nach Änderung der Einstellungen.
                        await updateViewModelChannelListOnSettingsChange(currentAppSettings);
                    }
                    else
                    {
                        // Synchronisiere nur die im ViewModel gehaltene Liste von Kanälen mit den
                        // lokal im System vorhanden Kanälen.
                        await updateViewModelChannelList();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during loading of managedChannels.");
                displayError(ex.ErrorCode);
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
            Channel affectedChannel = ManagedChannels.Where(channel => channel.Id == channelId).FirstOrDefault();
            if (affectedChannel != null)
            {
                int index = ManagedChannels.IndexOf(affectedChannel);
                Debug.WriteLine("It seems that the channel with id {0} is deleted on the server.", affectedChannel.Id);

                ManagedChannels.RemoveAt(index);
                affectedChannel.Deleted = true;
                ManagedChannels.Insert(index, affectedChannel);
            }
        }

        /// <summary>
        /// Stößt die Aktualisierung der Kanal-Moderatoren Beziehungen an.
        /// Fragt dabei die aktuellste Liste an verantwortlichen Kanälen vom Server ab
        /// und stößt die Aktualisierung der lokalen Datenbankeinträge an. Aktualisiert
        /// anschließend die ManagedChannelsListe.
        /// </summary>
        private async Task updateChannelModeratorRelationships()
        {
            Moderator activeModerator = channelController.GetLocalModerator();

            try
            {
                Debug.WriteLine("Start the updating process of channel moderator relationships.");
                
                // Frage Daten vom Server ab und aktualisiere lokale Datensätze.
                List<Channel> managedChannelsServer = await channelController.RetrieveManagedChannelsFromServerAsync(activeModerator.Id);

                // Aktualisiere die Beziehungen Moderator-Kanal für die verantwortlichen Moderatoren.
                await Task.Run(() => channelController.SynchronizeLocalManagedChannels(
                    channelController.GetLocalModerator(),
                    managedChannelsServer));
                
                Debug.WriteLine("Finished the updating process of channel moderator relationships.");
                
                // Aktualisiere ManagedChannels Liste.
                await updateViewModelChannelList();
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during updating of channel and moderator relationships.");
                Debug.WriteLine("Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiert die ManagedChannel Liste, die aktuell im ViewModel verwaltet wird durch
        /// Synchronisation mit den aktuell in der Anwendung verwalteten Datensätzen. Es findet kein Aufruf
        /// an den Server statt. Fügt der ManagedChannel Liste neu hinzugekommene Kanäle hinzu und nimmt nicht
        /// mehr vorhandene Kanäle raus. Falls lokal ein neuerer Datensatz für einen Kanal vorliegt, werden die 
        /// für die View relevanten Daten aktualisiert.
        /// </summary>
        private async Task updateViewModelChannelList()
        {
            Debug.WriteLine("updateViewModelChannelList: Start method. Current amount of items " + 
                "is {0} in the ManagedChannels list.", ManagedChannels.Count);
            Moderator activeModerator = channelController.GetLocalModerator();
            if (activeModerator == null)
                return;

            List<Channel> localChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

            try
            {
                localChannelList = sortChannelsByApplicationSetting(localChannelList);

                // Vergleiche, ob updatedChannelList Kanäle enthält, die noch nicht in ManagedChannels stehen.
                for (int i = 0; i < localChannelList.Count; i++)
                {
                    if (!currentChannels.ContainsKey(localChannelList[i].Id))
                    {
                        // Füge den Kanal der Liste hinzu.
                        currentChannels.Add(localChannelList[i].Id, localChannelList[i]);
                        ManagedChannels.Insert(i, localChannelList[i]);
                    }
                    else
                    {
                        // Prüfe, ob Aktualisierung der lokalen Kanal-Ressource erforderlich.
                        Channel currentChannel = currentChannels[localChannelList[i].Id];
                        if (DateTimeOffset.Compare(currentChannel.ModificationDate, localChannelList[i].ModificationDate) < 0)
                        {
                            updateViewRelatedPropertiesOfChannel(currentChannel, localChannelList[i]);
                        }

                        // Prüfe, ob Kanal als gelöscht markiert wurde.
                        if (localChannelList[i].Deleted && !currentChannel.Deleted)
                        {
                            // Aktualisiere View.
                            PerformViewUpdateOnChannelDeletedEvent(currentChannel.Id);
                        }
                    }
                }

                // Vergleiche, ob ManagedChannels Kanäle enthält, die nicht mehr in updatedChannelList stehen.
                for (int i = 0; i < ManagedChannels.Count; i++ )
                {
                    bool isContained = false;

                    foreach (Channel channel in localChannelList)
                    {
                        if (channel.Id == ManagedChannels[i].Id)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        // Entferne Kanal aus der Liste.
                        ManagedChannels.RemoveAt(i);
                        --i;    // Evaluiere diesen Index im nächsten Schleifendurchlauf nochmals.
                    }
                }

                Debug.WriteLine("updateViewModelChannelList: Finished! List contains now {0} items.", ManagedChannels.Count);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("updateViewModelChannelList: Error occurred.");
                Debug.WriteLine("updateViewModelChannelList: Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiert die Liste an vom Moderator verwalteten Kanäle nach einer
        /// Änderung der kanalspezifischen oder listenspezifischen Anwendungseinstellungen. Die Einträge der Liste
        /// müssen entsprechend der neuen Einstellungen neu angeordnet werden. In diesem Fall werden die Einträge neu
        /// geladen und die Liste neu initialisiert.
        /// </summary>
        /// <param name="currentSettings">Die aktuellen Anwendungseinstellungen, die nach der Änderung gelten.</param>
        private async Task updateViewModelChannelListOnSettingsChange(AppSettings currentSettings)
        {
            Moderator activeModerator = channelController.GetLocalModerator();
            if (activeModerator == null)
                return;

            List<Channel> managedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

            // Sortiere nach aktuellen Anwendungseinstellungen.
            managedChannelList = sortChannelsByApplicationSetting(managedChannelList);

            // Lade die komplette Collection neu.
            ManagedChannels = new ObservableCollection<Channel>(managedChannelList);

            cachedGeneralListSettings = currentSettings.GeneralListOrderSetting;
            cachedChannelOrderSettings = currentSettings.ChannelOderSetting;

            // Füge Kanäle noch dem Lookup-Verzeichnis hinzu.
            currentChannels.Clear();
            foreach (Channel channel in managedChannelList)
            {
                currentChannels.Add(channel.Id, channel);
            }
        } 

        /// <summary>
        /// Aktualisiert nur die Properties, welche für die View aktuell relevant sind, also Properties, die
        /// per Databinding an die View gebunden sind. Aktualisiert dabei die 
        /// </summary>
        /// <param name="updatableChannel">Das zu aktualisierende Channel Objekt.</param>
        /// <param name="newChannel">Das Channel Objekt mit den neuen Daten.</param>
        private void updateViewRelatedPropertiesOfChannel(Channel updatableChannel, Channel newChannel)
        {
            updatableChannel.Name = newChannel.Name;
            updatableChannel.Term = newChannel.Term;
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
        /// Führt den Befehl ChannelSelected aus. Es wird auf die Detailseite des gewählten Kanals navigiert.
        /// </summary>
        /// <param name="selectedChannel">Der gewählte Listeneintrag</param>
        private void executeChannelSelected(object selectedChannel)
        {
            Channel channel = selectedChannel as Channel;
            if (channel != null)
            {
                _navService.Navigate("ModeratorChannelDetails", channel.Id);
            }
        }

        /// <summary>
        /// Führt den Befehl SwitchToChannelAddDialogCommand aus. Navigation zum "Kanal erstellen" Dialog.
        /// </summary>
        private void executeSwitchToChannelAddDialogCommand()
        {
            _navService.Navigate("AddAndEditChannel");
        }
    }
}
