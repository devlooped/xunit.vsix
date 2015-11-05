using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsixTestMethodRunner : XunitTestMethodRunner
	{
		readonly IVsClient vsClient;


		public VsixTestMethodRunner (IVsClient vsClient, ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
			: base (testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, new object[0])
		{
			this.vsClient = vsClient;
		}

		protected override async Task<RunSummary> RunTestCaseAsync (IXunitTestCase testCase)
		{
			var vsixTest = (VsixTestCase)testCase;
			// We don't apply retry behavior when a debugger is attached, since that
			// typically means the developer is actually debugging a failing test.
			if (Debugger.IsAttached || vsixTest.RecycleOnFailure == false) {
				return await vsClient.RunAsync (vsixTest, MessageBus, Aggregator);
			}

			var bus = new BufferingMessageBus();
			var summary = await vsClient.RunAsync(vsixTest, bus, Aggregator);

			if (summary.Failed != 0) {
				vsClient.Recycle ();
				summary = await vsClient.RunAsync (vsixTest, MessageBus, Aggregator);
			} else {
				// Dispatch messages from the first run to actual bus.
				bus.messages.ForEach (msg => MessageBus.QueueMessage (msg));
			}

			return summary;
		}

		protected override void BeforeTestMethodFinished ()
		{
			base.BeforeTestMethodFinished ();
			if (Aggregator.HasExceptions && Aggregator.ToException () is TimeoutException)
				vsClient.Recycle ();
		}

		class BufferingMessageBus : IMessageBus
		{
			public List<IMessageSinkMessage> messages = new List<IMessageSinkMessage>();

			public void Dispose ()
			{
			}

			public bool QueueMessage (IMessageSinkMessage message)
			{
				messages.Add (message);
				return true;
			}
		}
	}
}
