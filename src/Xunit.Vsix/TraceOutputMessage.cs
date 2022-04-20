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
