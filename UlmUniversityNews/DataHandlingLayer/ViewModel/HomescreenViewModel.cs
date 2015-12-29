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
using DataHandlingLayer.Controller;
using DataHandlingLayer.Exceptions;

namespace DataHandlingLayer.ViewModel
{
    public class HomescreenViewModel : ViewModel
    {
        /// <summary>
        /// Eine Referenz auf eine Instanz des ChannelController.
        /// </summary>
        private ChannelController channelController;

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
                Debug.WriteLine("In setter method of selected pivot item index. The new index is: " + value);
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

            //// Start test
            //Lecture testChannel = new Lecture()
            //{
            //    Id = 1,
            //    Name = "MMK",
            //    Description = "Das ist der Kanal für die MMK Vorlesung",
            //    CreationDate = DateTime.Now,
            //    ModificationDate = DateTime.Now,
            //    Type = DataHandlingLayer.DataModel.Enums.ChannelType.LECTURE,
            //    Term = "SS2015",
            //    Locations = "H20 und H21",
            //    Dates = "Montag 14 - 16 Uhr und Mittwoch 12 - 14 Uhr",
            //    Contacts = "max-mustermann@uni-ulm.de",
            //    Website = "http://www.uni-ulm.de/in/vs/teach/mmk.html",
            //    Deleted = false,
            //    Faculty = DataModel.Enums.Faculty.ENGINEERING_COMPUTER_SCIENCE_PSYCHOLOGY,
            //    StartDate = "13.April.2015",
            //    EndDate = "16.Oktober.2015",
            //    Lecturer = "Professor Hauck",
            //    Assistant = "Mr Nobody"
            //};
            //channelController.storeTestChannel(testChannel);
            //// End test

            // Initialisiere die Kommandos.
            searchChannelsCommand = new RelayCommand(param => executeSearchChannelsCommand(), param => canSearchChannels());
            addGroupCommand = new RelayCommand(param => executeAddGroupCommand(), param => canAddGroup());
            searchGroupsCommand = new RelayCommand(param => executeSearchGroupsCommand(), param => canSearchGroups());
        }

        /// <summary>
        /// Lädt die Kanäle, die der lokale Nutzer abonniert hat und macht diese über die MyChannels Property abrufbar.
        /// </summary>
        public async Task LoadMyChannelsAsync()
        {
            List<Channel> channels;
            try
            {
                channels = await Task.Run(() => channelController.GetMyChannels());
                
                // Mache Kanäle über Property abrufbar.
                MyChannels = new ObservableCollection<Channel>(channels);
            }catch(ClientException e)
            {
                displayError(e.ErrorCode);
            }
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
            // TODO
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
    }
}
