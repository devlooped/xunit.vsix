using System.Collections.Generic;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsxTestCase : XunitTestCase
	{
		public VsxTestCase (IMessageSink messageSink, Xunit.Sdk.TestMethodDisplay defaultMethodDisplay, ITestMethod testMethod,
			string vsVersion, string rootSuffix, bool? newIdeInstance, int timeoutSeconds)
			: base (messageSink, defaultMethodDisplay, testMethod)
		{
			VisualStudioVersion = vsVersion;
			RootSuffix = rootSuffix;
			NewIdeInstance = newIdeInstance;
			TimeoutSeconds = timeoutSeconds;
		}

		public string VisualStudioVersion { get; private set; }

		public string RootSuffix { get; private set; }

		public bool? NewIdeInstance { get; private set; }

		public int TimeoutSeconds { get; private set; }

		protected override void Initialize ()
		{
			base.Initialize ();

			DisplayName += " > vs" + VisualStudioVersion;

			// Register VS version as a trait, so that it can be used to group runs.
			Traits["VisualStudioVersion"] = new List<string>(new [] { VisualStudioVersion });
		}

		protected override string GetUniqueID ()
		{
			return base.GetUniqueID () + "-" + VisualStudioVersion;
		}

        public override void Serialize(IXunitSerializationInfo data)
        {
            data.AddValue("VisualStudioVersion", VisualStudioVersion);
            data.AddValue(SpecialNames.VsxAttribute.RootSuffix, RootSuffix);
            data.AddValue(SpecialNames.VsxAttribute.NewIdeInstance, NewIdeInstance);
            data.AddValue(SpecialNames.VsxAttribute.TimeoutSeconds, TimeoutSeconds);
        }

        /// <inheritdoc/>
        public override void Deserialize(IXunitSerializationInfo data)
        {
            VisualStudioVersion = data.GetValue<string>("VisualStudioVersion");
            RootSuffix = data.GetValue<string>(SpecialNames.VsxAttribute.RootSuffix);
            NewIdeInstance = data.GetValue<bool?>(SpecialNames.VsxAttribute.NewIdeInstance);
			TimeoutSeconds = data.GetValue<int> (SpecialNames.VsxAttribute.TimeoutSeconds);
        }
	}
}
