using System.Diagnostics;

namespace Xunit
{
    static class Constants
    {
        /// <summary>
        /// This assembly name, for use in Xunit attributes that need to specify an
        /// assembly name.
        /// </summary>
        public const string ThisAssembly = "xunit.vsix";

        /// <summary>
        /// The root namespace of this assembly.
        /// </summary>
        public const string RootNamespace = "Xunit";

        public const string PipeNameEnvironmentVariable = "xunit.vsix.pipe";
        public const string BaseDirectoryEnvironmentVariable = "xunit.vsix.directory";

        /// <summary>
        /// Set to 'true' to launch a debugger in the xunit process.
        /// </summary>
        public const string DebugEnvironmentVariable = "XUNIT_VSIX_DEBUG";

        /// <summary>
        /// Set to 'true' to launch a debugger in the remote VS process.
        /// </summary>
        public const string DebugRemoteEnvironmentVariable = "XUNIT_VSIX_DEBUG_REMOTE";

        /// <summary>
        /// Environment variable to disable timeouts for debugging
        /// </summary>
        public const string DisableTimeoutEnvironmentVariable = "XUNIT_VSIX_NO_TIMEOUT";

        public const string TracerName = "xunit.vsix";
        public const string ServerChannelName = "xunit.vsix.server";
        public const string ClientChannelName = "xunit.vsix.client-";

        public static readonly TraceSource Tracer = new TraceSource(TracerName);
    }
}
