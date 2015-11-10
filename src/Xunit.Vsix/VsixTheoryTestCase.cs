using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Represents a test case which runs multiple tests for theory data, either because the
	/// data was not enumerable or because the data was not serializable.
	/// </summary>
	class VsixTheoryTestCase : VsixTestCase
	{
		[EditorBrowsable (EditorBrowsableState.Never)]
		[Obsolete ("Called by the de-serializer; should only be called by deriving classes for de-serialization purposes")]
		public VsixTheoryTestCase () { }

		public VsixTheoryTestCase (IMessageSink diagnosticMessageSink, TestMethodDisplay testMethodDisplay, ITestMethod testMethod,
			string vsVersion, string rootSuffix, bool? newIdeInstance, int timeoutSeconds, bool? recycleOnFailure)
			: base (diagnosticMessageSink, testMethodDisplay, testMethod, vsVersion, rootSuffix, newIdeInstance, timeoutSeconds, recycleOnFailure)
		{
		}

		public override Task<RunSummary> RunAsync (IMessageSink diagnosticMessageSink,
												  IMessageBus messageBus,
												  object[] constructorArguments,
												  ExceptionAggregator aggregator,
												  CancellationTokenSource cancellationTokenSource)
		{
			return new XunitTheoryTestCaseRunner (this, DisplayName, SkipReason, constructorArguments, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource).RunAsync ();
		}
	}
}