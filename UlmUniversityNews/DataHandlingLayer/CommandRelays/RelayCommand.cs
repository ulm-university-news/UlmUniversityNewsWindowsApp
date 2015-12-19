using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataHandlingLayer.CommandRelays
{
    public class RelayCommand : ICommand
    {
        // Event, das gefeuert wird, wenn sich der Zustand von CanExecute geändert hat.
        public event EventHandler CanExecuteChanged;

        // Delegate für Methode, die bestimmt ob das Kommando aktiviert oder deaktiviert ist.
        protected readonly Func<object, bool> _canExecute;        // Func kapselt eine Methode, die einen Wert zurückliefert.

        // Delegate für Methode, die beim Ausführen des Kommandos aufgerufen werden soll.
        protected readonly Action<object> _execute;                  // Action kapselt eine Methode, die keinen Wert zurückliefert.

        /// <summary>
        /// Erzeugt eine Instanz der RelayCommand Klasse.
        /// </summary>
        /// <param name="executeMethod">Die Methode, die durch das Kommando ausgelöst werden soll.</param>
        /// <param name="canExecuteMethod">Die Methode, die bestimmt, ob das Kommando aktiviert oder deaktiviert ist.</param>
        public RelayCommand(Action<object> executeMethod, Func<object, bool> canExecuteMethod)
        {
            _execute = executeMethod;
            _canExecute = canExecuteMethod;
        }

        /// <summary>
        /// Erzeugt eine Instanz der RelayCommand Klasse.
        /// </summary>
        /// <param name="executeMethod">Die Methode, die durch das Kommando ausgelöst werden soll.</param>
        public RelayCommand(Action<object> executeMethod)
        {
            _execute = executeMethod;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object parameter)
        {
            // Rufe Methode auf, falls sie gesetzt wurde.
            if(_execute != null){
                _execute(parameter);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            if(CanExecuteChanged != null)
            {
                CanExecuteChanged(this, EventArgs.Empty);
            }
        }
    }
}
