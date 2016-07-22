using DataHandlingLayer.Controller;
using DataHandlingLayer.DataModel;
using DataHandlingLayer.DataModel.Enums;
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

namespace DataHandlingLayer.ViewModel
{
    public class ChannelEnumerationBaseViewModel : ViewModel
    {
        #region Fields
        /// <summary>
        /// Eine Referenz auf eine Instanz der Klasse ChannelController.
        /// </summary>
        protected ChannelController channelController;

        /// <summary>
        /// Lookup Tabelle für Kanäle, in der alle aktuell im ViewModel verwalteten Kanäle gespeichert werden.
        /// </summary>
        protected Dictionary<int, Channel> currentChannels;
        #endregion Fields

        #region Properties
        private ObservableCollection<Channel> channelCollection;
        /// <summary>
        /// Eine Auflistung von Kanal-Objekten.
        /// </summary>
        public ObservableCollection<Channel> ChannelCollection
        {
            get { return channelCollection; }
            set { this.setProperty(ref this.channelCollection, value); }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse ChannelEnumerationBaseViewModel.
        /// </summary>
        /// <param name="navService">Eine Referenz auf den Navigationsdienst der Anwendung.</param>
        /// <param name="errorMapper">Eine Referenz auf den Fehlerdienst der Anwendung.</param>
        protected ChannelEnumerationBaseViewModel(INavigationService navService, IErrorMapper errorMapper)
            : base (navService, errorMapper)
        {
            channelController = new ChannelController(this);
            currentChannels = new Dictionary<int, Channel>();
        }

        /// <summary>
        /// Aktualisiert den Zustand der View im Falle eines eingehenden Events, welches
        /// über die Löschung eines Kanals informiert. Schaut, ob der betroffene Kanal in der 
        /// Auflistung ist und löst eine Aktualisierung der Anzeige aus.
        /// </summary>
        /// <param name="channelId">Die Id des betroffenen Kanals.</param>
        public void PerformViewUpdateOnChannelDeletedEvent(int channelId)
        {
            // Redraw herbeiführen durch Löschen und Wiedereinfügen.
            Channel affectedChannel = ChannelCollection.Where(channel => channel.Id == channelId).FirstOrDefault();
            if (affectedChannel != null)
            {
                int index = ChannelCollection.IndexOf(affectedChannel);
                Debug.WriteLine("It seems that the channel with id {0} is deleted on the server.", affectedChannel.Id);

                ChannelCollection.RemoveAt(index);
                affectedChannel.Deleted = true;
                ChannelCollection.Insert(index, affectedChannel);
            }
        }

        /// <summary>
        /// Aktualisiert den Zustand der View im Falle eines eingehenden Events, welches über die
        /// Änderung eines Kanals informiert. Schaut, ob der betroffene Kanal in der Auflistung ist und löst
        /// eine Aktualisierung der Anzeige aus.
        /// </summary>
        /// <param name="channelId">Die Id des betroffenen Kanals.</param>
        public async Task PerformViewUpdateOnChannelChangedEventAsync(int channelId)
        {
            // Aktualisiere die für die View relevanten Properties.
            Channel affectedChannel = ChannelCollection.Where(channel => channel.Id == channelId).FirstOrDefault();
            if (affectedChannel != null)
            {
                try
                {
                    Channel latestLocalChannel = await Task.Run(() => channelController.GetChannel(channelId));
                    updateViewRelatedPropertiesOfChannel(affectedChannel, latestLocalChannel);
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
        /// Lädt die Collection von Kanal-Objekten neu mit den Daten aus der übergebenen Liste
        /// von Kanal-Objekten.
        /// </summary>
        /// <param name="newChannels">Die Datensätze, mit denen die Collection neu geladen wird.</param>
        protected async Task reloadChannelCollectionCompletelyAsync(List<Channel> newChannels)
        {
            // Sortiere eingegebene Daten nach aktuellen Anwendungseinstellungen.
            newChannels = await Task.Run(() => sortChannelsByApplicationSetting(newChannels));

            // Lade die Collection neu und füge Kanäle dem Lookup Verzeichnis hinzu.
            // Muss auf dem UI Thread erfolgen, da Collection an View gebunden ist.
            ChannelCollection.Clear();
            currentChannels.Clear();
            foreach (Channel channel in newChannels)
            {
                ChannelCollection.Add(channel);
                currentChannels.Add(channel.Id, channel);
            }
        }

        /// <summary>
        /// Aktualisiert die ChannelCollection basierend auf der übergebenen Referenzliste.
        /// Fügt fehlende Kanäle hinzu oder nimmt solche Kanäle raus, die nicht mehr in der Referenzliste
        /// stehen. Aktualisiert die für die View relevanten Properties falls notwendig.
        /// </summary>
        /// <param name="referenceList">Die Referenzliste, basierend auf der die Aktualisierung erfolgt.</param>
        protected async Task updateViewModelChannelCollectionAsync(List<Channel> referenceList)
        {
            Debug.WriteLine("updateViewModelChannelCollectionAsync: Start method. Current amount of items " +
               "is {0} in the ChannelCollection collection.", ChannelCollection.Count);

            try
            {
                // Sortiere Referenzliste nach aktuellen Anwendungseinstellungen.
                referenceList = await Task.Run(() => sortChannelsByApplicationSetting(referenceList));

                // Vergleiche, ob referenceList Kanäle enthält, die noch nicht in ChannelCollection stehen.
                for (int i = 0; i < referenceList.Count; i++)
                {
                    if (!currentChannels.ContainsKey(referenceList[i].Id))
                    {
                        // Füge den Kanal der Liste hinzu.
                        currentChannels.Add(referenceList[i].Id, referenceList[i]);
                        ChannelCollection.Insert(i, referenceList[i]);
                    }
                    else
                    {
                        // Prüfe, ob Aktualisierung der lokalen Kanal-Ressource erforderlich.
                        Channel currentChannel = currentChannels[referenceList[i].Id];
                        if (DateTimeOffset.Compare(currentChannel.ModificationDate, referenceList[i].ModificationDate) < 0)
                        {
                            updateViewRelatedPropertiesOfChannel(currentChannel, referenceList[i]);
                        }

                        // Prüfe, ob Kanal als gelöscht markiert wurde.
                        if (referenceList[i].Deleted && !currentChannel.Deleted)
                        {
                            // Aktualisiere View.
                            PerformViewUpdateOnChannelDeletedEvent(currentChannel.Id);
                        }
                    }
                }

                // Vergleiche, ob ChannelCollection Kanäle enthält, die nicht mehr in referenceList stehen.
                for (int i = 0; i < ChannelCollection.Count; i++)
                {
                    bool isContained = false;

                    foreach (Channel channel in referenceList)
                    {
                        if (channel.Id == ChannelCollection[i].Id)
                        {
                            isContained = true;
                        }
                    }

                    if (!isContained)
                    {
                        // Entferne Kanal aus der Collection und dem Lookup-Verzeichnis.
                        currentChannels.Remove(ChannelCollection[i].Id);
                        ChannelCollection.RemoveAt(i);
                        --i;    // Evaluiere diesen Index im nächsten Schleifendurchlauf nochmals.
                    }
                }

                Debug.WriteLine("updateViewModelChannelCollectionAsync: Finished! List contains now {0} items.", ChannelCollection.Count);
            }
            catch (ClientException ex)
            {
                Debug.WriteLine("updateViewModelChannelCollectionAsync: Error occurred.");
                Debug.WriteLine("updateViewModelChannelCollectionAsync: Message is: {0}, Error Code is: {1}.", ex.Message, ex.ErrorCode);
            }
        }

        /// <summary>
        /// Aktualisiert nur die Properties, welche für die View aktuell relevant sind, also Properties, die
        /// per Databinding an die View gebunden sind. Aktualisiert dabei die 
        /// </summary>
        /// <param name="updatableChannel">Das zu aktualisierende Channel Objekt.</param>
        /// <param name="newChannel">Das Channel Objekt mit den neuen Daten.</param>
        protected void updateViewRelatedPropertiesOfChannel(Channel updatableChannel, Channel newChannel)
        {
            updatableChannel.Name = newChannel.Name;
            updatableChannel.Term = newChannel.Term;
        }

        /// <summary>
        /// Sortiert die Liste der Kanäle nach dem aktuell in den Anwendungseinstellungen festgelegten Kriterium.
        /// </summary>
        /// <param name="channels">Die Liste an zu sortierenden Kanälen.</param>
        /// <returns>Eine sortierte Liste der Kanäle.</returns>
        protected List<Channel> sortChannelsByApplicationSetting(List<Channel> channels)
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
                    // Extrahiere nur die Vorlesungen.
                    IEnumerable<Lecture> lectures = channels.Where(channel => channel.Type == ChannelType.LECTURE).Cast<Lecture>();
                    // Extrahiere die Kanäle anderer Kanaltypen.
                    IEnumerable<Channel> otherChannels = channels.Where(channel => channel.Type != ChannelType.LECTURE);

                    if (appSettings.GeneralListOrderSetting == OrderOption.ASCENDING)
                    {
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
                    }
                    else
                    {
                        // Sortiere die Vorlesungen.
                        lectures =
                            from lecture in lectures
                            orderby lecture.Faculty descending, lecture.Name descending
                            select lecture;

                        // Sortiere die anderen Kanaltypen.
                        otherChannels =
                            from channel in otherChannels
                            orderby channel.Type descending, channel.Name descending
                            select channel;
                    }

                    // Füge die beiden Listen zusammen.
                    channels = new List<Channel>();
                    foreach (Lecture lecture in lectures)
                    {
                        channels.Add(lecture);
                    }
                    foreach (Channel channel in otherChannels)
                    {
                        channels.Add(channel);
                    }

                    break;
                case OrderOption.BY_NEW_MESSAGES_AMOUNT:
                    if (appSettings.GeneralListOrderSetting == OrderOption.ASCENDING)
                    {
                        channels = new List<Channel>(
                            from item in channels
                            orderby item.NumberOfUnreadAnnouncements descending, item.Name ascending
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
    }
}
