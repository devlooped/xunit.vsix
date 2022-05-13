using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NuGet.Versioning;
using static ThisAssembly;

namespace Xunit
{
    /// <summary>
    /// Processes and loads available VS versions.
    /// </summary>
    class VsVersions
    {
        static readonly TraceSource s_tracer = Constants.Tracer;

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
                var devEnvDir = Environment.GetEnvironmentVariable("DevEnvDir") ??
                    Environment.GetEnvironmentVariable("VSAPPIDDIR");

                if (devEnvDir == null &&
                    Environment.GetEnvironmentVariable("VSINSTALLDIR") is string vsInstallDir)
                    devEnvDir = Path.Combine(vsInstallDir, @"Common7\IDE");

                if (!string.IsNullOrEmpty(devEnvDir) && Directory.Exists(devEnvDir))
                {
                    var iniPath = Path.Combine(devEnvDir, "devenv.isolation.ini");
                    // The above envvar will be 17.0 for 17.x, so first try to get the specific semver, if possible.
                    if (File.Exists(iniPath) && SemanticVersion.TryParse(File
                        .ReadAllLines(iniPath)
                        .FirstOrDefault(line => line.StartsWith("SemanticVersion="))?
                        .Substring("SemanticVersion=".Length),
                        out var semVer))
                    {
                        envVersion = semVer.Major + "." + semVer.Minor;
                    }
                }

                // Ensures we get one of the installed ones.
                currentVersion = installedVersions.FirstOrDefault(x => x == envVersion) ??
                    // If we can't find exact match, assume lowest version that starts with 
                    // same major version. Note installedVersions is sorted ascending already.
                    installedVersions.FirstOrDefault(x => x.StartsWith(envVersion.Substring(0, 3)));

                Debug.Assert(currentVersion != null);

                s_tracer.TraceInformation(Strings.VsVersions.CurrentVersion(currentVersion));
            }
            else
            {
                // Pick one arbitrarily
                currentVersion = installedVersions.FirstOrDefault();
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

            var ranges = vsVersions.Distinct().Select(x =>
            {
                if (!VersionRange.TryParse(x, out var range))
                    Debug.Fail($"Could not parse {x} as a version range.");

                return VersionRange.Parse(x);
            }).ToList();

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
