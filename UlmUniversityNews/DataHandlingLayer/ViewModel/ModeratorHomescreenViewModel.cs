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
    public class ModeratorHomescreenViewModel : ChannelEnumerationBaseViewModel
    {
        #region Fields
        // Speichert die AppSettings, die zum Zeitpunkt des Ladens der Kanäle aktuell gültig sind.
        // Wird benötigt, um zu prüfen, ob bei geänderten Einstellungen die Liste der Kanäle neu 
        // sortiert werden muss.
        private OrderOption cachedGeneralListSettings;
        private OrderOption cachedChannelOrderSettings;
        #endregion Fields

        #region Properties
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

        private AsyncRelayCommand synchronizeManagedChannelsCommand;
        /// <summary>
        /// Befehl, um die Synchronisation der lokalen Daten bezüglich der verwalteten
        /// Kanäle mit den Serverdaten zu synchronisieren.
        /// </summary>
        public AsyncRelayCommand SynchronizeManagedChannelsCommand
        {
            get { return synchronizeManagedChannelsCommand; }
            set { synchronizeManagedChannelsCommand = value; }
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
            // Erzeuge Befehle.
            ChannelSelected = new RelayCommand(param => executeChannelSelected(param));
            SwitchToAddChannelDialogCommand = new RelayCommand(param => executeSwitchToChannelAddDialogCommand());
            SynchronizeManagedChannelsCommand = new AsyncRelayCommand(param => executeSynchronizeManagedChannelsCommand());
        }

        /// <summary>
        /// Lade die Kanäle, für die der eingeloggte Moderator als Verantwortlicher eingetragen ist.
        /// </summary>
        public async Task LoadManagedChannelsAsync()
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
                if (ChannelCollection == null || ChannelCollection.Count == 0)
                {
                    Debug.WriteLine("The managed channels need to be loaded completely.");

                    List<Channel> managedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

                    // Sortiere nach aktuellen Anwendungseinstellungen.
                    managedChannelList = await Task.Run(() => sortChannelsByApplicationSetting(managedChannelList));

                    ChannelCollection = new ObservableCollection<Channel>(managedChannelList);

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
                    await updateChannelModeratorRelationshipsAsync(false);
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
                        // Lade zunächst die aktuellsten lokal gehaltenen Daten.
                        List<Channel> managedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));

                        await reloadChannelCollectionCompletelyAsync(managedChannelList);

                        // Aktualisiere die nun für die View geltenden Einstellungen.
                        cachedGeneralListSettings = currentAppSettings.GeneralListOrderSetting;
                        cachedChannelOrderSettings = currentAppSettings.ChannelOderSetting;
                    }
                    else
                    {
                        // Synchronisiere nur die im ViewModel gehaltene Liste von Kanälen mit den
                        // lokal im System vorhanden Kanälen.
                        List<Channel> localManagedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));
                        await updateViewModelChannelCollectionAsync(localManagedChannelList);
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
        /// Stößt die Aktualisierung der Kanal-Moderatoren Beziehungen an.
        /// Fragt dabei die aktuellste Liste an verantwortlichen Kanälen vom Server ab
        /// und stößt die Aktualisierung der lokalen Datenbankeinträge an. Aktualisiert
        /// anschließend die ManagedChannelsListe.
        /// </summary>
        /// <param name="displayErrors">Gibt an, ob ein möglicherweise auftretender Fehler dem Nutzer 
        ///     angezeigt werden soll.</param>
        private async Task updateChannelModeratorRelationshipsAsync(bool displayErrors)
        {
            Moderator activeModerator = channelController.GetLocalModerator();
            if (activeModerator == null)
                return;

            displayIndeterminateProgressIndicator();
            try
            {               
                // Frage Daten vom Server ab und aktualisiere lokale Datensätze.
                List<Channel> managedChannelsServer = await channelController.RetrieveManagedChannelsFromServerAsync(activeModerator.Id);

                // Aktualisiere die Beziehungen Moderator-Kanal für die verantwortlichen Moderatoren.
                await Task.Run(() => channelController.SynchronizeLocalManagedChannels(
                    channelController.GetLocalModerator(),
                    managedChannelsServer));
                                
                // Aktualisiere ManagedChannels Liste.
                List<Channel> localManagedChannelList = await Task.Run(() => channelController.GetManagedChannels(activeModerator.Id));
                await updateViewModelChannelCollectionAsync(localManagedChannelList);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error occurred during updating of channel and moderator relationships.");
                Debug.WriteLine("Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);

                if (displayErrors)
                {
                    displayError(ex.ErrorCode);
                }
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        #region CommandFunctions
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

        /// <summary>
        /// Führt den Befehl SynchronizeManagedChannelsCommand aus. Stößt die Synchronisation
        /// der lokalen Daten bezüglich der verwalteten Kanäle eines Moderators mit den Serverdaten
        /// an.
        /// </summary>
        private async Task executeSynchronizeManagedChannelsCommand()
        {
            // Stoße die Synchronisation an.
            await updateChannelModeratorRelationshipsAsync(true);
        }
        #endregion CommandFunctions
    }
}
