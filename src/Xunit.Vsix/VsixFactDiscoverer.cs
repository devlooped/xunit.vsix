using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VsixFactDiscoverer : IXunitTestCaseDiscoverer
    {
        IMessageSink _messageSink;

        /// <summary />
        public VsixFactDiscoverer(IMessageSink messageSink)
        {
            _messageSink = messageSink;
        }

        /// <summary />
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
                    // Didn't find any VS versions to run against. Report as a skipped test.
                    testCases.Add(new VsixTestCase(
                        _messageSink, defaultMethodDisplay, testMethod,
                        string.Join(", ", testMethod.GetComputedProperty<string[]>(factAttribute, nameof(IVsixAttribute.VisualStudioVersions))),
                        vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread)
                    {
                        SkipReason = $"Cannot execute test because no matching installation was found for Visual Studio version(s) '{string.Join(",", factAttribute.GetNamedArgument<string[]>(nameof(IVsixAttribute.VisualStudioVersions)))}'.",
                    });
                }
                else
                {
                    testCases.AddRange(vsix.VisualStudioVersions
                        .Select(version => new VsixTestCase(_messageSink, defaultMethodDisplay, testMethod, version, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread)));
                }

                return testCases;
            }
        }
    }
}
