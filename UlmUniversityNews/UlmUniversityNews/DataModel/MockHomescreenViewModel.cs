using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using UlmUniversityNews.DataModel.FakeDataForDesign;

namespace UlmUniversityNews.DataModel
{
    public class MockHomescreenViewModel
    {
        #region Properties
        private ObservableCollection<FakeChannel> myChannels;

        public ObservableCollection<FakeChannel> MyChannels
        {
            get { return myChannels; }
            set { myChannels = value; }
        }    
        #endregion Properties

        public MockHomescreenViewModel()
        {
            MyChannels.Add(new FakeChannel()
            {
                Id = 5,
                Name = "Mein TestKanal",
                Term = "WS 2015/16",
                NumberOfUnreadAnnouncements = 2,
                Description = "Ein neuer Testkanal",
                Deleted = false
            });
            MyChannels.Add(new FakeChannel()
            {
                Id = 6,
                Name = "Testkanal",
                Term = "SS2017",
                NumberOfUnreadAnnouncements = 4,
                Deleted = false,
                Contacts = "Kontakt",
                Dates = "Montag",
                Description = "Nur ein Testkanal",
                Website = "www und so"
            });
        }
    }
}
