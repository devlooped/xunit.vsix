using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.ComTypes;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Threading;
using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit.Sdk;
using static ThisAssembly;

namespace Xunit
{
    class VsClient : IVsClient
    {
        static readonly TraceSource s_tracer = Constants.Tracer;

        string _visualStudioVersion;
        string _pipeName;
        string _rootSuffix;
        string _devEnvPath;

        IChannel _clientChannel;
        IVsRemoteRunner _runner;
        VsixRunnerSettings _settings;
        string _emptySettings;

        ConcurrentDictionary<IMessageBus, RemoteMessageBus> _remoteBuses = new ConcurrentDictionary<IMessageBus, RemoteMessageBus>();
        ConcurrentBag<MarshalByRefObject> _remoteObjects = new ConcurrentBag<MarshalByRefObject>();

        public VsClient(string visualStudioVersion, string rootSuffix, VsixRunnerSettings settings)
        {
            // NOTE: the EmptyStartup.vssettings file is included via .targets as a content file 
            // from the xunit.vsix package. This means it will be copied to the output, which would 
            // be the current directory when the test is run.
            _emptySettings = "EmptyStartup.vssettings";
            if (!File.Exists(_emptySettings))
            {
                _emptySettings = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), _emptySettings);
                if (!File.Exists(_emptySettings))
                    throw new InvalidOperationException("Unable to find EmptyStartup.vssettings to properly reset the environment");
            }

            _visualStudioVersion = visualStudioVersion;
            _rootSuffix = rootSuffix;
            _settings = settings;

            _devEnvPath = GetDevEnvPath();
        }

        public Process Process { get; private set; }

        public void Recycle()
        {
            Stop();
        }

        public async Task<RunSummary> RunAsync(VsixTestCase vsixTest, IMessageBus messageBus, ExceptionAggregator aggregator)
        {
            // We don't apply retry behavior when a debugger is attached, since that
            // typically means the developer is actually debugging a failing test.
#if !DEBUG
            if (Debugger.IsAttached)
            {
                return await RunAsyncCore(vsixTest, messageBus, aggregator);
            }
#endif

            using var bufferBus = new InterceptingMessageBus(messageBus);
            var summary = await RunAsyncCore(vsixTest, messageBus, aggregator);
            var shouldRecycle = vsixTest.RecycleOnFailure.GetValueOrDefault();

            // Special case for MEF cache corruption, clear cache and restart the test.
            if (summary.Failed != 0 && (
                (aggregator.HasExceptions && aggregator.ToException().GetType().FullName == "Microsoft.VisualStudio.ExtensibilityHosting.InvalidMEFCacheException") ||
                (bufferBus.Messages.OfType<IFailureInformation>().Where(fail => fail.ExceptionTypes.Any(type => type == "Microsoft.VisualStudio.ExtensibilityHosting.InvalidMEFCacheException")).Any())
                ))
            {
                shouldRecycle = true;
                try
                {
                    var path = VsSetup.GetComponentModelCachePath(_devEnvPath, new Version(_visualStudioVersion), _rootSuffix);
                    if (Directory.Exists(path))
                        Directory.Delete(path, true);
                }
                catch (IOException)
                {
                    s_tracer.TraceEvent(TraceEventType.Warning, 0, "Failed to clear MEF cache after a failed test caused by an InvalidMEFCacheException.");
                }
            }

            if (summary.Failed != 0 && shouldRecycle)
            {
                Recycle();
                aggregator.Clear();
                return await RunAsyncCore(vsixTest, messageBus, aggregator);
            }

            return summary;
        }

        async Task<RunSummary> RunAsyncCore(VsixTestCase testCase, IMessageBus messageBus, ExceptionAggregator aggregator)
        {
            if (!EnsureConnected(testCase, messageBus))
            {
                return new RunSummary
                {
                    Failed = 1,
                };
            }

            var xunitTest = new XunitTest(testCase, testCase.DisplayName);

            try
            {
                var remoteBus = _remoteBuses.GetOrAdd(messageBus, bus =>
                {
                    var instance = new RemoteMessageBus(bus);
                    _remoteObjects.Add(instance);
                    return instance;
                });
                var outputBus = new TraceOutputMessageBus(remoteBus);
                var summary = await Task.Run(
                    () => _runner.Run(testCase, outputBus))
                    .TimeoutAfter(testCase.TimeoutSeconds * 1000);

                if (summary.Exception != null)
                    aggregator.Add(summary.Exception);

                return summary.ToRunSummary();
            }
            catch (Exception ex)
            {
                if (ex is RemotingException || ex is TimeoutException)
                    Stop();

                if (ex is RemotingException)
                    ex = new Exception("Connection to running IDE lost.");

                aggregator.Add(ex);
                messageBus.QueueMessage(new TestFailed(xunitTest, 0, ex.Message, ex));
                return new RunSummary
                {
                    Failed = 1
                };
            }
        }

        public void Dispose()
        {
            Stop();
        }

        bool EnsureConnected(VsixTestCase testCase, IMessageBus messageBus)
        {
            if (!EnsureStarted(testCase, messageBus))
                return false;

            if (_runner == null)
            {
                var hostUrl = RemotingUtil.GetHostUri(_pipeName);
                var clientPipeName = Guid.NewGuid().ToString("N");

                _clientChannel = RemotingUtil.CreateChannel(Constants.ClientChannelName + clientPipeName, clientPipeName);
                _runner = (IVsRemoteRunner)RemotingServices.Connect(typeof(IVsRemoteRunner), hostUrl);
            }

            var retries = 0;
            var connected = false;
            var sleep = _settings.RetrySleepInterval;
            while (retries++ <= _settings.RemoteConnectionRetries && !(connected = TryPing(_runner)))
            {
                Stop();
                if (!EnsureStarted(testCase, messageBus))
                    return false;

                if (_runner == null)
                {
                    var hostUrl = RemotingUtil.GetHostUri(_pipeName);
                    var clientPipeName = Guid.NewGuid().ToString("N");

                    _clientChannel = RemotingUtil.CreateChannel(Constants.ClientChannelName + clientPipeName, clientPipeName);
                    _runner = (IVsRemoteRunner)RemotingServices.Connect(typeof(IVsRemoteRunner), hostUrl);
                }

                Thread.Sleep(sleep);
                sleep = sleep * retries;
            }

            if (!connected)
            {
                Stop();
                var message = Strings.VsClient.FailedToConnect(testCase.VisualStudioVersion, testCase.RootSuffix);
                messageBus.QueueMessage(new TestFailed(new XunitTest(testCase, testCase.DisplayName), 0, message, new InvalidOperationException(message)));
                return false;
            }

            var remoteVars = _runner.GetEnvironment();

            Constants.Tracer.TraceEvent(TraceEventType.Verbose, 0, Strings.VsClient.RemoteEnvVars(string.Join(Environment.NewLine,
                remoteVars.Select(x => "    " + x[0] + "=" + x[1]))));

            return true;
        }

        bool EnsureStarted(VsixTestCase testCase, IMessageBus messageBus)
        {
            if (Process == null)
            {
                var sleep = _settings.RetrySleepInterval;
                var retries = 0;
                var started = false;
                while (retries++ <= _settings.ProcessStartRetries && !(started = Start()))
                {
                    Stop();
                    Thread.Sleep(sleep);
                    sleep = sleep * retries;
                }

                if (!started)
                {
                    Stop();

                    s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToStart(_visualStudioVersion, _rootSuffix));
                    messageBus.QueueMessage(new TestFailed(new XunitTest(testCase, testCase.DisplayName), 0,
                        Strings.VsClient.FailedToStart(_visualStudioVersion, _rootSuffix),
                        new TimeoutException()));

                    return false;
                }
            }

            return true;
        }

        bool TryPing(IVsRemoteRunner runner)
        {
            try
            {
                runner.Ping();
                return true;
            }
            catch (RemotingException)
            {
                return false;
            }
        }

        bool Start()
        {
            _pipeName = Guid.NewGuid().ToString();
            var args = "";

            if (Version.Parse(_visualStudioVersion) >= new Version("16.0"))
                args = $"/ResetSettings {_emptySettings} ";

            if (!string.IsNullOrEmpty(_rootSuffix))
                args += "/NoSplash /RootSuffix " + _rootSuffix;

            var info = new ProcessStartInfo(_devEnvPath, args + " /log")
            {
                UseShellExecute = false,
                WorkingDirectory = Directory.GetCurrentDirectory(),
            };

            // This environment variable is used by the VsRemoveRunner to set up the right
            // server channel named pipe, which is later used by the test runner to execute
            // tests in the VS app domain.
            info.EnvironmentVariables[Constants.PipeNameEnvironmentVariable] = _pipeName;
            info.EnvironmentVariables[Constants.BaseDirectoryEnvironmentVariable] = Directory.GetCurrentDirectory();
            // Allow debugging xunit.vsix itself by setting the `xunit.vsix.debug=true` envvar in the current VS.
            info.EnvironmentVariables[Constants.DebugEnvironmentVariable] = Environment.GetEnvironmentVariable(Constants.DebugEnvironmentVariable);
            // Allow debugging the tests via CLI
            info.EnvironmentVariables[Constants.DebugRemoteEnvironmentVariable] = Environment.GetEnvironmentVariable(Constants.DebugRemoteEnvironmentVariable);

            // Propagate profiling values to support OpenCover or any third party profiler
            // already attached to the current process.
            PropagateProfilingVariables(info);

            Constants.Tracer.TraceEvent(TraceEventType.Verbose, 0, Strings.VsClient.RunnerEnvVars(string.Join(Environment.NewLine, Environment
                .GetEnvironmentVariables()
                .OfType<DictionaryEntry>()
                .OrderBy(x => (string)x.Key)
                .Where(x =>
                   !((string)x.Key).Equals("path", StringComparison.OrdinalIgnoreCase) &&
                   !((string)x.Key).Equals("pathbackup", StringComparison.OrdinalIgnoreCase))
                .Select(x => "    " + x.Key + "=" + x.Value))));

            // Eat the standard output to prevent this from polluting AppVeyor or other CI systems 
            // that capture processes standard output.
            info.RedirectStandardOutput = true;
            info.RedirectStandardInput = true;
            info.RedirectStandardError = true;

            Process = Process.Start(info);

            // This forces us to wait until VS is fully started.
            var dte = RunningObjects.GetDTE(_visualStudioVersion, Process.Id, TimeSpan.FromSeconds(_settings.StartupTimeout));
            if (dte == null)
                return false;

            //var services = new OleServiceProvider(dte);
            // These casts don't work on this side of the client, for some reason. 
            //IVsShell shell;
            //while ((shell = services.GetService<SVsShell, IVsShell>()) == null)
            //{
            //    Thread.Sleep(_settings.RetrySleepInterval);
            //}

            //object zombie;
            //// __VSSPROPID.VSSPROPID_Zombie
            //while ((int?)(zombie = shell.GetProperty(-9014, out zombie)) != 0)
            //{
            //    Thread.Sleep(_settings.RetrySleepInterval);
            //}

            // Retrieve the component model service, which could also now take time depending on new
            // extensions being installed or updated before the first launch.
            //var components = services.GetService<Interop.SComponentModel, object>();

            //if (Debugger.IsAttached)
            //{
            //    // When attached via TD.NET, there will be an environment variable named DTE_MainWindow=2296172
            //    var mainWindow = Environment.GetEnvironmentVariable("DTE_MainWindow");
            //    if (!string.IsNullOrEmpty(mainWindow))
            //    {
            //        var attached = false;
            //        var retries = 0;
            //        var sleep = _settings.RetrySleepInterval;
            //        while (retries++ < _settings.DebuggerAttachRetries && !attached)
            //        {
            //            try
            //            {
            //                var mainHWnd = int.Parse(mainWindow);
            //                var mainDte = GetAllDtes().FirstOrDefault(x => x.MainWindow.HWnd == mainHWnd);
            //                if (mainDte != null)
            //                {
            //                    var startedVs = mainDte.Debugger.LocalProcesses.OfType<EnvDTE.Process>().FirstOrDefault(x => x.ProcessID == Process.Id);
            //                    if (startedVs != null)
            //                    {
            //                        startedVs.Attach();
            //                        attached = true;
            //                        break;
            //                    }
            //                }
            //            }
            //            catch (Exception ex)
            //            {
            //                s_tracer.TraceEvent(TraceEventType.Warning, 0, Strings.VsClient.RetryAttach(retries, _settings.DebuggerAttachRetries) + Environment.NewLine + ex.ToString());
            //            }

            //            Thread.Sleep(sleep);
            //            sleep = sleep * retries;
            //        }

            //        if (!attached)
            //            s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToAttach(_visualStudioVersion, _rootSuffix));
            //    }
            //}

            try
            {
                NativeMethods.IsWow64Process(Process.Handle, out var isWow);
                var platform = isWow ? "x86" : "x64";
                var thisFile = Assembly.GetExecutingAssembly().Location;
                if (thisFile.StartsWith(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)))
                {
                    s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToInject(Process.Id) + ": xunit.vsix seems to be running in shadow copy mode, which is not supported.");
                    return false;
                }

                var toolPath = Path.Combine(Path.GetDirectoryName(thisFile), "Injector", platform, "Injector.exe");
                if (!File.Exists(toolPath))
                {
                    s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToInject(Process.Id) + $": could not find .NET injector helper at {toolPath}.");
                    return false;
                }

                var launchDebugger = bool.TryParse(Environment.GetEnvironmentVariable(Constants.DebugEnvironmentVariable), out var shouldDebug) && shouldDebug;

#if DEBUG
                // We'll only auto-launch on the client side of the debugger for debug builds of 
                // xunit.vsix (local dev) *and* an already attached debugger on the xunit side.
                launchDebugger |= Debugger.IsAttached;
#endif

                var injector = Process.Start(
                    new ProcessStartInfo(toolPath,
                        Process.MainWindowHandle + " " +
                        thisFile + " " +
                        typeof(VsStartup).FullName + " " +
                        nameof(VsStartup.Start))
                    {
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                    });

                // Wait max 10 seconds for the injection to succeed
                if (!injector.WaitForExit(10000))
                {
                    s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToInject(Process.Id));
                    return false;
                }
            }
            catch (Exception ex)
            {
                s_tracer.TraceEvent(TraceEventType.Error, 0, Strings.VsClient.FailedToInject(Process.Id) + Environment.NewLine + ex.ToString());
                return false;
            }

            return true;
        }

        void PropagateProfilingVariables(ProcessStartInfo info)
        {
            var allVars = Environment.GetEnvironmentVariables()
                .OfType<DictionaryEntry>()
                .Select(x => new KeyValuePair<string, string>((string)x.Key, (string)x.Value))
                .ToList();

            foreach (var envVar in allVars
                .Where(x => x.Key.StartsWith("Cor_", StringComparison.OrdinalIgnoreCase) ||
                   x.Key.StartsWith("CorClr_", StringComparison.OrdinalIgnoreCase) ||
                   x.Key.StartsWith("CoreClr_", StringComparison.OrdinalIgnoreCase) ||
                   x.Key.StartsWith("OpenCover_", StringComparison.OrdinalIgnoreCase))
                .Where(x => !info.EnvironmentVariables.ContainsKey(x.Key)))
            {
                info.EnvironmentVariables.Add(envVar.Key, envVar.Value);
            }
        }

        void Stop()
        {
            try
            {
                if (_runner != null)
                    _runner.Dispose();
            }
            catch { }

            foreach (var mbr in _remoteObjects)
            {
                try
                {
                    if (mbr is IDisposable disposable)
                        disposable.Dispose();
                }
                catch { }

                try
                {
                    RemotingServices.Disconnect(mbr);
                }
                catch { }
            }

            if (_clientChannel != null)
                ChannelServices.UnregisterChannel(_clientChannel);

            _clientChannel = null;
            _runner = null;

            if (Process != null)
            {
                if (!Process.HasExited)
                    Process.Kill();

                Process = null;
            }
        }

        string GetDevEnvPath()
        {
            var version = new Version(_visualStudioVersion);
            if (version.Major >= 15)
            {
                return VsSetup.GetDevEnv(version);
            }

            var varName = "VS" + _visualStudioVersion.Replace(".", "") + "COMNTOOLS";
            var varValue = Environment.GetEnvironmentVariable(varName);
            if (string.IsNullOrEmpty(varValue) || !Directory.Exists(varValue))
                throw new ArgumentException(string.Format("Visual Studio Version '{0}' path was not found in environment variable '{1}'.", _visualStudioVersion, varName));

            // Path is like: C:\Program Files (x86)\Microsoft Visual Studio 10.0\Common7\Tools\..\IDE\devenv.exe
            // We need to move up one level and down to IDE for the final devenv path.
            var path = Path.GetFullPath(Path.Combine(varValue, @"..\IDE\devenv.exe"));
            if (!File.Exists(path))
                throw new ArgumentException(string.Format("Visual Studio Version '{0}' executable was not found at the expected location '{1}' according to the environment variable '{2}'.", _visualStudioVersion, path, varName));

            return path;
        }

        IEnumerable<Interop.DTE> GetAllDtes()
        {
            IRunningObjectTable table;
            IEnumMoniker moniker;
            if (ErrorHandler.Failed(NativeMethods.GetRunningObjectTable(0, out table)))
                yield break;

            table.EnumRunning(out moniker);
            moniker.Reset();
            var pceltFetched = IntPtr.Zero;
            var rgelt = new IMoniker[1];

            while (moniker.Next(1, rgelt, pceltFetched) == 0)
            {
                IBindCtx ctx;
                if (!ErrorHandler.Failed(NativeMethods.CreateBindCtx(0, out ctx)))
                {
                    string displayName;
                    rgelt[0].GetDisplayName(ctx, null, out displayName);
                    if (displayName.Contains("VisualStudio.DTE"))
                    {
                        object comObject;
                        table.GetObject(rgelt[0], out comObject);
                        yield return (Interop.DTE)comObject;
                    }
                }
            }
        }

        class TraceOutputMessageBus : LongLivedMarshalByRefObject, IMessageBus
        {
            IMessageBus _innerBus;

            public TraceOutputMessageBus(IMessageBus innerBus)
            {
                _innerBus = innerBus;
            }

            public void Dispose()
            {
                try
                {
                    _innerBus.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    // Others consuming the inner bus could dispose it before us.
                }
            }

            public bool QueueMessage(IMessageSinkMessage message)
            {
                var resultMessage = message as ITestResultMessage;
                var traceMessage = message as TraceOutputMessage;

                var output = resultMessage?.Output ?? traceMessage?.Message;

                if (!string.IsNullOrEmpty(output))
                {
                    Trace.WriteLine(output);
                    Debugger.Log(0, "", output);
                    Console.WriteLine(output);
                }

                return _innerBus.QueueMessage(message);
            }
        }
    }
}
