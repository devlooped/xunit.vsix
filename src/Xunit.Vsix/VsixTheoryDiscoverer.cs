using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
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

			var vsix = testMethod.GetVsixAttribute(theoryAttribute);
			var validVsVersions = vsix.VisualStudioVersions.Where (v => VsVersions.InstalledVersions.Contains (v)).ToArray();

			// We always pre-enumerate theories, since that's how we build the concrete test cases.
			try {
				var dataAttributes = testMethod.Method.GetCustomAttributes(typeof(DataAttribute));
				var results = new List<IXunitTestCase>();

				foreach (var dataAttribute in dataAttributes) {
					var discovererAttribute = dataAttribute.GetCustomAttributes(typeof(DataDiscovererAttribute)).First();
					var discoverer = ExtensibilityPointFactory.GetDataDiscoverer(diagnosticMessageSink, discovererAttribute);
					if (!discoverer.SupportsDiscoveryEnumeration (dataAttribute, testMethod.Method))
						return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure).ToArray ();

					// GetData may return null, but that's okay; we'll let the NullRef happen and then catch it
					// down below so that we get the composite test case.
					foreach (var dataRow in discoverer.GetData (dataAttribute, testMethod.Method)) {
						// Determine whether we can serialize the test case, since we need a way to uniquely
						// identify a test and serialization is the best way to do that. If it's not serializable,
						// this will throw and we will fall back to a single theory test case that gets its data at runtime.
						if (!SerializationHelper.IsSerializable (dataRow))
							return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure).ToArray ();

						var testCases = CreateTestCasesForDataRow(discoveryOptions, testMethod, validVsVersions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure, dataRow);
						results.AddRange (testCases);
					}
				}

				if (results.Count == 0)
					results.Add (new ExecutionErrorTestCase (diagnosticMessageSink,
														   discoveryOptions.MethodDisplayOrDefault (),
														   testMethod,
														   $"No data found for {testMethod.TestClass.Class.Name}.{testMethod.Method.Name}"));

				// Add invalid VS versions.
				results.AddRange (vsix.VisualStudioVersions
					.Where (version => !VsVersions.InstalledVersions.Contains (version))
					.Select (version => new ExecutionErrorTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod,
						string.Format ("Cannot execute test for specified {0}={1} because there is no VSSDK installed for that version.", nameof(IVsixAttribute.VisualStudioVersions), version))));

				return results;
			} catch { }  // If something goes wrong, fall through to return just the XunitTestCase

			return CreateTestCasesForTheory (discoveryOptions, testMethod, validVsVersions, vsix.RootSuffix, vsix.NewIdeInstance, vsix.TimeoutSeconds, vsix.RecycleOnFailure).ToArray ();
		}

		IEnumerable<IXunitTestCase> CreateTestCasesForDataRow (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, string rootSuffix, bool? newIdeInstance, int timeoutSeconds, bool? RecycleOnFailure, object[] dataRow)
		{
			return vsVersions.Select (version => new VsixTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod, version, rootSuffix, newIdeInstance, timeoutSeconds, RecycleOnFailure, dataRow));
		}

		IEnumerable<IXunitTestCase> CreateTestCasesForTheory (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, string[] vsVersions, string rootSuffix, bool? newIdeInstance, int timeoutSeconds, bool? RecycleOnFailure)
		{
			return vsVersions.Select (version => new VsixTheoryTestCase (diagnosticMessageSink, discoveryOptions.MethodDisplayOrDefault (), testMethod, version, rootSuffix, newIdeInstance, timeoutSeconds, RecycleOnFailure));
		}
	}
}
