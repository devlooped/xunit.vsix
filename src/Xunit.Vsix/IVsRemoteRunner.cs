using System.Diagnostics;
using Xunit.Sdk;

namespace Xunit
{
	interface IVsRemoteRunner
	{
		void AddListener (TraceListener listener);

		VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus);
	}
}
