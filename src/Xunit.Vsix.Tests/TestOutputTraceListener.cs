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
		ITestOutputHelper output;

		public TestOutputTraceListener (ITestOutputHelper output)
		{
			this.output = output;
		}

		public override void Write (string message)
		{
			WriteLine (message);
		}

		public override void WriteLine (string message)
		{
			output.WriteLine (message);
		}
	}
}
