using System.Diagnostics;
using Xunit.Sdk;

namespace Xunit
{
	interface IVsRemoteRunner
	{
		void AddListener (TraceListener listener);
		bool ShouldRestart ();
		VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus, object[] constructorArguments);
	}
}
