using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataHandlingLayer.Controller.ValidationErrorReportInterface
{
    public interface IValidationErrorReport
    {
        void ReportValidationError(string property, string failureMessage);
        void RemoveFailureMessagesForProperty(string property);
        void RemoveAllFailureMessages();
    }
}
