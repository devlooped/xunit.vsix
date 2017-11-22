using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    internal class VsixTestMethodRunner : XunitTestMethodRunner
    {
        private readonly IVsClient _vsClient;


        public VsixTestMethodRunner(IVsClient vsClient, ITestMethod testMethod, IReflectionTypeInfo @class, IReflectionMethodInfo method, IEnumerable<IXunitTestCase> testCases, IMessageSink diagnosticMessageSink, IMessageBus messageBus, ExceptionAggregator aggregator, CancellationTokenSource cancellationTokenSource)
            : base(testMethod, @class, method, testCases, diagnosticMessageSink, messageBus, aggregator, cancellationTokenSource, new object[0])
        {
            _vsClient = vsClient;
        }

        protected override Task<RunSummary> RunTestCaseAsync(IXunitTestCase testCase)
        {
            if (!CancellationTokenSource.IsCancellationRequested)
                return _vsClient.RunAsync((VsixTestCase)testCase, MessageBus, Aggregator);
            else
                return Task.FromResult(new RunSummary());
        }

        protected override void BeforeTestMethodFinished()
        {
            base.BeforeTestMethodFinished();
            if (Aggregator.HasExceptions && Aggregator.ToException() is TimeoutException)
                _vsClient.Recycle();
        }
    }
}
