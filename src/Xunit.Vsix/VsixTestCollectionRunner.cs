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
        IMessageSink _diagnosticMessageSink;
        string _vsVersion;
        string _rootSuffix;
        IVsClient _vs;

        public VsixTestCollectionRunner(VsixTestCollection testCollection, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink,
            IMessageBus messageBus, ITestCaseOrderer testCaseOrderer, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource) :
            base(testCollection, testCases, diagnosticMessageSink, messageBus, testCaseOrderer, aggregator, cancellationTokenSource)
        {
            _diagnosticMessageSink = diagnosticMessageSink;

            _vsVersion = testCollection.VisualStudioVersion;
            _rootSuffix = testCollection.RootSuffix;
            _vs = new LazyVsClient(() => new VsClient(_vsVersion, _rootSuffix, testCollection.Settings));
        }

        protected override Task<RunSummary> RunTestClassAsync(ITestClass testClass, IReflectionTypeInfo @class, IEnumerable<IXunitTestCase> testCases)
        {
            return new VsixTestClassRunner(_vs, testClass, @class, testCases, _diagnosticMessageSink, MessageBus, TestCaseOrderer,
                Aggregator, CancellationTokenSource, CollectionFixtureMappings).RunAsync();
        }

        public void Dispose() => _vs.Dispose();

        class LazyVsClient : IVsClient
        {
            Lazy<IVsClient> _vs;
            
            public LazyVsClient(Func<IVsClient> factory) => _vs = new Lazy<IVsClient>(factory);

            public void Dispose()
            {
                if (_vs.IsValueCreated)
                    _vs.Value.Dispose();
            }

            public void Recycle()
            {
                if (_vs.IsValueCreated)
                    _vs.Value.Recycle();
            }

            public Task<RunSummary> RunAsync(VsixTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
                => _vs.Value.RunAsync(testCase, messageBus, aggregator);
        }
    }
}
