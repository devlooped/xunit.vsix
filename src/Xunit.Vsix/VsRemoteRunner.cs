using System;
using System.Diagnostics;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using Xunit.Sdk;

namespace Xunit
{
	/// <summary>
	/// Remote runner instance running in the IDE AppDomain/process
	/// to execute tests on.
	/// </summary>
	class VsRemoteRunner : MarshalByRefObject, IVsRemoteRunner
	{
		string pipeName;
		IChannel channel;

		public VsRemoteRunner ()
		{
			pipeName = Environment.GetEnvironmentVariable (Constants.PipeNameEnvironmentVariable);

			RemotingServices.Marshal (this, RemotingUtil.HostName);
		}

		public void AddListener (TraceListener listener)
		{
			Trace.Listeners.Add (listener);
		}

		public VsixRunSummary Run (VsixTestCase testCase, IMessageBus messageBus)
		{
			var aggregator = new ExceptionAggregator ();

			var result = new XunitTestCaseRunner (
					testCase, testCase.DisplayName, testCase.SkipReason,
					testCase.HasTestOutput ? new[] { new TestOutputHelper() } : new object[0],
					testCase.TestMethodArguments, messageBus,
					aggregator, new CancellationTokenSource ())
				.RunAsync ()
				.Result
				.ToVsixRunSummary ();

			if (aggregator.HasExceptions)
				result.Exception = aggregator.ToException ();

			return result;
		}

		/// <summary>
		/// Invoked by the <see cref="VsStartup.Start"/> injected managed method in the
		/// VS process.
		/// </summary>
		public void Start ()
		{
			channel = RemotingUtil.CreateChannel (Constants.ServerChannelName, pipeName);
		}

		public override object InitializeLifetimeService ()
		{
			return null;
		}
	}
}
