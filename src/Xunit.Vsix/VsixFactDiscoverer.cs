using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    class VsixFactDiscoverer : IXunitTestCaseDiscoverer
    {
        IMessageSink _messageSink;

        public VsixFactDiscoverer(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        [System.Obsolete]
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

                if (vsix.VisualStudioVersions == null)
                {
                    testCases.Add(new XunitSkippedDataRowTestCase(
                        _messageSink, defaultMethodDisplay, testMethod,
                       string.Format(
                           CultureInfo.CurrentCulture,
                           "Cannot execute test for specified {0}={1} because {2}={3} and {4}={5}.",
                           nameof(IVsixAttribute.VisualStudioVersions),
                           string.Join(",", factAttribute.GetNamedArgument<string[]>(nameof(IVsixAttribute.VisualStudioVersions))),
                           nameof(IVsixAttribute.MinimumVisualStudioVersion),
                           vsix.MinimumVisualStudioVersion,
                           nameof(IVsixAttribute.MaximumVisualStudioVersion),
                           vsix.MaximumVisualStudioVersion)));
                }
                else
                {
                    // Add invalid VS versions.
                    testCases.AddRange(vsix.VisualStudioVersions
                        .Where(version => !VsVersions.Default.InstalledVersions.Contains(version))
                        .Select(v => new ExecutionErrorTestCase(_messageSink, defaultMethodDisplay, testMethod,
                           string.Format("Cannot execute test for specified {0}={1} because no installation was found for it.", nameof(IVsixAttribute.VisualStudioVersions), v))));

                    testCases.AddRange(vsix.VisualStudioVersions
                        .Where(version => VsVersions.Default.InstalledVersions.Contains(version))
                        .Select(version => new VsixTestCase(_messageSink, defaultMethodDisplay, testMethod, version, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread)));
                }

                return testCases;
            }
        }
    }
}
