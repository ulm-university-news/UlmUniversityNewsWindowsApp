﻿using DataHandlingLayer.ErrorMapperInterface;
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
    public class HomescreenViewModel : ChannelEnumerationBaseViewModel
    {
        #region Fields
        /// <summary>
        /// Referenz auf den GroupController.
        /// </summary>
        private GroupController groupController;

        /// <summary>
        /// Lookup Verzeichnis für Gruppen. Enthält Gruppen,
        /// die aktuell an die View gebunden sind.
        /// </summary>
        private Dictionary<int, Group> currentGroups;

        // Speichert die AppSettings, die zum Zeitpunkt des Ladens der Kanäle aktuell gültig sind.
        // Wird benötigt, um zu prüfen, ob bei geänderten Einstellungen die Liste der Kanäle neu 
        // sortiert werden muss.
        private OrderOption cachedGeneralListSettings;
        private OrderOption cachedChannelOrderSettings;
        // Gleiches gilt für Gruppen.
        private OrderOption cachedGroupOrderSettings;
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

        private ObservableCollection<Group> groupCollection;
        /// <summary>
        /// Liste von Gruppenobjekten, in denen der lokale Nutzer Teilnehmer ist.
        /// </summary>
        public ObservableCollection<Group> GroupCollection
        {
            get { return groupCollection; }
            set { this.setProperty(ref this.groupCollection, value); }
        }

        private User localUser;
        /// <summary>
        /// Das Nutzerobjekt des aktuell angemeldeten Nutzers.
        /// </summary>
        public User LocalUser
        {
            get { return localUser; }
            set { localUser = value; }
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

        private RelayCommand groupSelected;
        /// <summary>
        /// Es wurde eine Gruppe ausgewählt, zu der nun die Gruppendetails angezeigt werden sollen.
        /// </summary>
        public RelayCommand GroupSelected
        {
            get { return groupSelected; }
            set { groupSelected = value; }
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
            groupController = new GroupController();

            // Initialisiere die Befehle.
            searchChannelsCommand = new RelayCommand(param => executeSearchChannelsCommand(), param => canSearchChannels());
            addGroupCommand = new RelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
            searchGroupsCommand = new RelayCommand(param => executeSearchGroupsCommand(), param => canSearchGroups());
            channelSelected = new RelayCommand(param => executeChannelSelected(param), param => canSelectChannel());
            groupSelected = new RelayCommand(param => executeGroupSelected(param), param => canSelectGroup());
        }

        /// <summary>
        /// Lädt die Kanäle, die der lokale Nutzer abonniert hat und macht diese über die MyChannels Property abrufbar.
        /// </summary>
        public async Task LoadMyChannelsAsync()
        {
            List<Channel> channels;
            try
            {
                if (ChannelCollection == null || ChannelCollection.Count == 0)
                {
                    Debug.WriteLine("Loading subscribed channels from DB.");

                    // Frage abonnierte Kanäle ab.
                    channels = await Task.Run(() => channelController.GetMyChannels());

                    // Sortiere Liste nach aktuellen Anwendungseinstellungen.
                    channels = await Task.Run(() => sortChannelsByApplicationSetting(channels));

                    // Mache Kanäle über Property abrufbar.
                    ChannelCollection = new ObservableCollection<Channel>(channels);

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
                        // Restrukturierung der Liste abonnierter Kanäle durch neu laden.
                        channels = await Task.Run(() => channelController.GetMyChannels());
                        await reloadChannelCollectionCompletelyAsync(channels);

                        // Aktualisiere die nun für die View geltenden Einstellungen.
                        cachedGeneralListSettings = currentAppSettings.GeneralListOrderSetting;
                        cachedChannelOrderSettings = currentAppSettings.ChannelOderSetting;
                    }
                    else
                    {
                        // Führe nur lokale Synchronisation durch.
                        channels = await Task.Run(() => channelController.GetMyChannels());
                        await updateViewModelChannelCollectionAsync(channels);
                    }             

                    // Führe Aktualisierung von Anzahl ungelesener Nachrichten Properties der Kanäle aus.
                    await UpdateNumberOfUnreadAnnouncementsAsync();
                }
            }
            catch(ClientException e)
            {
                Debug.WriteLine("Error during loading process of subscribed channels.");
                displayError(e.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiere die Werte für die noch ungelesenen Announcements in jedem Kanal aus 
        /// der "ChannelCollection" Liste.
        /// </summary>
        public async Task UpdateNumberOfUnreadAnnouncementsAsync()
        {
            // Delegiere an Hintergrundthread.
            Dictionary<int, int> numberOfUnreadAnnouncements =
                await Task.Run(() => channelController.GetAmountOfUnreadAnnouncementsForMyChannels());

            if (numberOfUnreadAnnouncements != null)
            {
                // Debug.WriteLine("NumberOfUnreadAnnouncements contains {0} entries.", numberOfUnreadAnnouncements.Count);

                foreach (Channel channel in ChannelCollection)
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
                        // Debug.WriteLine("Channel with id {0} did not appear in NumberOfUnreadAnnouncements, set nrOfUnreadMsg to 0.",
                        // channel.Id);
                        channel.NumberOfUnreadAnnouncements = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Lädt die Gruppen, in denen der lokale Nutzer Teilnehmer ist und 
        /// macht diese über Properties zugreifbar.
        /// </summary>
        public async Task LoadMyGroupsAsync()
        {
            if (LocalUser == null)
            {
                // Setze lokales Nutzerobjekt.
                LocalUser = groupController.GetLocalUser();
            }

            List<Group> groups = null;
            if (GroupCollection == null || GroupCollection.Count == 0)
            {
                Debug.WriteLine("LoadMyGroupsAsync: Not from cache. Load groups from DB.");

                // Frage Gruppen aus der Datenbank ab.
                groups = await Task.Run(() => groupController.GetAllGroups());
                Debug.WriteLine("There are {0} group elements in the list.", groups.Count);

                // Sortiere Gruppen anhand von aktuellen Anwendungseinstellungen.
                groups = await Task.Run(() => sortGroupsByApplicationSettings(groups));

                // Mache Gruppen über Property abrufbar.
                GroupCollection = new ObservableCollection<Group>(groups);

                // Speichere die aktuell gültigen Anwendungseinstellungen zwischen.
                AppSettings currentSettings = groupController.GetApplicationSettings();
                cachedGroupOrderSettings = currentSettings.GroupOrderSetting;

                // Füge Gruppen dem Lookup Verzeichnis hinzu.
                foreach (Group group in groups)
                {
                    currentGroups.Add(group.Id, group);
                }
            }
            else
            {
                Debug.WriteLine("LoadMyGroupsAsync: Seems like page comes from cache.");

                // TODO
            }
        }

        /// <summary>
        /// Sortiere die Gruppen anhand der aktuellen Anwendungseinstellungen.
        /// </summary>
        /// <param name="groups">Die zu sortierende Liste an Gruppen-Objekten.</param>
        /// <returns>Eine sortierte Liste von Gruppen.</returns>
        private List<Group> sortGroupsByApplicationSettings(List<Group> groups)
        {
            // TODO
            return groups;
        }

        #region CommandFunctionality
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
        /// Wechsle auf den Dialog zum Hinzufügen einer Gruppe.
        /// </summary>
        private void executeAddGroupCommand()
        {
            _navService.Navigate("AddGroup");
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

        /// <summary>
        /// Zeigt an, ob aktuell eine Gruppe ausgewählt werden kann.
        /// </summary>
        /// <returns></returns>
        private bool canSelectGroup()
        {
            if (selectedPivotItemIndex == 1)     // Aktiv, wenn "Meine Gruppen" Pivotitem aktiv ist.
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Bereite Anzeige der Gruppendetails für die ausgwählte Gruppe vor und löse Übergang
        /// auf Gruppendetails View aus.
        /// </summary>
        /// <param name="selectedGroupObj"></param>
        private void executeGroupSelected(object selectedGroupObj)
        {
            // TODO
        }
        #endregion CommandFunctionality
    }
}
