using System;
using System.Diagnostics;

namespace Xunit
{
    /// <summary>
    ///  Provides global VSIX runner defaults.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public class VsixRunnerAttribute : Attribute
    {
        /// <summary>
        /// Number of retries to attach a debugger to the running VS instance.
        /// </summary>
        public int DebuggerAttachRetries { get; set; }

        /// <summary>
        /// Number of retries to connect to a running VS after it has
        /// been successfully started to start test execution.
        /// </summary>
        public int RemoteConnectionRetries { get; set; }

        /// <summary>
        /// Number of retries to start VS and connect to its DTE service.
        /// </summary>
        public int ProcessStartRetries { get; set; }

        /// <summary>
        /// The starting sleep interval across retries, in milliseconds.
        /// Will be used to exponentially increase the wait after each
        /// retry.
        /// </summary>
        public int RetrySleepInterval { get; set; }

        /// <summary>
        /// The timeout in seconds to wait for Visual Studio to
        /// start and initialize its DTE automation model.
        /// </summary>
        public int StartupTimeout { get; set; }

        /// <summary>
        /// Wait time in milliseconds to wait for Visual Studio to warm up 
        /// before injecting into the .NET runtime in it.
        /// </summary>
        public int WarmupMilliseconds { get; set; }

        /// <summary>
        /// Specifies the tracing level for runner diagnostics.
        /// </summary>
        public SourceLevels TraceLevel { get; set; }
    }
}
