using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// A VSIX test collection represents the set of tests to run against
	/// a particular IDE/RootSuffix combination.
	/// </summary>
	class VsixTestCollectionRunner : XunitTestCollectionRunner, IDisposable
	{
		IMessageSink diagnosticMessageSink;
		string vsVersion;
		string rootSuffix;
		IVsClient vs;

		public VsixTestCollectionRunner (VsixTestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink,
			IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) :
			base (testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
		{
			this.diagnosticMessageSink = diagnosticMessageSink;

			vsVersion = testCollection.VisualStudioVersion;
			rootSuffix = testCollection.RootSuffix;
			vs = new VsClient (vsVersion, rootSuffix, testCollection.Settings);
		}

		protected override Task<RunSummary> RunTestClassAsync (ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
		{
            return new VsixTestClassRunner(vs, testClass, @class, testCases, diagnosticMessageSink, MessageBus, TestCaseOrderer,
				Aggregator, CancellationTokenSource, CollectionFixtureMappings).RunAsync();
		}

		public void Dispose ()
		{
			vs.Dispose ();
		}
	}
}
