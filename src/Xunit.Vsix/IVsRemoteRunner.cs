using System;
using Xunit.Sdk;

namespace Xunit
{
    interface IVsRemoteRunner : IDisposable
    {
        void EnsureInitialized();

        string[][] GetEnvironment();

        void Ping();

        VsixRunSummary Run(VsixTestCase testCase, IMessageBus messageBus);
    }
}
