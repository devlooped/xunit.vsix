using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using NuGet.Versioning;

namespace Xunit
{
    static class RunningObjects
    {
        static readonly Regex s_versionExpr = new Regex(@"Microsoft Visual Studio (?<version>\d\d\.\d)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public static Interop.DTE GetDTE(TimeSpan retryTimeout)
        {
            var processId = Process.GetCurrentProcess().Id;
            var devEnv = Process.GetCurrentProcess().MainModule.FileName;

            if (Path.GetFileName(devEnv) != "devenv.exe")
                throw new NotSupportedException("Can only retrieve the current DTE from a running devenv.exe instance.");

            // C:\Program Files (x86)\Microsoft Visual Studio 12.0\Common7\IDE\devenv.exe
            var version = s_versionExpr.Match(devEnv).Groups["version"];
            if (!version.Success)
            {
                var ini = Path.ChangeExtension(devEnv, "isolation.ini");
                if (!File.Exists(ini))
                    throw new NotSupportedException("Could not determine Visual Studio version from running process from " + devEnv);

                var semver = File.ReadAllLines(ini)
                    .Where(line => line.StartsWith("SemanticVersion=", StringComparison.Ordinal))
                    .Select(line => SemanticVersion.Parse(line.Substring(16)))
                    .FirstOrDefault();

                if (semver == null)
                    throw new NotSupportedException("Could not determine the SemanticVersion for Visual Studio from devenv.isolation.ini at " + ini);

                return GetComObject<Interop.DTE>(string.Format("!{0}.{1}.0:{2}",
                    "VisualStudio.DTE", semver.Major, processId), retryTimeout);
            }
            else
            {
                return GetComObject<Interop.DTE>(string.Format("!{0}.{1}:{2}",
                    "VisualStudio.DTE", version.Value, processId), retryTimeout);
            }
        }

        public static bool FindDTE(Version visualStudioVersion, int processId)
            => FindMoniker($"!VisualStudio.DTE.{visualStudioVersion.Major}.0:{processId}") != null;

        public static Interop.DTE GetDTE(string visualStudioVersion, int processId, TimeSpan retryTimeout)
        {
            var version = Version.Parse(visualStudioVersion);
            return GetComObject<Interop.DTE>(string.Format("!{0}.{1}:{2}",
                "VisualStudio.DTE", version.Major + ".0", processId), retryTimeout);
        }

        public static T GetComObject<T>(string monikerName, TimeSpan retryTimeout)
        {
            object comObject;
            var stopwatch = Stopwatch.StartNew();
            do
            {
                comObject = GetComObject(monikerName);
                if (comObject != null)
                    break;

                System.Threading.Thread.Sleep(100);
            }

            while (stopwatch.Elapsed < retryTimeout);

            return (T)comObject;
        }

        static object GetComObject(string monikerName)
        {
            var moniker = FindMoniker(monikerName);
            if (moniker == null)
                return null;

            if (ErrorHandler.Succeeded(NativeMethods.GetRunningObjectTable(0, out var rdt)) &&
                ErrorHandler.Succeeded(rdt.GetObject(moniker, out var comObject)))
                return comObject;

            return null;
        }

        static IMoniker? FindMoniker(string monikerName)
        {
            try
            {
                IRunningObjectTable table;
                IEnumMoniker moniker;
                if (ErrorHandler.Failed(NativeMethods.GetRunningObjectTable(0, out table)))
                    return null;

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
                        if (displayName == monikerName)
                        {
                            return rgelt[0];
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            return null;
        }

    }
}
