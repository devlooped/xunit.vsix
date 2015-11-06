using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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

		Dictionary<Type, object> assemblyFixtureMappings = new Dictionary<Type, object>();
		Dictionary<Type, object> collectionFixtureMappings = new Dictionary<Type, object>();
		ConcurrentDictionary<ITestCollection, VsRemoteTestCollectionRunner> collectionRunnerMap = new ConcurrentDictionary<ITestCollection, VsRemoteTestCollectionRunner>();

		public VsRemoteRunner ()
		{
			pipeName = Environment.GetEnvironmentVariable (Constants.PipeNameEnvironmentVariable);

			RemotingServices.Marshal (this, RemotingUtil.HostName);
		}

		public void Ping () { }

		public void AddListener (TraceListener listener)
		{
			if (Trace.Listeners.Contains (listener))
				Trace.Listeners.Add (listener);
		}

		public VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus)
		{
			var aggregator = new ExceptionAggregator ();
			var runner = collectionRunnerMap.GetOrAdd(testCase.TestMethod.TestClass.TestCollection, tc => new VsRemoteTestCollectionRunner(tc, assemblyFixtureMappings, collectionFixtureMappings));

			if (SynchronizationContext.Current == null)
				SynchronizationContext.SetSynchronizationContext (new SynchronizationContext ());

			try {
				VsixRunSummary result = runner.RunAsync (testCase, messageBus, aggregator)
				.Result
				.ToVsixRunSummary ();

				if (aggregator.HasExceptions && result != null)
					result.Exception = aggregator.ToException ();

				return result;
			} catch (AggregateException aex) {
				return new VsixRunSummary {
					Failed = 1,
					Exception = aex.Flatten ().InnerException
				};
			}
		}

		public void Dispose ()
		{
			var aggregator = new ExceptionAggregator();
			var tasks = collectionFixtureMappings.Values.OfType<IAsyncLifetime> ()
				.Select(asyncFixture => aggregator.RunAsync(asyncFixture.DisposeAsync))
				.Concat(assemblyFixtureMappings.Values.OfType<IAsyncLifetime>()
				.Select(asyncFixture => aggregator.RunAsync(asyncFixture.DisposeAsync)))
				.ToArray();

			foreach (var disposable in assemblyFixtureMappings.Values.OfType<IDisposable> ()
				.Concat (collectionFixtureMappings.Values.OfType<IDisposable> ())) {
				aggregator.Run (disposable.Dispose);
			}

			Task.WaitAll (tasks);
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

		class VsRemoteTestCollectionRunner : XunitTestCollectionRunner
		{
			readonly Dictionary<Type, object> assemblyFixtureMappings;

			public VsRemoteTestCollectionRunner (ITestCollection testCollection, Dictionary<Type, object> assemblyFixtureMappings, Dictionary<Type, object> collectionFixtureMappings)
				: base (testCollection, Enumerable.Empty<IXunitTestCase> (), new NullMessageSink (), null,
					  new DefaultTestCaseOrderer (new NullMessageSink ()), new ExceptionAggregator (), new CancellationTokenSource ())
			{
				this.assemblyFixtureMappings = assemblyFixtureMappings;
				CollectionFixtureMappings = collectionFixtureMappings;
			}

			public Task<RunSummary> RunAsync (IXunitTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
			{
				TestCases = new[] { testCase };
				MessageBus = messageBus;
				Aggregator = aggregator;

				return RunAsync ();
			}

			protected override Task BeforeTestCollectionFinishedAsync ()
			{
				// Prevent the automatic cleanup of the collection fixture mappings that happens via the base class,
				// since our collection cleanup is exactly the same as the VS cleanup, since there's a 1>1 mapping
				// of VS version+hive per collection.
				return Task.FromResult (true);
			}

			protected override Task<RunSummary> RunTestClassAsync (ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
			{
				foreach (var fixtureType in @class.Type.GetTypeInfo ().ImplementedInterfaces
						.Where (i => i.GetTypeInfo ().IsGenericType && i.GetGenericTypeDefinition () == typeof (IAssemblyFixture<>))
						.Select (i => i.GetTypeInfo ().GenericTypeArguments.Single ())
						// First pass at filtering out before locking
						.Where (i => !assemblyFixtureMappings.ContainsKey (i))) {
					// ConcurrentDictionary's GetOrAdd does not lock around the value factory call, so we need
					// to do it ourselves.
					lock (assemblyFixtureMappings) {
						if (!assemblyFixtureMappings.ContainsKey (fixtureType)) {
							Aggregator.Run (() => {
								var fixture = Activator.CreateInstance (fixtureType);
								var asyncFixture = fixture as IAsyncLifetime;
								if (asyncFixture != null)
									Aggregator.RunAsync (asyncFixture.InitializeAsync);

								assemblyFixtureMappings.Add (fixtureType, fixture);
							});
						}
					}
				}

				// Don't want to use .Concat + .ToDictionary because of the possibility of overriding types,
				// so instead we'll just let collection fixtures override assembly fixtures.
				var combinedFixtures = new Dictionary<Type, object>(assemblyFixtureMappings);
				foreach (var kvp in CollectionFixtureMappings)
					combinedFixtures[kvp.Key] = kvp.Value;

				// We've done everything we need, so let the built-in types do the rest of the heavy lifting
				return new VsRemoteTestClassRunner (testClass, @class, combinedFixtures).RunAsync (testCases.Single (), MessageBus, Aggregator);
			}
		}

		class VsRemoteTestClassRunner : XunitTestClassRunner
		{
			public VsRemoteTestClassRunner (ITestClass testClass, IReflectionTypeInfo @class, Dictionary<Type, object> collectionFixtureMappings)
				: base (testClass, @class, Enumerable.Empty<IXunitTestCase> (), new NullMessageSink (), null,
					  new DefaultTestCaseOrderer (new NullMessageSink ()), new ExceptionAggregator (), new CancellationTokenSource (), collectionFixtureMappings)
			{
			}

			public Task<RunSummary> RunAsync (IXunitTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
			{
				TestCases = new[] { testCase };
				MessageBus = messageBus;
				Aggregator = aggregator;
				return RunAsync ();
			}

			protected override async Task<RunSummary> RunTestMethodAsync (ITestMethod testMethod, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, object[] constructorArguments)
			{
				var vsixTest = testCases.OfType<VsixTestCase>().Single();
				var cancellation = Debugger.IsAttached ?
					new CancellationTokenSource(TimeSpan.FromSeconds(vsixTest.TimeoutSeconds)).Token :
					CancellationToken.None;

				// We don't want to run the discovery again over the test method,
				// generate new test cases and so on, since we already have received a single test case to run.
				// Also, we want the test case to be run in the UI thread, which is what you typically want.
				return await Application.Current.Dispatcher.InvokeAsync (() =>
					new SyncTestCaseRunner (
							testCases.Single (),
							testCases.Single ().DisplayName,
							testCases.Single ().SkipReason,
							constructorArguments,
							testCases.Single ().TestMethodArguments,
							MessageBus,
							Aggregator, new CancellationTokenSource ())
						.RunAsync ()
						.Result, DispatcherPriority.Background, cancellation);
			}

			class SyncTestCaseRunner : XunitTestCaseRunner
			{
				public SyncTestCaseRunner (IXunitTestCase testCase, string displayName, string skipReason, object[] constructorArguments, object[] testMethodArguments, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
					: base (testCase, displayName, skipReason, constructorArguments, testMethodArguments, messageBus, aggregator, cancellationTokenSource)
				{
				}

				protected override Task<RunSummary> RunTestAsync ()
				{
					return new SyncTestRunner (new XunitTest (TestCase, DisplayName), MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, SkipReason, BeforeAfterAttributes, new ExceptionAggregator (Aggregator), CancellationTokenSource).RunAsync ();
				}

				class SyncTestRunner : XunitTestRunner
				{
					public SyncTestRunner (ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, string skipReason, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
						: base (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, skipReason, beforeAfterAttributes, aggregator, cancellationTokenSource)
					{
					}

					protected override Task<decimal> InvokeTestMethodAsync (ExceptionAggregator aggregator)
					{
						return new SyncTestInvoker (Test, MessageBus, TestClass, ConstructorArguments, TestMethod, TestMethodArguments, BeforeAfterAttributes, aggregator, CancellationTokenSource).RunAsync ();
					}

					class SyncTestInvoker : XunitTestInvoker
					{
						public SyncTestInvoker (ITest test, IMessageBus messageBus, Type testClass, object[] constructorArguments, MethodInfo testMethod, object[] testMethodArguments, IReadOnlyList<BeforeAfterTestAttribute> beforeAfterAttributes, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
							: base (test, messageBus, testClass, constructorArguments, testMethod, testMethodArguments, beforeAfterAttributes, aggregator, cancellationTokenSource)
						{
						}

						protected override Task<decimal> InvokeTestMethodAsync (object testClassInstance)
						{
								Aggregator.Run (() => Timer.Aggregate (() => {
									var parameterCount = TestMethod.GetParameters().Length;
									var valueCount = TestMethodArguments == null ? 0 : TestMethodArguments.Length;
									if (parameterCount != valueCount) {
										Aggregator.Add (
											new InvalidOperationException (
												$"The test method expected {parameterCount} parameter value{(parameterCount == 1 ? "" : "s")}, but {valueCount} parameter value{(valueCount == 1 ? "" : "s")} {(valueCount == 1 ? "was" : "were")} provided."
											)
										);
									} else {
										var result = CallTestMethod(testClassInstance);
										var task = result as Task;
										if (task != null)
											task.Wait ();
									}
								}));

							return Task.FromResult(Timer.Total);
						}
					}
				}
			}
		}
	}
}
