using System.Diagnostics;
using Xunit.Abstractions;

namespace Xunit
{
    public class TestOutputTraceListener : TraceListener
    {
        ITestOutputHelper _output;

        public TestOutputTraceListener(ITestOutputHelper output)
        {
            _output = output;
        }

        public override void Write(string message)
        {
            WriteLine(message);
        }

        public override void WriteLine(string message)
        {
            _output.WriteLine(message);
        }
    }
}
