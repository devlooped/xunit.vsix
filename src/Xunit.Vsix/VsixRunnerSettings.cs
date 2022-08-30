namespace Xunit
{
    class VsixRunnerSettings
    {
        public VsixRunnerSettings(int? debuggerAttachRetries = null, int? remoteConnectionRetries = null,
            int? processStartRetries = null, int? retrySleepInterval = null,
            int? startupTimeoutSeconds = null,
            int? warnupSeconds = null)
        {
            DebuggerAttachRetries = debuggerAttachRetries ?? 5;
            RemoteConnectionRetries = remoteConnectionRetries ?? 2;
            ProcessStartRetries = processStartRetries ?? 1;
            RetrySleepInterval = retrySleepInterval ?? 200;
            StartupTimeoutSeconds = startupTimeoutSeconds ?? 30;
            WarmupSeconds = warnupSeconds ?? 10;
        }

        /// <summary>
        /// Number of retries to attach a debugger to the running VS instance.
        /// </summary>
        public int DebuggerAttachRetries { get; private set; }

        /// <summary>
        /// Number of retries to connect to a running VS after it has
        /// been successfully started to start test execution.
        /// </summary>
        public int RemoteConnectionRetries { get; private set; }

        /// <summary>
        /// Number of retries to start VS and connect to its DTE service.
        /// </summary>
        public int ProcessStartRetries { get; private set; }

        /// <summary>
        /// The starting sleep interval across retries, in milliseconds.
        /// Will be used to exponentially increase the wait after each
        /// retry.
        /// </summary>
        public int RetrySleepInterval { get; private set; }

        /// <summary>
        /// The timeout in seconds to for the test runner to successfully 
        /// inject itself into the Visual Studio .NET runtime.
        /// </summary>
        public int StartupTimeoutSeconds { get; private set; }

        /// <summary>
        /// Wait time in seconds for Visual Studio to warm up 
        /// before injecting into the .NET runtime in it.
        /// </summary>
        public int WarmupSeconds { get; private set; }
    }
}
