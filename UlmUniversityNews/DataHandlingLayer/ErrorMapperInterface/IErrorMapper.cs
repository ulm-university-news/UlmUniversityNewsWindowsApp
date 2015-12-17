using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.ErrorMapperInterface
{
    public interface IErrorMapper
    {
        void DisplayErrorMessage(int errorCode);
    }
}
