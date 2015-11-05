using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	[SpecialName]
	class VsixTheoryDiscoverer : IXunitTestCaseDiscoverer
	{
		IMessageSink diagnosticMessageSink;

		public VsixTheoryDiscoverer (IMessageSink diagnosticMessageSink)
		//: base (diagnosticMessageSink)
		{
			this.diagnosticMessageSink = diagnosticMessageSink;
		}

		public IEnumerable<IXunitTestCase> Discover (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo theoryAttribute)
		{
			// Special case Skip, because we want a single Skip (not one per data item); plus, a skipped test may
			// not actually have any data (which is quasi-legal, since it's skipped).
			var skipReason = theoryAttribute.GetNamedArgument<string>("Skip");
			if (skipReason != null)
				return new[] { new XunitTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod) };

			var vsVersions = VsVersions.GetFinalVersions(
				testMethod.GetComputedProperty<string[]>(theoryAttribute, SpecialNames.VsixAttribute.VisualStudioVersions),
                testMethod.GetComputedProperty<string>(theoryAttribute, SpecialNames.VsixAttribute.MinimumVisualStudioVersion),
				testMethod.GetComputedProperty<string>(theoryAttribute, SpecialNames.VsixAttribute.MaximumVisualStudioVersion));

			var validVsVersions = vsVersions.Where (v => VsVersions.InstalledVersions.Contains (v)).ToArray();

			// Process VS-specific traits.
			var suffix = testMethod.GetComputedArgument<string>(theoryAttribute, SpecialNames.VsixAttribute.RootSuffix) ?? "Exp";
			var newInstance = testMethod.GetComputedArgument<bool?>(theoryAttribute, SpecialNames.VsixAttribute.NewIdeInstance);
			var timeout = testMethod.GetComputedArgument<int?>(theoryAttribute, SpecialNames.VsixAttribute.TimeoutSeconds).GetValueOrDefault(XunitExtensions.DefaultTimeout);
			var recycle = testMethod.GetComputedArgument<bool?>(theoryAttribute, SpecialNames.VsixAttribute.RecycleOnFailure);

			if (discoveryOptions.PreEnumerateTheoriesOrDefault ()) {
				try {
					var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute));
					var results = new List<IXunitTestCase>();

					// Add invalid VS versions.
					results.AddRange (vsVersions
						.Where (v => !VsVersions.InstalledVersions.Contains (v))
						.Select (v => new ExecutionErrorTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod,
							string.Format ("Cannot execute test for specified {0}={1} because there is no VSSDK installed for that version.", SpecialNames.VsixAttribute.VisualStudioVersions, v))));

					foreach (var dataAttribute in dataAttributes) {
						var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
						var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);
						if (!discoverer.SupportsDiscoveryEnumeration (dataAttribute, testMethod.Method))
							return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, suffix, newInstance, timeout, recycle).ToArray ();

						// GetData may return null, but that's okay; we'll let the NullRef happen and then catch it
						// down below so that we get the composite test case.
						foreach (var dataRow in discoverer.GetData (dataAttribute, testMethod.Method)) {
							// Determine whether we can serialize the test case, since we need a way to uniquely
							// identify a test and serialization is the best way to do that. If it's not serializable,
							// this will throw and we will fall back to a single theory test case that gets its data at runtime.
							if (!SerializationHelper.IsSerializable (dataRow))
								return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, suffix, newInstance, timeout, recycle).ToArray ();

							var testCases = CreateTestCasesForDataRow(discoveryOptions, testMethod, validVsVersions, suffix, newInstance, timeout, recycle, dataRow);
							results.AddRange (testCases);
						}
					}

					if (results.Count == 0)
						results.Add (new ExecutionErrorTestCase (diagnosticMessageSink,
															   discoveryOptions.MethodDisplayOrDefault (),
															   testMethod,
															   $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));

					return results;
				} catch { }  // If something goes wrong, fall through to return just the XunitTestCase
			}

			return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, suffix, newInstance, timeout, recycle).ToArray ();
		}

		IEnumerable<IXunitTestCase> CreateTestCasesForDataRow (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, string rootSuffix, bool? newIdeInstance, int timeoutSeconds, bool? RecycleOnFailure, object[] dataRow)
		{
			return vsVersions.Select (v => new VsixTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod, v, rootSuffix, newIdeInstance, timeoutSeconds, RecycleOnFailure, dataRow));
		}

		IEnumerable<IXunitTestCase> CreateTestCasesForTheory (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, string rootSuffix, bool? newIdeInstance, int timeoutSeconds, bool? RecycleOnFailure)
		{
			return vsVersions.Select (v => new VsixTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod, v, rootSuffix, newIdeInstance, timeoutSeconds, RecycleOnFailure));
		}
	}
}
