using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	[SpecialName]
	class VsixFactDiscoverer : IXunitTestCaseDiscoverer
	{
		IMessageSink messageSink;

		public VsixFactDiscoverer (IMessageSink messageSink)
		{
			this.messageSink = messageSink;
		}

		public IEnumerable<IXunitTestCase> Discover (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
		{
			var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault ();
			if (testMethod.Method.GetParameters ().Any ()) {
				return new IXunitTestCase[] {
					new ExecutionErrorTestCase (messageSink, defaultMethodDisplay, testMethod,  "[VsixFact] methods are not allowed to have parameters.")
				};
			} else {
				var vsVersions = VsVersions.GetFinalVersions(
					testMethod.GetComputedProperty<string[]>(factAttribute, SpecialNames.VsixAttribute.VisualStudioVersions),
					testMethod.GetComputedProperty<string>(factAttribute, SpecialNames.VsixAttribute.MinimumVisualStudioVersion),
					testMethod.GetComputedProperty<string>(factAttribute, SpecialNames.VsixAttribute.MaximumVisualStudioVersion));

				// Process VS-specific traits.
				var suffix = testMethod.GetComputedArgument<string>(factAttribute, SpecialNames.VsixAttribute.RootSuffix) ?? "Exp";
				var newInstance = testMethod.GetComputedArgument<bool?>(factAttribute, SpecialNames.VsixAttribute.NewIdeInstance);
				var timeout = testMethod.GetComputedArgument<int?>(factAttribute, SpecialNames.VsixAttribute.TimeoutSeconds).GetValueOrDefault(XunitExtensions.DefaultTimeout);
				var recycle = testMethod.GetComputedArgument<bool?>(factAttribute, SpecialNames.VsixAttribute.RecycleOnFailure);

				var testCases = new List<IXunitTestCase>();

				// Add invalid VS versions.
				testCases.AddRange (vsVersions
					.Where (v => !VsVersions.InstalledVersions.Contains (v))
					.Select (v => new ExecutionErrorTestCase (messageSink, defaultMethodDisplay, testMethod,
						string.Format ("Cannot execute test for specified {0}={1} because there is no VSSDK installed for that version.", SpecialNames.VsixAttribute.VisualStudioVersions, v))));

				testCases.AddRange (vsVersions
					.Where (v => VsVersions.InstalledVersions.Contains (v))
					.Select (v => new VsixTestCase (messageSink, defaultMethodDisplay, testMethod, v, suffix, newInstance, timeout, recycle)));

				return testCases;
			}
		}
	}
}
