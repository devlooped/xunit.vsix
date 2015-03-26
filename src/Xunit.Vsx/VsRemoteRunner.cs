using System;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Reflection;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
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

		/// <summary>
		/// Returns true if the IDE should be restarted.
		/// </summary>
		public bool ShouldRestart ()
		{
			var emGuid = new Guid ("E7576C05-1874-450c-9E98-CF3A0897A069");
			var vsShell = GlobalServices.GetService<SVsShell, IVsShell> ();
			IVsPackage emPackage;
			vsShell.IsPackageLoaded (ref emGuid, out emPackage);
			if (emPackage == null)
				vsShell.LoadPackage (ref emGuid, out emPackage);

			if (emPackage == null)
				throw new InvalidOperationException ("Failed to load the Extension Manager package.");

			dynamic manager = GlobalServices.GetService (new Guid ("316F4DE6-3CA4-4f0d-B003-962D28F65238"));
			if (manager == null)
				throw new InvalidOperationException ("Failed to retrieve the Extension Manager service.");

			try {
				dynamic extension = ((object)manager.GetInstalledExtension (Constants.VsixIdentifier)).AsDynamicReflection ();
				var enabled = (int)extension.State;
				if (enabled != 1) {
					var restart = (int)manager.Enable (extension.target);
					if (restart != 0)
						return true;
				}
			} catch (Exception ex) {
				if (ex.GetType ().Name == "NotInstalledException") {
					dynamic dte = GlobalServices.GetService (new Guid ("04A72314-32E9-48E2-9B87-A63603454F3E"));
					// Would be for example "12.0Exp"
					string rootSuffix = Path.GetFileName (dte.RegistryRoot);

					var vsixPath = Path.Combine (Path.GetTempPath (), @"Xamarin\xunit.vsx", rootSuffix + ".vsix");
					if (!File.Exists (vsixPath))
						throw new InvalidOperationException ("Did not find the Xunit VSX extension installer at " + vsixPath + ".");

					var createMethod = ((object)manager).GetType ().GetMethod ("CreateInstallableExtension", BindingFlags.Static | BindingFlags.Public);
					dynamic extension = createMethod.Invoke (null, new object[] { vsixPath });

					var restart = (int)manager.Install (extension, false);
					if (restart != 0)
						return true;
				}
			}

			return false;
		}

		public void AddListener (TraceListener listener)
		{
			Trace.Listeners.Add (listener);
		}

		public VsxRunSummary Run (VsxTestCase testCase, IMessageBus messageBus, object[] constructorArguments)
		{
			var aggregator = new ExceptionAggregator ();

			var result = new XunitTestCaseRunner (
					testCase, testCase.DisplayName, testCase.SkipReason, constructorArguments, testCase.TestMethodArguments, messageBus,
					aggregator, new CancellationTokenSource ())
				.RunAsync ()
				.Result
				.ToVsxRunSummary ();

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
