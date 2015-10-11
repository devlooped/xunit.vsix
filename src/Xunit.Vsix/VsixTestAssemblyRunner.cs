using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Groups test cases by IDE version and root suffix, and
	/// delegates to the <see cref="VsixTestCollectionRunner"/>.
	/// </summary>
	class VsixTestAssemblyRunner : XunitTestAssemblyRunner
	{
		List<IDisposable> disposables = new List<IDisposable>();

		public VsixTestAssemblyRunner (ITestAssembly testAssembly,
									   IEnumerable<IXunitTestCase> testCases,
									   IMessageSink diagnosticMessageSink,
									   IMessageSink executionMessageSink,
									   ITestFrameworkExecutionOptions executionOptions)
			: base (testAssembly, testCases, diagnosticMessageSink, executionMessageSink, executionOptions)
		{ }

		protected override IMessageBus CreateMessageBus ()
		{
			return new InterceptingMessageBus (base.CreateMessageBus (), OnMessage);
		}

		protected override async Task<RunSummary> RunTestCollectionsAsync (IMessageBus messageBus, CancellationTokenSource cancellationTokenSource)
		{
			var allTests = TestCases;

			try {
				TestCases = allTests.Where (tc => !(tc is VsixTestCase));

				// Preserves base xunit run behavior.
				var xunitSummary = await base.RunTestCollectionsAsync(messageBus, cancellationTokenSource);

				var maxParallelThreads = base.ExecutionOptions.MaxParallelThreadsOrDefault();
				if (maxParallelThreads < VsVersions.InstalledVersions.Length)
					maxParallelThreads = VsVersions.InstalledVersions.Length;

				SetupSyncContext (maxParallelThreads);

				var scheduler = TaskScheduler.FromCurrentSynchronizationContext();

				var tasks = CreateTestCollections(allTests.OfType<VsixTestCase>()).Select(collection =>
					Task.Factory.StartNew(() =>
						RunTestCollectionAsync(messageBus, collection.Item1, collection.Item2, cancellationTokenSource),
						cancellationTokenSource.Token,
						TaskCreationOptions.DenyChildAttach | TaskCreationOptions.HideScheduler,
						scheduler)
				).ToArray();

				var summaries = await Task.WhenAll(tasks.Select(t => t.Unwrap()));

				return new RunSummary ()
				{
					Total = summaries.Sum (s => s.Total) + xunitSummary.Total,
					Failed = summaries.Sum (s => s.Failed) + xunitSummary.Failed,
					Skipped = summaries.Sum (s => s.Skipped) + xunitSummary.Skipped
				};

			} finally {
				TestCases = allTests;
			}
		}

		protected override Task<RunSummary> RunTestCollectionAsync (IMessageBus messageBus, ITestCollection testCollection, IEnumerable<IXunitTestCase> testCases, CancellationTokenSource cancellationTokenSource)
		{
			var vsixCollection = testCollection as VsixTestCollection;
			if (vsixCollection != null) {
				var runner = new VsixTestCollectionRunner (vsixCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator (Aggregator), cancellationTokenSource);
				disposables.Add (runner);
				return runner.RunAsync ();
			}

			return new XunitTestCollectionRunner (testCollection, testCases, DiagnosticMessageSink, messageBus, TestCaseOrderer, new ExceptionAggregator (Aggregator), cancellationTokenSource).RunAsync();
		}

		private void OnMessage(IMessageSinkMessage message)
		{
			if (message is ITestAssemblyFinished)
				disposables.ForEach (d => d.Dispose ());
		}

		/// <summary>
		/// Orders the test collections using the TestCollectionOrderer.
		/// </summary>
		/// <returns>Test collections (and the associated test cases) in run order</returns>
		private List<Tuple<VsixTestCollection, List<IXunitTestCase>>> CreateTestCollections (IEnumerable<VsixTestCase> vsixTests)
		{
			var collections = new ConcurrentDictionary<string, ITestCollection>();

			var testCases = from tc in vsixTests.OfType<VsixTestCase>()
							// For NewIdeInstance tests, every test case is its own new collection that will
							// run in parallel with the rest. Otherwise, it's a combination of VS + Suffix.
							let key = tc.NewIdeInstance.GetValueOrDefault() ? Guid.NewGuid().ToString() : tc.VisualStudioVersion + tc.RootSuffix
							let col = collections.GetOrAdd(key, x => new VsixTestCollection(
								tc.TestMethod.TestClass.TestCollection.TestAssembly,
								tc.TestMethod.TestClass.Class,
								tc.VisualStudioVersion, tc.RootSuffix))
							select new { Collection = col, Test = tc };

			var testCasesByCollection = testCases.GroupBy(tc => tc.Collection, TestCollectionComparer.Instance)
				.ToDictionary(group => group.Key, group => group.Select(x => (IXunitTestCase)x.Test).ToList());

			IEnumerable<ITestCollection> orderedTestCollections;

			try {
				orderedTestCollections = TestCollectionOrderer.OrderTestCollections (testCasesByCollection.Keys);
			} catch (Exception ex) {
				var innerEx = ex.Unwrap();
				DiagnosticMessageSink.OnMessage (new DiagnosticMessage ("Test collection orderer '{0}' threw '{1}' during ordering: {2}", TestCollectionOrderer.GetType ().FullName, innerEx.GetType ().FullName, innerEx.StackTrace));
				orderedTestCollections = testCasesByCollection.Keys.ToList ();
			}

			return orderedTestCollections
				.OfType<VsixTestCollection>()
				.Select (collection => Tuple.Create (collection, testCasesByCollection[collection]))
				.ToList ();
		}
	}
}
