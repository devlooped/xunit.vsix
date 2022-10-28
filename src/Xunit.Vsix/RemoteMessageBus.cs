using System;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    class RemoteMessageBus : LongLivedMarshalByRefObject, IMessageBus
    {
        readonly IMessageBus _localMessageBus;

        public RemoteMessageBus(IMessageBus localMessageBus) => _localMessageBus = localMessageBus;

        public void Dispose() => _localMessageBus.Dispose();

        [Obsolete]
        public bool QueueMessage(IMessageSinkMessage message)
        {
            if (message is ITestAssemblyFinished testAssemblyFinished)
            {
                // The test cases in the ITestAssemblyFinished message are remote proxies, but the objects won't be
                // used until after the remote process terminates. Recreate the objects in the current process to
                // avoid using objects that are no longer available.
                var testCases = testAssemblyFinished.TestCases.Select(testCase => testCase switch
                {
                    VsixTheoryTestCase theory => new VsixTestCase(new NullMessageSink(), TestMethodDisplay.ClassAndMethod, theory.TestMethod, theory.VisualStudioVersion, theory.RootSuffix, theory.NewIdeInstance, theory.Timeout, theory.RecycleOnFailure, theory.RunOnUIThread, theory.TestMethodArguments),
                    VsixTestCase fact => new VsixTestCase(new NullMessageSink(), TestMethodDisplay.ClassAndMethod, fact.TestMethod, fact.VisualStudioVersion, fact.RootSuffix, fact.NewIdeInstance, fact.Timeout, fact.RecycleOnFailure, fact.RunOnUIThread, fact.TestMethodArguments),
                    _ => new XunitTestCase(new NullMessageSink(), TestMethodDisplay.ClassAndMethod, TestMethodDisplayOptions.None, testCase.TestMethod, testCase.TestMethodArguments)
                }); ;

                message = new TestAssemblyFinished(testCases.ToArray(), testAssemblyFinished.TestAssembly, testAssemblyFinished.ExecutionTime, testAssemblyFinished.TestsRun, testAssemblyFinished.TestsFailed, testAssemblyFinished.TestsSkipped);
            }

            return _localMessageBus.QueueMessage(message);
        }
    }
}
