using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	[SpecialName]
	class VsxFactDiscoverer : IXunitTestCaseDiscoverer
	{
		IMessageSink messageSink;

		public VsxFactDiscoverer (IMessageSink messageSink)
		{
			this.messageSink = messageSink;
		}

		public IEnumerable<IXunitTestCase> Discover (ITestFrameworkDiscoveryOptions discoveryOptions, ITestMethod testMethod, IAttributeInfo factAttribute)
		{
			var defaultMethodDisplay = discoveryOptions.MethodDisplayOrDefault ();
			if (testMethod.Method.GetParameters ().Any ()) {
				return new IXunitTestCase[] {
					new ExecutionErrorTestCase (messageSink, defaultMethodDisplay, testMethod,  "[VsxFact] methods are not allowed to have parameters.")
				};
			} else {
				var vsVersions = VsxVersions.GetFinalVersions(testMethod.GetComputedProperty<string[]>(factAttribute, SpecialNames.VsxAttribute.VisualStudioVersions));
				// Process VS-specific traits.
				var suffix = testMethod.GetComputedArgument<string>(factAttribute, SpecialNames.VsxAttribute.RootSuffix) ?? "Exp";
				var newInstance = testMethod.GetComputedArgument<bool?>(factAttribute, SpecialNames.VsxAttribute.NewIdeInstance);
				var timeout = testMethod.GetComputedArgument<int?>(factAttribute, SpecialNames.VsxAttribute.TimeoutSeconds).GetValueOrDefault(Vsx.DefaultTimeout);

				var testCases = new List<IXunitTestCase>();

				// Add invalid VS versions.
				testCases.AddRange (vsVersions
					.Where (v => !VsxVersions.InstalledVersions.Contains (v))
					.Select (v => new ExecutionErrorTestCase (messageSink, defaultMethodDisplay, testMethod,
						string.Format ("Cannot execute test for specified {0}={1} because there is no VSSDK installed for that version.", SpecialNames.VsxAttribute.VisualStudioVersions, v))));

				testCases.AddRange (vsVersions
					.Where (v => VsxVersions.InstalledVersions.Contains (v))
					.Select (v => new VsxTestCase (messageSink, defaultMethodDisplay, testMethod, v, suffix, newInstance, timeout)));

				return testCases;
			}
		}
	}
}
