using System;
using System.Threading.Tasks;
using Xunit.Sdk;

namespace Xunit
{
    internal interface IVsClient : IDisposable
    {
        void Recycle();

        Task<RunSummary> RunAsync(VsixTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator);
    }
}
