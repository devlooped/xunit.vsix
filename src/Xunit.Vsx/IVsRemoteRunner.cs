using System.Diagnostics;
using Xunit.Sdk;

namespace Xunit
{
	interface IVsRemoteRunner
	{
		void AddListener (TraceListener listener);
		bool ShouldRestart ();
		VsxRunSummary Run (VsxTestCase testCase, IMessageBus messageBus, object[] constructorArguments);
	}
}
