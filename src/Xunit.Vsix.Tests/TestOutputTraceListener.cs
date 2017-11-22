using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit
{
    public class TestOutputTraceListener : TraceListener
    {
        private ITestOutputHelper _output;

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
