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
            return new VsixTestMethodRunner(vsClient, testMethod, Class, method, testCases, DiagnosticMessageSink, MessageBus, new ExceptionAggregator(Aggregator), CancellationTokenSource).RunAsync();
		}

		protected override void CreateClassFixture (Type fixtureType)
		{
			// NOTE: we also never create the class fixture type in the calling app domain, for the same reason as below.
		}

		protected override object[] CreateTestClassConstructorArguments ()
		{
			// NOTE: we never create these arguments in the calling app domain. The VsRemoteRunner uses the XunitTestClassRunner
			// in the remove VS instance to do this automatically on the proper site.
			return new object[0];
		}
	}
}
