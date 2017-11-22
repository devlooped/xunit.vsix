using System;
using System.Diagnostics;
using Xunit.Sdk;

namespace Xunit
{
    internal interface IVsRemoteRunner : IDisposable
    {
        string[][] GetEnvironment();

        void Ping();

        VsixRunSummary Run(VsixTestCase testCase, IMessageBus messageBus);
    }
}
