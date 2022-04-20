using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Xunit
{
    class TraceOutputMessage : DiagnosticMessage
    {
        public TraceOutputMessage(string message) : base(message)
        {
        }
    }
}
