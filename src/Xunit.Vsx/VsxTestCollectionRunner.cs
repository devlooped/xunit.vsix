using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// A VSX test collection represents the set of tests to run against 
	/// a particular IDE/RootSuffix combination. This runner takes care 
	/// of keeping that process running, recyling it when timeouts happen, 
	/// etc.
	/// </summary>
	class VsxTestCollectionRunner : XunitTestCollectionRunner, IDisposable
	{
		IMessageSink diagnosticMessageSink;
		string vsVersion;
		string rootSuffix;
		IVsClient vs;

		public VsxTestCollectionRunner (VsxTestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, 
			IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) :
			base (testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
		{
			this.diagnosticMessageSink = diagnosticMessageSink;

			vsVersion = testCollection.VisualStudioVersion;
			rootSuffix = testCollection.RootSuffix;
			vs = new VsClient (vsVersion, Guid.NewGuid ().ToString (), rootSuffix);
		}

		protected override Task<RunSummary> RunTestClassAsync (ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
		{
            return new VsxTestClassRunner(vs, testClass, @class, testCases, diagnosticMessageSink, MessageBus, TestCaseOrderer, 
				Aggregator, CancellationTokenSource, CollectionFixtureMappings).RunAsync();
		}

		public void Dispose ()
		{
			vs.Dispose ();
		}
	}
}
