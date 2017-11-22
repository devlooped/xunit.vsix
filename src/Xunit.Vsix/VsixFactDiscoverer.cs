using System.Collections.Generic;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    internal class VsixFactDiscoverer : IXunitTestCaseDiscoverer
    {
        private IMessageSink _messageSink;

        public VsixFactDiscoverer(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
        {
            var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault();
            if (testMethod.Method.GetParameters().Any())
            {
                return new IXunitTestCase[] {
                    new ExecutionErrorTestCase (_messageSink, defaultMethodDisplay, testMethod,  "[VsixFact] methods are not allowed to have parameters.")
                };
            }
            else
            {
                var vsix = testMethod.GetVsixAttribute(factAttribute);
                var testCases = new List<IXunitTestCase>();

                // Add invalid VS versions.
                testCases.AddRange(vsix.VisualStudioVersions
                    .Where(version => !VsVersions.InstalledVersions.Contains(version))
                    .Select(v => new ExecutionErrorTestCase(_messageSink, defaultMethodDisplay, testMethod,
                       string.Format("Cannot execute test for specified {0}={1} because there is no VSSDK installed for that version.", nameof(IVsixAttribute.VisualStudioVersions), v))));

                testCases.AddRange(vsix.VisualStudioVersions
                    .Where(version => VsVersions.InstalledVersions.Contains(version))
                    .Select(version => new VsixTestCase(_messageSink, defaultMethodDisplay, testMethod, version, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread)));

                return testCases;
            }
        }
    }
}
