using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Setup.Configuration;
using NuGet.Versioning;

namespace Xunit
{
    class VsSetup
    {
        public static string[] GetInstalled()
        {
            var vs2017 = from instance in EnumerateUsableInstances()
                         let productVersion = (string)(instance as ISetupInstanceCatalog)?.GetCatalogInfo()?.GetValue("productSemanticVersion")
                         where productVersion != null
                         let semver = SemanticVersion.Parse(productVersion)
                         select new Version(semver.Major, semver.Minor);

            return vs2017.Distinct().Select(v => v.ToString()).ToArray();
        }

        public static string GetDevEnv(Version version)
        {
            var vs = from instance in EnumerateUsableInstances()
                     let productVersion = (string)(instance as ISetupInstanceCatalog)?.GetCatalogInfo()?.GetValue("productSemanticVersion")
                     where productVersion != null
                     let semver = SemanticVersion.Parse(productVersion)
                     where semver.Major == version.Major && semver.Minor == version.Minor
                     orderby semver descending
                     select Path.Combine(instance.GetInstallationPath(), @"Common7\IDE\devenv.exe");

            return vs.FirstOrDefault();
        }

        public static string GetComponentModelCachePath(string devEnvPath, Version version, string rootSuffix)
        {
            if (version.Major < 15)
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    @"Microsoft\VisualStudio",
                    version.ToString() + rootSuffix,
                    "ComponentModelCache");

            var ini = Path.ChangeExtension(devEnvPath, "isolation.ini");
            if (!File.Exists(ini))
                return null;

            return File.ReadAllLines(ini)
                .Where(line => line.StartsWith("InstallationID=", StringComparison.Ordinal))
                .Select(line => line.Substring(15))
                .Select(instanceId => Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    $@"Microsoft\VisualStudio\{version.Major}.0_{instanceId}{rootSuffix}\ComponentModelCache"))
                .FirstOrDefault();
        }

        /// <summary>
        /// Filters the <see cref="EnumerateInstances"/> by those that are locally installed and 
        /// have the VSSDK workload installed.
        /// </summary>
        static IEnumerable<ISetupInstance2> EnumerateUsableInstances()
            => from instance in EnumerateInstances()
               let state = instance.GetState()
               where state == InstanceState.Complete &&
               (state & InstanceState.Local) == InstanceState.Local &&
               instance.GetPackages().Any(package => package.GetId() == "Microsoft.VisualStudio.Component.CoreEditor")
               select instance;

        static IEnumerable<ISetupInstance2> EnumerateInstances()
        {
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
                {
                    var instance = (ISetupInstance2)result[0];
                    var state = instance.GetState();
                    if (state == InstanceState.Complete &&
                        (state & InstanceState.Local) == InstanceState.Local)
                        yield return instance;
                }
            } while (fetched > 0);
        }
    }
}
