using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsixTestClassRunner : XunitTestClassRunner
	{
		IVsClient vsClient;

		public VsixTestClassRunner (IVsClient vsClient,
								   ITestClass testClass,
								   IReflectionTypeInfo @class,
								   IEnumerable<IXunitTestCase> testCases,
								   IMessageSink diagnosticMessageSink,
								   IMessageBus messageBus,
								   ITestCaseOrderer testCaseOrderer,
								   ExceptionAggregator aggregator,
								   CancellationTokenSource cancellationTokenSource,
								   IDictionary<Type, object> collectionFixtureMappings)
			: base (testClass, @class, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource, collectionFixtureMappings)
		{
			this.vsClient = vsClient;
		}

		protected override Task<RunSummary> RunTestMethodAsync (ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
		{
            return new VsixTestMethodRunner(vsClient, testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource, constructorArguments).RunAsync();
		}
	}
}
