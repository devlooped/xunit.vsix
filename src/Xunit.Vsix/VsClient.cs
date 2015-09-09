using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Xunit
{
	class VsClient : IVsClient
	{
		const string BindingPathKey = "{30C4F4A2-17C9-470C-AED2-2D4E97CC5686}";

		bool initializedExtension;

		string visualStudioVersion;
		string pipeName;
		string rootSuffix;
		string devEnvPath;

		IChannel clientChannel;
		IVsRemoteRunner runner;

		ConcurrentBag<MarshalByRefObject> remoteObjects = new ConcurrentBag<MarshalByRefObject> ();

		public VsClient (string visualStudioVersion, string pipeName, string rootSuffix)
		{
			this.visualStudioVersion = visualStudioVersion;
			this.pipeName = pipeName;
			this.rootSuffix = rootSuffix;

			devEnvPath = GetDevEnvPath ();
		}

		public Process Process { get; private set; }

		public void Shutdown ()
		{
			Stop ();
		}

		public async Task<RunSummary> RunAsync (VsixTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator, object[] constructorArguments)
		{
			if (Process == null) {
				if (!Start ()) {
					Stop ();
					if (!Start ()) {
						Stop ();

						messageBus.QueueMessage (new TestFailed (new XunitTest (testCase, testCase.DisplayName), 0,
							string.Format ("Failed to start Visual Studio {0}{1}.", visualStudioVersion, rootSuffix),
							new TimeoutException ()));

						return new RunSummary {
							Failed = 1,
						};
					}
				}
			}

			if (runner == null) {
				var hostUrl = RemotingUtil.GetHostUri (pipeName);
				var clientPipeName = Guid.NewGuid ().ToString ("N");

				clientChannel = RemotingUtil.CreateChannel (Constants.ClientChannelName + clientPipeName, clientPipeName);

				try {
					runner = (IVsRemoteRunner)RemotingServices.Connect (typeof (IVsRemoteRunner), hostUrl);
					// We don't require restart anymore since we write to the registry directly the binding paths,
					// rather than installing a VSIX
					//if (runner.ShouldRestart ()) {
					//    Stop ();
					//    return await RunAsync (testCase, messageBus, aggregator, constructorArguments);
					//}

					if (Debugger.IsAttached) {
						// Add default trace listeners to the remote process.
						foreach (var listener in Trace.Listeners.OfType<TraceListener> ()) {
							runner.AddListener (listener);
						}
					}

				} catch (Exception ex) {
					messageBus.QueueMessage (new TestFailed (new XunitTest (testCase, testCase.DisplayName), 0, ex.Message, ex));
					return new RunSummary {
						Failed = 1
					};
				}
			}

			var xunitTest = new XunitTest (testCase, testCase.DisplayName);

			try {
				var outputHelper = constructorArguments.OfType<TestOutputHelper> ().FirstOrDefault ();
				if (outputHelper != null)
					outputHelper.Initialize (messageBus, xunitTest);

				// Special case for test output, since it's not MBR.
				var args = constructorArguments.Select (arg => {
					var helper = arg as ITestOutputHelper;
					if (helper != null) {
						var remoteHeper = new RemoteTestOutputHelper (helper);
						remoteObjects.Add (remoteHeper);
						return remoteHeper;
					}

					return arg;
				}).ToArray ();

				var remoteBus = new RemoteMessageBus (messageBus);
				remoteObjects.Add (remoteBus);

				var summary = await System.Threading.Tasks.Task.Run (
					() => runner.Run (testCase, remoteBus, args))
					.TimeoutAfter (testCase.TimeoutSeconds * 1000);

				// Dump output only if a debugger is attached, meaning that most likely
				// there is a single test being run/debugged.
				if (Debugger.IsAttached && outputHelper != null && !string.IsNullOrEmpty (outputHelper.Output)) {
					Trace.WriteLine (outputHelper.Output);
					Debugger.Log (0, "", outputHelper.Output);
					Console.WriteLine (outputHelper.Output);
				}

				if (summary.Exception != null)
					aggregator.Add (summary.Exception);

				return summary.ToRunSummary ();
			} catch (Exception ex) {
				aggregator.Add (ex);
				messageBus.QueueMessage (new TestFailed (xunitTest, 0, ex.Message, ex));
				return new RunSummary {
					Failed = 1
				};
			} finally {
				var outputHelper = constructorArguments.OfType<TestOutputHelper> ().FirstOrDefault ();
				if (outputHelper != null)
					outputHelper.Uninitialize ();
			}
		}

		public void Dispose ()
		{
			Stop ();
			ClearBindingPaths ();
		}

		private bool Start ()
		{
			AddBindingPaths ();

			// This environment variable is used by the VsRemoveRunner to set up the right
			// server channel named pipe, which is later used by the test runner to execute
			// tests in the VS app domain.
			Environment.SetEnvironmentVariable (Constants.PipeNameEnvironmentVariable, pipeName);

			Process = new Process {
				StartInfo = {
					FileName = devEnvPath,
					Arguments = string.IsNullOrEmpty (rootSuffix) ? "" : "/RootSuffix " + rootSuffix,
					UseShellExecute = false,
					WorkingDirectory = Directory.GetCurrentDirectory (),
				},
			};

			Process.Start ();

			// This forces us to wait until VS is fully started.
			var dte = RunningObjects.GetDTE (visualStudioVersion, Process.Id, TimeSpan.FromSeconds (120));
			if (dte == null)
				return false;

			var services = new Microsoft.VisualStudio.Shell.ServiceProvider ((Microsoft.VisualStudio.OLE.Interop.IServiceProvider)dte);
			IVsShell shell;
			while ((shell = (IVsShell)services.GetService (typeof (SVsShell))) == null) {
				System.Threading.Thread.Sleep (100);
			}

			object zombie;
			while ((int?)(zombie = shell.GetProperty ((int)__VSSPROPID.VSSPROPID_Zombie, out zombie)) != 0) {
				System.Threading.Thread.Sleep (100);
			}

			if (Debugger.IsAttached) {
				// When attached via TD.NET, there will be an environment variable named DTE_MainWindow=2296172
				var mainWindow = Environment.GetEnvironmentVariable ("DTE_MainWindow");
				if (!string.IsNullOrEmpty (mainWindow)) {
					var mainHWnd = int.Parse (mainWindow);
					var mainDte = GetAllDtes ().FirstOrDefault (x => x.MainWindow.HWnd == mainHWnd);
					if (mainDte != null) {
						var startedVs = mainDte.Debugger.LocalProcesses.OfType<EnvDTE.Process> ().FirstOrDefault (x => x.ProcessID == Process.Id);
						if (startedVs != null)
							startedVs.Attach ();
					}
				}
			}

			try {
				Injector.Launch (Process.MainWindowHandle,
					this.GetType ().Assembly.Location,
					typeof (VsStartup).FullName, "Start");
			} catch (Exception) {
				return false;
			}

			return true;
		}

		private void AddBindingPaths ()
		{
			if (initializedExtension)
				return;

			// Add all currently loaded assemblies paths to the resolve paths.
			var probingPaths = AppDomain.CurrentDomain.GetAssemblies ()
				.Select (x => Path.GetDirectoryName (x.Location))
				//.Concat (new[] { Directory.GetCurrentDirectory() })
				.Where (x => x.StartsWith (Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData)))
				.Distinct ()
				.ToArray ();

			var extensionsPath = Path.Combine (
				Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData),
				@"Microsoft\VisualStudio",
				visualStudioVersion + rootSuffix,
				"Extensions");

			using (var pathsKey = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\VisualStudio\" +
				visualStudioVersion + rootSuffix + @"_Config\BindingPaths\", true)) {

				var bindingKey = pathsKey.OpenSubKey (BindingPathKey, true);
				if (bindingKey == null)
					bindingKey = pathsKey.CreateSubKey (BindingPathKey);

				using (bindingKey) {
					// Across multiple VsClient sessions within the same
					// test run (i.e. tests that request their own clean
					// instance of VS), these paths won't change.
					var bindingPaths = new HashSet<string>(bindingKey.GetValueNames());

					if (probingPaths.Any (probingPath => !bindingPaths.Contains (probingPath))) {
						// There was a change, meaning it's another run, and typically all
						// assemblies change location because of shadow copying, so we
						// have to refresh all of them.
						foreach (var name in bindingKey.GetValueNames ()) {
							bindingKey.DeleteValue (name);
						}

						foreach (var probingPath in probingPaths) {
							bindingKey.SetValue (probingPath, "");
						}
					}
				}
			}

			initializedExtension = true;
		}

		private void ClearBindingPaths ()
		{
			using (var pathsKey = Registry.CurrentUser.OpenSubKey (@"Software\Microsoft\VisualStudio\" +
				visualStudioVersion + rootSuffix + @"_Config\BindingPaths\", true)) {
				pathsKey.DeleteSubKey (BindingPathKey);
			}
		}


		private void Stop ()
		{
			foreach (var mbr in remoteObjects) {
				try {
					var disposable = mbr as IDisposable;
					if (disposable != null)
						disposable.Dispose ();
				} catch { }

				try {
					RemotingServices.Disconnect (mbr);
				} catch { }
			}

			if (clientChannel != null)
				ChannelServices.UnregisterChannel (clientChannel);

			clientChannel = null;
			runner = null;
			Process.Kill ();
			Process = null;
		}

		private string GetDevEnvPath ()
		{
			var varName = "VS" + visualStudioVersion.Replace (".", "") + "COMNTOOLS";
			var varValue = Environment.GetEnvironmentVariable (varName);
			if (string.IsNullOrEmpty (varValue) || !Directory.Exists (varValue))
				throw new ArgumentException (string.Format ("Visual Studio Version '{0}' path was not found in environment variable '{1}'.", visualStudioVersion, varName));

			// Path is like: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\Tools\..\IDE\devenv.exe
			// We need to move up one level and down to IDE for the final devenv path.
			var path = Path.Combine (varValue, @"..\IDE\devenv.exe");
			if (!File.Exists (path))
				throw new ArgumentException (string.Format ("Visual Studio Version '{0}' executable was not found at the expected location '{1}' according to the environment variable '{2}'.", visualStudioVersion, path, varName));
			return path;
		}

		private IEnumerable<EnvDTE.DTE> GetAllDtes ()
		{
			IRunningObjectTable table;
			IEnumMoniker moniker;
			if (ErrorHandler.Failed (NativeMethods.GetRunningObjectTable (0, out table)))
				yield break;

			table.EnumRunning (out moniker);
			moniker.Reset ();
			var pceltFetched = IntPtr.Zero;
			var rgelt = new IMoniker[1];

			while (moniker.Next (1, rgelt, pceltFetched) == 0) {
				IBindCtx ctx;
				if (!ErrorHandler.Failed (NativeMethods.CreateBindCtx (0, out ctx))) {
					string displayName;
					rgelt[0].GetDisplayName (ctx, null, out displayName);
					if (displayName.Contains ("VisualStudio.DTE")) {
						object comObject;
						table.GetObject (rgelt[0], out comObject);
						yield return (EnvDTE.DTE)comObject;
					}
				}
			}
		}
	}
}
