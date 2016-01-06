using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.DataModel
{
    /// <summary>
    /// Die Klasse PropertyChangedNotifier ist eine Hilfsklasse, die das INotifyPropertyChanged Interface
    /// implementiert. Es werden zudem Methoden bereitgestellt, um Änderungen an Properties über das 
    /// PropertyChanged Event bekanntzumachen.
    /// </summary>
    public class PropertyChangedNotifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        // Property Change Logik:
        /// <summary>
        /// Methode zum Setzen eines Property Wertes mit anschließender Benachrichtigung 
        /// möglicher Interessenten an der Änderung.
        /// </summary>
        /// <typeparam name="T">Der Typ des Property.</typeparam>
        /// <param name="storage"></param>
        /// <param name="value">Der neue Wert der Property.</param>
        /// <param name="propertyName">Der Name der Property.</param>
        /// <returns>Wahr, wenn der Wert der Property aktualisiert wurde.</returns>
        protected bool setProperty<T>(ref T storage, T value, [CallerMemberName] String propertyName = null)
        {
            if (object.Equals(storage, value))
            {
                return false;
            }
            storage = value;
            this.onPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Methode, die das PropertyChanged Event auslöst.
        /// </summary>
        /// <param name="propertyName">Der Name der Property, für die das Event ausgelöst wird.</param>
        protected void onPropertyChanged([CallerMemberName] String propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
