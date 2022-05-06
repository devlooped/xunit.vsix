using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    /// <summary />
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class VsixTheoryDiscoverer : IXunitTestCaseDiscoverer
    {
        IMessageSink _diagnosticMessageSink;

        /// <summary />
        public VsixTheoryDiscoverer(IMessageSink diagnosticMessageSink)
        //: base (diagnosticMessageSink)
        {
            _diagnosticMessageSink = diagnosticMessageSink;
        }

        /// <summary />
        [Obsolete]
        public IEnumerable<IXunitTestCase> Discover(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
        {
            // Special case Skip, because we want a single Skip (not one per data item); plus, a skipped test may
            // not actually have any data (which is quasi-legal, since it's skipped).
            var skipReason = theoryAttribute.GetInitializedArgument<string>("Skip");
            if (skipReason != null)
                return new[] { new XunitTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod) };

            var vsix = testMethod.GetVsixAttribute(theoryAttribute);

            // We always pre-enumerate theories, since that's how we build the concrete test cases.
            try
            {
                var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute));
                var results = new List<IXunitTestCase>();

                foreach (var dataAttribute in dataAttributes)
                {
                    var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
                    var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(_diagnosticMessageSink, discovererAttribute);
                    if (!discoverer.SupportsDiscoveryEnumeration(dataAttribute, testMethod.Method))
                        return CreateTestCasesForTheory(discoveryOptions, testMethod, vsix.VisualStudioVersions, vsix).ToArray();

                    // GetData may return null, but that's okay; we'll let the NullRef happen and then catch it
                    // down below so that we get the composite test case.
                    foreach (var dataRow in discoverer.GetData(dataAttribute, testMethod.Method))
                    {
                        // Determine whether we can serialize the test case, since we need a way to uniquely
                        // identify a test and serialization is the best way to do that. If it's not serializable,
                        // this will throw and we will fall back to a single theory test case that gets its data at runtime.
                        if (!SerializationHelper.IsSerializable(dataRow))
                            return CreateTestCasesForTheory(discoveryOptions, testMethod, vsix.VisualStudioVersions, vsix).ToArray();

                        var testCases = CreateTestCasesForDataRow(discoveryOptions, testMethod, vsix.VisualStudioVersions, vsix, dataRow);
                        results.AddRange(testCases);
                    }
                }

                if (results.Count == 0)
                    results.Add(new ExecutionErrorTestCase(_diagnosticMessageSink,
                                                           discoveryOptions.MethodDisplayOrDefault(),
                                                           testMethod,
                                                           $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));

                return results;
            }
            catch { }  // If something goes wrong, fall through to return just the XunitTestCase

            return CreateTestCasesForTheory(discoveryOptions, testMethod, vsix.VisualStudioVersions, vsix).ToArray();
        }

        [Obsolete]
        IEnumerable<IXunitTestCase> CreateTestCasesForDataRow(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, IVsixAttribute vsix, object[] dataRow)
        {
            if (vsVersions.Length == 0)
            {
                var versions = string.Join(", ", testMethod.GetComputedProperty<string[]>(nameof(IVsixAttribute.VisualStudioVersions)));
                // Didn't find any VS versions to run against. Report as a skipped test.
                return new[]
                {
                    new VsixTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod,
                        versions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread, dataRow)
                    {
                        SkipReason = $"Cannot execute test because no matching installation was found for Visual Studio version(s) '{versions}'.",
                    }
                };
            }
            else
            {
                return vsVersions.Select(version => new VsixTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod, version, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread, dataRow));
            }
        }

        [Obsolete]
        IEnumerable<IXunitTestCase> CreateTestCasesForTheory(ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, IVsixAttribute vsix)
        {
            if (vsVersions.Length == 0)
            {
                var versions = string.Join(", ", testMethod.GetComputedProperty<string[]>(nameof(IVsixAttribute.VisualStudioVersions)));
                // Didn't find any VS versions to run against. Report as a skipped test.
                return new[]
                {
                    new VsixTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod,
                        versions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread)
                    {
                        SkipReason = $"Cannot execute test because no matching installation was found for Visual Studio version(s) '{versions}'.",
                    }
                };
            }
            else
            {
                return vsVersions.Select(version =>
                    new VsixTheoryTestCase(_diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault(), testMethod,
                        version, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, vsix.RunOnUIThread));
            }
        }
    }
}
