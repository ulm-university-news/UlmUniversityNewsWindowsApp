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
                    Debug.WriteLine("We already have managed channels from cache.");

                    // TODO - Application settings changed case

                    // Frage nur die verwalteten Kanäle von der lokalen DB ab und schaue, ob Aktualisierungen der ManagedChannelsList notwendig sind.
                    List<Channel> managedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

                    updateManagedChannelList(managedChannelList);
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during loading of managedChannels.");
                displayError(ex.ErrorCode);
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

                // Aktualisiere zunächst die lokalen Kanaldatensätze.
                await Task.Run(() => channelController.UpdateChannels(managedChannelsServer));

                // Aktualisiere die Beziehungen Moderator-Kanal für die verantwortlichen Moderatoren.
                await Task.Run(() => channelController.UpdateManagedChannelsRelationships(managedChannelsServer));
                
                Debug.WriteLine("Finished the updating process of channel moderator relationships.");
                
                // Aktualisiere ManagedChannels Liste.
                updateManagedChannelList(managedChannelsServer);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during updating of channel and moderator relationships.");
                Debug.WriteLine("Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiert die ManagedChannel Liste auf Basis der übergebebenen Liste.
        /// Fügt neu hinzugekommene Kanäle hinzu und nimmt nicht mehr vorhandene Kanäle
        /// raus.
        /// </summary>
        /// <param name="updatedChannelList">Die aktualisierte Liste an Kanalressourcen, die als Referenz dient.</param>
        private void updateManagedChannelList(List<Channel> updatedChannelList)
        {
            Debug.WriteLine("Start the updateManagedChannelList method. Current amount of items " + 
                "is {0} in the ManagedChannels list.", ManagedChannels.Count);
            Moderator activeModerator = channelController.GetLocalModerator();

            try
            {
                updatedChannelList = sortChannelsByApplicationSetting(updatedChannelList);

                // Vergleiche, ob updatedChannelList Kanäle enthält, die noch nicht in ManagedChannels stehen.
                for (int i = 0; i < updatedChannelList.Count; i++)
                {
                    if (!currentChannels.ContainsKey(updatedChannelList[i].Id))
                    {
                        // Füge den Kanal der Liste hinzu.
                        currentChannels.Add(updatedChannelList[i].Id, updatedChannelList[i]);
                        ManagedChannels.Insert(i, updatedChannelList[i]);
                    }
                    else
                    {
                        // TODO - Prüfe, ob Aktualisierung der lokalen Kanal-Ressource erforderlich.
                    }
                }

                // Vergleiche, ob ManagedChannels Kanäle enthält, die nicht mehr in updatedChannelList stehen.
                for (int i = 0; i < ManagedChannels.Count; i++ )
                {
                    bool isContained = false;

                    foreach (Channel channel in updatedChannelList)
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

                Debug.WriteLine("Updated ManagedChannels list. It contains now {0} items.", ManagedChannels.Count);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred in updateManagedChannelList");
                Debug.WriteLine("Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);
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
