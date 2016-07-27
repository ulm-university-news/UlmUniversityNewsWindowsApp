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

        private bool hasSynchronizeAllGroupsOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglickeit hat, die Synchronisation aller Gruppen auszuführen,
        /// in denen er Teilnehmer ist.
        /// </summary>
        public bool HasSynchronizeAllGroupsOption
        {
            get { return hasSynchronizeAllGroupsOption; }
            set { this.setProperty(ref this.hasSynchronizeAllGroupsOption, value); }
        }

        private bool hasSynchronizeAllChannelsOption;
        /// <summary>
        /// Gibt an, ob der Nutzer die Möglickeit hat, die Synchronisation aller Kanäle auszuführen,
        /// die er abonniert hat.
        /// </summary>
        public bool HasSynchronizeAllChannelsOption
        {
            get { return hasSynchronizeAllChannelsOption; }
            set { this.setProperty(ref this.hasSynchronizeAllChannelsOption, value); }
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

        private AsyncRelayCommand synchronizeAllGroupsCommand;
        /// <summary>
        /// Befehl zur Synchronisation aller lokal verwalteter Gruppen, in 
        /// denen der Nutzer Teilnehmer ist.
        /// </summary>
        public AsyncRelayCommand SynchronizeAllGroupsCommand
        {
            get { return synchronizeAllGroupsCommand; }
            set { synchronizeAllGroupsCommand = value; }
        }

        private AsyncRelayCommand synchronizeAllChannelsCommand;
        /// <summary>
        /// Befehl zur Synchronisation aller lokal verwalteten Kanäle, die
        /// der Nutzer abonniert hat.
        /// </summary>
        public AsyncRelayCommand SynchronizeAllChannelsCommand
        {
            get { return synchronizeAllChannelsCommand; }
            set { synchronizeAllChannelsCommand = value; }
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
            currentGroups = new Dictionary<int, Group>();

            // Initialisiere die Befehle.
            SearchChannelsCommand = new RelayCommand(param => executeSearchChannelsCommand(), param => canSearchChannels());
            AddGroupCommand = new RelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
            SearchGroupsCommand = new RelayCommand(param => executeSearchGroupsCommand(), param => canSearchGroups());
            ChannelSelected = new RelayCommand(param => executeChannelSelected(param), param => canSelectChannel());
            GroupSelected = new RelayCommand(param => executeGroupSelected(param), param => canSelectGroup());
            SynchronizeAllGroupsCommand = new AsyncRelayCommand(param => executeSynchronizeAllGroupsCommandAsync(), param => canSynchronizeAllGroups());
            SynchronizeAllChannelsCommand = new AsyncRelayCommand(param => executeSynchronizeAllChannelsCommandAsync(), param => canSynchronizeAllChannels());
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

            checkCommandExecution();
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
            try
            {
                if (GroupCollection == null || GroupCollection.Count == 0)
                {
                    Debug.WriteLine("LoadMyGroupsAsync: Not from cache. Load groups from DB.");

                    // Frage Gruppen aus der Datenbank ab.
                    groups = await Task.Run(() => groupController.GetAllGroups());
                    Debug.WriteLine("LoadMyGroupsAsync: There are {0} group elements in the list.", groups.Count);

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

                    // Prüfe zunächst, ob die Einstellungen aktualisiert wurden.
                    AppSettings currentAppSettings = channelController.GetApplicationSettings();
                    if (currentAppSettings.GroupOrderSetting != cachedGroupOrderSettings)
                    {
                        Debug.WriteLine("LoadMyGroupsAsync: Reloading completely.");
                        // Restrukturierung der Liste von Gruppen durch neu laden.
                        List<Group> localGroups = await Task.Run(() => groupController.GetAllGroups());
                        await ReloadGroupCollectionCompletelyAsync(localGroups);

                        // Aktualisiere die nun für die View geltenden Einstellungen.
                        cachedGroupOrderSettings = currentAppSettings.GroupOrderSetting;
                    }
                    else
                    {
                        Debug.WriteLine("LoadMyGroupsAsync: Perform local sync.");
                        // Führe nur lokale Synchronisation durch.
                        Task<List<int>> localGroups = Task.Run(() => groupController.GetLocalGroupIdentifiers());
                        List<Group> modifiedGroups = await Task.Run(() => groupController.GetDirtyGroups());
                        List<int> localGroupSnapshot = await localGroups;
                        await updateViewModelGroupCollectionAsync(modifiedGroups, localGroupSnapshot);

                        // Setze Dirty-Flag zurück.
                        groupController.ResetDirtyFlagsOnGroups();
                    }
                }
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("Error during loading process of my groups");
                displayError(ex.ErrorCode);
            }

            checkCommandExecution();
        }

        /// <summary>
        /// Aktualisiert die Group Collection, die im ViewModel gehalten wird.
        /// <param name="modifiedGroups">Die Gruppen, die seit dem letzten Vergleich geändert wurden.</param>
        /// <param name="localGroupSnapshot">Ein Snapshot über die Ids der aktuell im System verwalten Gruppen.</param>
        /// </summary>
        private async Task updateViewModelGroupCollectionAsync(List<Group> modifiedGroups, List<int> localGroupSnapshot)
        {
            bool requiresReload = false;

            Debug.WriteLine("updateViewModelGroupCollection: Start. Collection has {0} elements.", GroupCollection.Count);
            // Gehe geänderte Gruppen durch.
            foreach (Group group in modifiedGroups)
            {
                if (currentGroups.ContainsKey(group.Id))
                {
                    // Aktualisiere die für die View relevanten Properties.
                    updateViewRelatedGroupProperties(currentGroups[group.Id], group);
                    Debug.WriteLine("updateViewModelGroupCollection: Performed update on group with id {0}.", group.Id);

                    if (currentGroups[group.Id].Deleted != group.Deleted)
                    {
                        Debug.WriteLine("updateViewModelGroupCollection: Seems that a group has been removed. " + 
                            "The group with id {0} seems to be deleted.", group.Id);
                        // Lade Collection neu, dass die Icons aktualisiert werden.
                        requiresReload = true;
                    }
                }
                else
                {
                    // Füge hinzugekommene Gruppe der Aufzählung hinzu. 
                    GroupCollection.Add(group);
                    currentGroups.Add(group.Id, group);
                    Debug.WriteLine("updateViewModelGroupCollection: Added group with id {0}.", group.Id);
                    // Änderung: 22.07.16 Aufgrund der Problematik bezüglich der Sortierung wird die Liste auch 
                    // in diesem Fall komplett neu geladen.
                    requiresReload = true;
                }
            }

            // Prüfe, ob Gruppen lokal entfernt wurden.
            List<Group> groups = currentGroups.Values.ToList<Group>();
            for (int i=0; i < groups.Count; i++)
            {
                Group group = groups[i];
                if (!localGroupSnapshot.Contains(group.Id))
                {
                    // Entferne Gruppe von Collection.
                    GroupCollection.RemoveAt(i);
                    currentGroups.Remove(group.Id);
                    Debug.WriteLine("updateViewModelGroupCollection: Removed group with id {0}.", group.Id);
                }
            }

            // Wenn eine Gruppe als gelöscht erkannt wurde, dann lade die komplette Collection neu.
            // Ebenso, wenn eine neue Gruppe hinzugekommen ist.
            if (requiresReload)
            {
                // Restrukturierung der Liste von Gruppen durch neu laden.
                List<Group> localGroups = await Task.Run(() => groupController.GetAllGroups());
                await ReloadGroupCollectionCompletelyAsync(localGroups);
            }

            Debug.WriteLine("updateViewModelGroupCollection: Finished. Collection has {0} elements.", GroupCollection.Count);
        }

        /// <summary>
        /// Lädt die Collection von Gruppen-Objekten neu mit den Daten aus der übergebenen Liste
        /// von Gruppen-Objekten. Die Ausführung muss auf dem UI-Thread erfolgen.
        /// </summary>
        /// <param name="newGroups">Die Datensätze, mit denen die Collection neu geladen wird.</param>
        public async Task ReloadGroupCollectionCompletelyAsync(List<Group> newGroups)
        {
            // Sortiere eingegebene Daten nach aktuellen Anwendungseinstellungen.
            newGroups = await Task.Run(() => sortGroupsByApplicationSettings(newGroups));

            // Lade die Collection neu und füge Gruppen dem Lookup Verzeichnis hinzu.
            // Muss auf dem UI Thread erfolgen, da Collection an View gebunden ist.
            GroupCollection.Clear();
            currentGroups.Clear();
            foreach (Group group in newGroups)
            {
                GroupCollection.Add(group);
                currentGroups.Add(group.Id, group);
            }
        }

        /// <summary>
        /// Aktualisiert eine einzelne Gruppeninstanz, die in der an die View gebundenen Collection
        /// gespeichert ist. Es wird die Gruppeninstanz aktualisiert, die über die angegebenen Id identifiziert
        /// wird. Die Ausführung muss auf dem UI-Thread erfolgen.
        /// </summary>
        /// <param name="groupId">Die Id der Gruppe, die in der Collection aktualisiert werden soll.</param>
        public void UpdateIndividualGroupInCollection(int groupId)
        {
            Debug.WriteLine("UpdateIndividualGroupInCollection: Start with group id: {0}.", groupId);

            if (currentGroups.ContainsKey(groupId) &&
                GroupCollection != null && GroupCollection.Count > 0)
            {
                // Rufe die aktuellsten Gruppendaten ab.
                Group group = null;
                try
                {
                    group = groupController.GetGroup(groupId);
                }
                catch (ClientException ex)
                {
                    Debug.WriteLine("UpdateIndividualGroupInCollection: Failed to retrieve group. " + 
                        "Error code is {0}.", ex.ErrorCode);
                }

                if (group != null)
                {
                    // Suche den Index der Gruppe in der aktuellen Collection.
                    int index = GroupCollection.IndexOf(currentGroups[groupId]);

                    // Ersetze Gruppen Objekt in Collection.
                    GroupCollection.RemoveAt(index);
                    GroupCollection.Insert(index, group);

                    // Ersetze Gruppe auch in Lookup Verzeichnis.
                    currentGroups[groupId] = group;

                    Debug.WriteLine("UpdateIndividualGroupInCollection: Successfully updated individual group.");
                }                
            }
        }

        /// <summary>
        /// Aktualisiert die für die View relevanten Properties der aktuell
        /// verwalteten Gruppen-Instanz mit den Werten der übergebenen neuen
        /// Gruppen-Instanz.
        /// </summary>
        /// <param name="currentGroup">Die aktuell im ViewModel verwaltete Gruppe.</param>
        /// <param name="newGroup">Die neue Gruppen-Instanz.</param>
        private void updateViewRelatedGroupProperties(Group currentGroup, Group newGroup)
        {
            currentGroup.Name = newGroup.Name;
            currentGroup.Term = newGroup.Term;
            currentGroup.HasNewEvent = newGroup.HasNewEvent;
        }

        /// <summary>
        /// Sortiere die Gruppen anhand der aktuellen Anwendungseinstellungen.
        /// </summary>
        /// <param name="groups">Die zu sortierende Liste an Gruppen-Objekten.</param>
        /// <returns>Eine sortierte Liste von Gruppen.</returns>
        private List<Group> sortGroupsByApplicationSettings(List<Group> groups)
        {
            // Hole die Anwendungseinstellungen.
            AppSettings appSettings = channelController.GetApplicationSettings();

            switch (appSettings.GroupOrderSetting)
            {
                case OrderOption.ALPHABETICAL:
                    groups = new List<Group>(
                        from item in groups
                        orderby item.Name ascending
                        select item);
                    break;
                case OrderOption.BY_TYPE:
                    groups = new List<Group>(
                        from item in groups
                        orderby item.Type ascending, item.Name ascending 
                        select item);
                    break;
            }

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
            SynchronizeAllGroupsCommand.OnCanExecuteChanged();

            if (canSynchronizeAllGroups())
                HasSynchronizeAllGroupsOption = true;
            else
                HasSynchronizeAllGroupsOption = false;

            SynchronizeAllChannelsCommand.OnCanExecuteChanged();

            if (canSynchronizeAllChannels())
                HasSynchronizeAllChannelsOption = true;
            else
                HasSynchronizeAllChannelsOption = false;
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
            _navService.Navigate("AddAndEditGroup");
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
        /// Ausführung des Befehls SearchGroupsCommand. Wechsel auf die Gruppensuche Ansicht.
        /// </summary>
        private void executeSearchGroupsCommand()
        {
            if (_navService != null)
            {
                _navService.Navigate("SearchGroups");
            }
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
            Group selectedGroup = selectedGroupObj as Group;
            if (selectedGroup != null)
            {
                _navService.Navigate("GroupDetails", selectedGroup.Id);
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zur Synchronisation aller Gruppen aktuell zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeAllGroups()
        {
            if (SelectedPivotItemIndex == 1 && 
                GroupCollection != null && GroupCollection.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeAllGroupsCommand aus. Stößt die Synchronisation
        /// aller Gruppendaten an.
        /// </summary>
        private async Task executeSynchronizeAllGroupsCommandAsync()
        {
            try
            {
                displayIndeterminateProgressIndicator();

                await Task.Run(() => groupController.SynchronizeAllGroupsWithServer());

                // Restrukturierung der Liste von Gruppen durch neu laden.
                List<Group> localGroups = await Task.Run(() => groupController.GetAllGroups());

                foreach (Group localGroup in localGroups)
                {
                    Debug.WriteLine("executeSynchronizeAllGroupsCommand: HasNewEvent flag: {0}.", localGroup.HasNewEvent);
                }

                await ReloadGroupCollectionCompletelyAsync(localGroups);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeAllGroupsCommand: Sync failed. Error code is: {0}.", ex.ErrorCode);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }

        /// <summary>
        /// Gibt an, ob der Befehl zur Synchronisation aller abonnierten
        /// Kanäle zur Verfügung steht.
        /// </summary>
        /// <returns>Liefert true, wenn der Befehl zur Verfügung steht, ansonsten false.</returns>
        private bool canSynchronizeAllChannels()
        {
            if (SelectedPivotItemIndex == 0 && 
                ChannelCollection != null && ChannelCollection.Count > 0)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Führt den Befehl SynchronizeAllChannelsCommandAsync aus. Stößt die Synchronisation
        /// aller abonnierten Kanäle an.
        /// </summary>
        private async Task executeSynchronizeAllChannelsCommandAsync()
        {
            try
            {
                displayIndeterminateProgressIndicator();

                await Task.Run(() => channelController.SynchronizeAllChannelsAsync());

                // Lade Kanalliste neu.
                List<Channel> channels = await Task.Run(() => channelController.GetMyChannels());
                await reloadChannelCollectionCompletelyAsync(channels);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("executeSynchronizeAllChannelsCommandAsync: Sync failed. Error code is: {0}.", ex.ErrorCode);
                displayError(ex.ErrorCode);
            }
            finally
            {
                hideIndeterminateProgressIndicator();
            }
        }
        #endregion CommandFunctionality
    }
}
