using System;
using System.Linq;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
    class VsixTestCollection : TestCollection
    {
        [Obsolete]
        public VsixTestCollection() { }

        public VsixTestCollection(ITestAssembly testAssembly, ITypeInfo collectionDefinition,
            string visualStudioVersion, string rootSuffix)
            : base(testAssembly, collectionDefinition, visualStudioVersion + " (" + rootSuffix + ")")
        {
            VisualStudioVersion = visualStudioVersion;
            RootSuffix = rootSuffix;

            var settingsAttribute = testAssembly.Assembly.GetCustomAttributes(typeof(VsixRunnerAttribute)).FirstOrDefault();
            if (settingsAttribute == null)
                Settings = new VsixRunnerSettings();
            else
                Settings = new VsixRunnerSettings(
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.DebuggerAttachRetries)),
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.RemoteConnectionRetries)),
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.ProcessStartRetries)),
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.RetrySleepInterval)),
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.StartupTimeoutSeconds)),
                    settingsAttribute.GetInitializedArgument<int?>(nameof(VsixRunnerSettings.WarmupSeconds)));
        }

        public string VisualStudioVersion { get; }

        public string RootSuffix { get; }

        public VsixRunnerSettings Settings { get; }
    }
}
