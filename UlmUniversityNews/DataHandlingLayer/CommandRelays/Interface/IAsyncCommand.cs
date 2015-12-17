using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DataHandlingLayer.CommandRelays.Interface
{
    public interface IAsyncCommand : ICommand
    {
        Task Execute(object parameter);
    }
}
