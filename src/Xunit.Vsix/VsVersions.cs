using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Setup.Configuration;
using Microsoft.Win32;
using Xunit.Properties;

namespace Xunit
{
    /// <summary>
    /// Processes and loads available VS versions.
    /// </summary>
    internal static class VsVersions
    {
        private static readonly TraceSource s_tracer = Constants.Tracer;

        static VsVersions()
        {
            var versions = new List<string>();
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\VisualStudio\SxS\VS7"))
            {
                versions.AddRange(key
                    .GetValueNames()
                    .Where(version => Directory.Exists(Path.Combine((string)key.GetValue(version), "VSSDK"))));
            }

            var query = (ISetupConfiguration2)new SetupConfiguration();
            var e = query.EnumAllInstances();
            var helper = (ISetupHelper)query;
            var instances = new List<ISetupInstance2>();
            var result = new ISetupInstance[1];
            int fetched;
            do
            {
                e.Next(1, result, out fetched);
                if (fetched > 0)
                    instances.Add((ISetupInstance2)result[0]);
            } while (fetched > 0);

            var vs2017 = from instance in instances
                         let state = instance.GetState()
                         where state == InstanceState.Complete &&
                         (state & InstanceState.Local) == InstanceState.Local &&
                         // Require the VSSDK workload, just like we do for pre-2017 VS
                         instance.GetPackages().Any(package => package.GetId() == "Microsoft.VisualStudio.Workload.VisualStudioExtension")
                         let productVersion = (string)(instance as ISetupInstanceCatalog)?.GetCatalogInfo()?.GetValue("productSemanticVersion")
                         where productVersion != null
                         let semver = NuGet.Versioning.SemanticVersion.Parse(productVersion)
                         select new Version(semver.Major, 0);

            versions.AddRange(vs2017.Distinct().Select(v => v.ToString()));
            versions.Sort();

            InstalledVersions = versions.Distinct().ToArray();
            LatestVersion = InstalledVersions.LastOrDefault();
            s_tracer.TraceInformation(Strings.VsVersions.InstalledVersions(string.Join(", ", InstalledVersions)));
            s_tracer.TraceInformation(Strings.VsVersions.LatestVersion(LatestVersion));

            var currentVersion = Environment.GetEnvironmentVariable("VisualStudioVersion");
            if (!string.IsNullOrEmpty(currentVersion) && InstalledVersions.Contains(currentVersion))
            {
                CurrentVersion = currentVersion;
                s_tracer.TraceInformation(Strings.VsVersions.CurrentVersion(currentVersion));
            }
        }

        public static string CurrentVersion { get; private set; }

        public static string LatestVersion { get; private set; }

        public static string[] InstalledVersions { get; private set; }

        /// <summary>
        /// Converts the token values for All, Current and Latest to their actual
        /// values, and returns a distinct list.
        /// </summary>
        public static string[] GetFinalVersions(string[] candidateVersions, string minimumVersion, string maximumVersion)
        {
            // If no version is specified, we default to current or latest.
            if (candidateVersions == null || candidateVersions.Length == 0)
                return new[] { CurrentVersion ?? LatestVersion };

            var vsVersions = candidateVersions.ToList();

            if (vsVersions.Any(vs => vs == VisualStudioVersion.All))
            {
                vsVersions.AddRange(InstalledVersions);
                vsVersions.RemoveAll(vs => vs == VisualStudioVersion.All);
            }
            if (vsVersions.Any(vs => vs == VisualStudioVersion.Current))
            {
                vsVersions.Add(CurrentVersion ?? LatestVersion);
                vsVersions.RemoveAll(vs => vs == VisualStudioVersion.Current);
            }
            if (vsVersions.Any(vs => vs == VisualStudioVersion.Latest))
            {
                vsVersions.Add(LatestVersion);
                vsVersions.RemoveAll(vs => vs == VisualStudioVersion.Latest);
            }

            if (!string.IsNullOrEmpty(minimumVersion))
                vsVersions.RemoveAll(vs => vs.CompareTo(minimumVersion) == -1);
            if (!string.IsNullOrEmpty(maximumVersion))
                vsVersions.RemoveAll(vs => vs.CompareTo(maximumVersion) == 1);

            return vsVersions.Distinct().ToArray();
        }
    }
}
