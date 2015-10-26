using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Remote runner instance running in the IDE AppDomain/process
	/// to execute tests on.
	/// </summary>
	class VsRemoteRunner : MarshalByRefObject, IVsRemoteRunner
	{
		string pipeName;
		IChannel channel;
		ConcurrentDictionary<ITestClass, VsRemoteTestClassRunner> classRunnerMap = new ConcurrentDictionary<ITestClass, VsRemoteTestClassRunner>();

		public VsRemoteRunner ()
		{
			pipeName = Environment.GetEnvironmentVariable (Constants.PipeNameEnvironmentVariable);

			RemotingServices.Marshal (this, RemotingUtil.HostName);
		}

		public void AddListener (TraceListener listener)
		{
			Trace.Listeners.Add (listener);
		}

		public VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus)
		{
			var classRunner = classRunnerMap.GetOrAdd(testCase.TestMethod.TestClass,
				testClass => new VsRemoteTestClassRunner(testClass, (IReflectionTypeInfo)testClass.Class));

			var aggregator = new ExceptionAggregator ();

			try {
				var result = classRunner.RunAsync(testCase, messageBus, aggregator)
					.Result
					.ToVsixRunSummary();

				//var result = new XunitTestCaseRunner (
				//		testCase, testCase.DisplayName, testCase.SkipReason,
				//		// NOTE: we create the test output helper always locally in the
				//		// remote process, since otherwise it isn't properly initialized.
				//		testCase.HasTestOutput ? new[] { new TestOutputHelper() } : new object[0],
				//		testCase.TestMethodArguments, messageBus,
				//		aggregator, new CancellationTokenSource ())
				//	.RunAsync ()
				//	.Result
				//	.ToVsixRunSummary ();

				if (aggregator.HasExceptions)
					result.Exception = aggregator.ToException ();

				return result;

			} catch (Exception) {
				throw;
			}
		}

		/// <summary>
		/// Invoked by the <see cref="VsStartup.Start"/> injected managed method in the
		/// VS process.
		/// </summary>
		public void Start ()
		{
			channel = RemotingUtil.CreateChannel (Constants.ServerChannelName, pipeName);
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}

		class VsRemoteTestClassRunner : XunitTestClassRunner
		{
			public VsRemoteTestClassRunner (ITestClass testClass, IReflectionTypeInfo @class)
				: base (testClass, @class, Enumerable.Empty<IXunitTestCase> (), new NullMessageSink (), null,
					  new DefaultTestCaseOrderer (new NullMessageSink ()), new ExceptionAggregator (), new CancellationTokenSource (), new Dictionary<Type, object> ())
			{
			}

			public Task<RunSummary> RunAsync (IXunitTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
			{
				TestCases = new[] { testCase };
				MessageBus = messageBus;
				Aggregator = aggregator;
				return RunAsync ();
			}

			protected override Task<RunSummary> RunTestMethodAsync (ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
			{
				// We don't want to run the discovery again over the test method,
				// generate new test cases and so on, since we already have received a single test case to run.
				return new XunitTestCaseRunner (
						testCases.Single (),
						testCases.Single ().DisplayName,
						testCases.Single ().SkipReason,
						constructorArguments,
						testCases.Single ().TestMethodArguments,
						MessageBus,
						Aggregator, new CancellationTokenSource ())
						.RunAsync ();
			}
		}
	}
}
