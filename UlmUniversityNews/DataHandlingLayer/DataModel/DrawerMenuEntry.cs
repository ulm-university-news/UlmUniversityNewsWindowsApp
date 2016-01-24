using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse DrawerMenuEntry repräsentiert einen Menüeintrag im Drawer Menü der Anwendung.
    /// </summary>
    public class DrawerMenuEntry
    {
        #region Properties
        private string menuEntryName;
        /// <summary>
        /// Der Name des Menüeintrags.
        /// </summary>
        public string MenuEntryName
        {
            get { return menuEntryName; }
            set { menuEntryName = value; }
        }

        private string displayableNameResourceKey;
        /// <summary>
        /// Der Schlüssel auf einen Eintrag in den Resource Dateien, der den
        /// String identifiziert, der schlussendlich auf dem Bildschirm angezeigt wird.
        /// </summary>
        public string DisplayableNameResourceKey
        {
            get { return displayableNameResourceKey; }
            set { displayableNameResourceKey = value; }
        }
        

        private string iconPath;
        /// <summary>
        /// Der Pfad zum Icon dieses Menüeintrags.
        /// </summary>
        public string IconPath
        {
            get { return iconPath; }
            set { iconPath = value; }
        }

        private string referencedPageKey;
        /// <summary>
        /// Der Schlüssel, der die Seite identifiziert, auf die dieser Menüeintrag verweist.
        /// </summary>
        public string ReferencedPageKey
        {
            get { return referencedPageKey; }
            set { referencedPageKey = value; }
        }
        #endregion Properties

        /// <summary>
        /// Erzeugt eine Instanz der Klasse DrawerMenuEntry.
        /// </summary>
        public DrawerMenuEntry()
        {

        }
    }
}
