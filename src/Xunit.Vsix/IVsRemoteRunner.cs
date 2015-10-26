using System;
using System.Diagnostics;
using Xunit.Sdk;

namespace Xunit
{
	interface IVsRemoteRunner : IDisposable
	{
		void AddListener (TraceListener listener);

		VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus);
	}
}
