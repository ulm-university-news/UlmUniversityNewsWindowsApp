using DataHandlingLayer.CommandRelays.Interface;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.CommandRelays
{
    public class AsyncRelayCommand : IAsyncCommand
    {
        readonly Func<object, bool> _canExecute;
        bool _isExecuting;
        readonly Func<object, Task> _action;

        public event EventHandler CanExecuteChanged;

        /// <summary>
        /// Erstelle AsyncRelayCommand.
        /// </summary>
        /// <param name="action">Die Funktion, die durch das Kommando ausgelöst wird.</param>
        public AsyncRelayCommand(Func<object, Task> action)
        {
            _action = action;
        }

        /// <summary>
        /// Erstelle AsyncRelayCommand.
        /// </summary>
        /// <param name="action">Die Funktion, die durch das Kommando ausgelöst wird.</param>
        /// <param name="canExecute">Die Funktion, die bestimmt, ob das Kommando aktiviert ist oder deaktiviert.</param>
        public AsyncRelayCommand(Func<object, Task> action, Func<object, bool> canExecute)
            : this(action)
        {
            _canExecute = canExecute;
        }

        public async Task Execute(object parameter)
        {
            // Setze das isExecuting Feld, so dass keine weiteren Aufrufe auf dieses Kommando während der Ausführung erfolgen können.
            changeIsExecuting(true);
            try
            {
                await _action(parameter);
            }
            finally
            {
                // Setze isExecuting wieder auf false.
                changeIsExecuting(false);
            }
        }

        public bool CanExecute(object parameter)
        {
            var canExecuteResult = _canExecute == null || _canExecute(parameter);
            return !_isExecuting && canExecuteResult;
        }
        
        async void System.Windows.Input.ICommand.Execute(object parameter)
        {
            await Execute(parameter);
        }

        private void changeIsExecuting(bool newValue)
        {
            if(newValue == _isExecuting)
            {
                return;
            }
            _isExecuting = newValue;
            OnCanExecuteChanged();
        }

        public void OnCanExecuteChanged()
        {
            var handler = CanExecuteChanged;
            if(handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
