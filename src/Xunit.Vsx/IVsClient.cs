using System;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Xunit
{
	interface IVsClient : IDisposable
	{
		void Shutdown ();

		Task<RunSummary> RunAsync (VsxTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator, object[] constructorArguments);
	}
}
