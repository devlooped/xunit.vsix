using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Win32;
using NuGet.Versioning;
using Xunit.Properties;

namespace Xunit
{
    /// <summary>
    /// Processes and loads available VS versions.
    /// </summary>
    internal class VsVersions
    {
        private static readonly TraceSource s_tracer = Constants.Tracer;

        public static VsVersions Default { get; }

        readonly List<NuGetVersion> installedSemVer;

        public VsVersions(string currentVersion, string latestVersion, string[] installedVersions)
        {
            CurrentVersion = currentVersion;
            LatestVersion = latestVersion;
            InstalledVersions = installedVersions;
            installedSemVer = installedVersions.Select(x => NuGetVersion.Parse(x)).ToList();
        }

        static VsVersions()
        {
            var versions = VsSetup.GetInstalled().Distinct().ToList();
            versions.Sort();

            var installedVersions = versions.Distinct().ToArray();
            var latestVersion = installedVersions.LastOrDefault();
            var currentVersion = default(string);

            s_tracer.TraceInformation(Strings.VsVersions.InstalledVersions(string.Join(", ", installedVersions)));
            s_tracer.TraceInformation(Strings.VsVersions.LatestVersion(latestVersion));

            if (Environment.GetEnvironmentVariable("VisualStudioVersion") is string envVersion && 
                !string.IsNullOrEmpty(envVersion))
            {
                var iniPath = Path.Combine(Environment.GetEnvironmentVariable("VSAPPIDDIR"), "devenv.isolation.ini");
                // The above envvar will be 17.0 for 17.x, so first try to get the specific semver, if possible.
                if (File.Exists(iniPath) && SemanticVersion.TryParse(File
                    .ReadAllLines(iniPath)
                    .FirstOrDefault(line => line.StartsWith("SemanticVersion="))?
                    .Substring("SemanticVersion=".Length), 
                    out var semVer)) 
                {
                    envVersion = semVer.Major + "." + semVer.Minor;
                }

                // Ensures we get one of the installed ones. If the envvar results in no installed one, that's a bug.
                currentVersion = installedVersions.FirstOrDefault(x => x == envVersion);
                Debug.Assert(currentVersion != null);
                s_tracer.TraceInformation(Strings.VsVersions.CurrentVersion(currentVersion));
            }

            Default = new VsVersions(currentVersion, latestVersion, installedVersions);
        }

        public string CurrentVersion { get; }

        public string LatestVersion { get; }

        public string[] InstalledVersions { get; }

        /// <summary>
        /// Converts the token values for All, Current and Latest to their actual
        /// values, and returns a distinct list.
        /// </summary>
        public string[] GetFinalVersions(string[] candidateVersions, NuGetVersion minimumVersion = null, NuGetVersion maximumVersion = null)
        {
            // If no version is specified, we default to current or latest.
            if (candidateVersions == null || candidateVersions.Length == 0)
                return new[] { CurrentVersion ?? LatestVersion };

            var vsVersions = candidateVersions.SelectMany(version =>
                version == VisualStudioVersion.All ?
                InstalledVersions :
                version == VisualStudioVersion.Current ?
                new[] { "[" + CurrentVersion + "]" ?? "[" + LatestVersion + "]" } :
                version == VisualStudioVersion.Latest ?
                new[] { "[" + LatestVersion + "]" } :
                new[] { version }).ToList();

            var ranges = vsVersions.Distinct().Select(x => VersionRange.Parse(x)).ToList();

            var final = installedSemVer.Where(semVer =>
            {
                if (minimumVersion != null && semVer < minimumVersion)
                    return false;
                if (maximumVersion != null && semVer > maximumVersion)
                    return false;

                return ranges.Any(range => range.Satisfies(semVer));
            });

            return final.Select(x => x.Major + "." + x.Minor).ToArray();
        }
    }
}
