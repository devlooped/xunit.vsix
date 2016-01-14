using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xunit
{
	internal class VsixRunnerSettings
	{
		public VsixRunnerSettings (int? debuggerAttachRetries = null, int? remoteConnectionRetries = null,
			int? processStartRetries = null, int? retrySleepInterval = null,
			int? startupTimeout = null)
		{
			DebuggerAttachRetries = debuggerAttachRetries ?? 5;
			RemoteConnectionRetries = remoteConnectionRetries ?? 2;
			ProcessStartRetries = processStartRetries ?? 1;
			RetrySleepInterval = retrySleepInterval ?? 200;
			StartupTimeout = startupTimeout ?? 300;
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
		/// The timeout in seconds to wait for Visual Studio to
		/// start and initialize its DTE automation model.
		/// </summary>
		public int StartupTimeout { get; private set; }
	}
}
