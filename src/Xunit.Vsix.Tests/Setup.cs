using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Setup.Configuration;
using Xunit;

namespace Xunit
{
    public class Setup
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);

        //[Fact]
        public void when_enumerating_vs_then_retrieves_all_instances()
        {
            var query = new SetupConfiguration();
            var query2 = (ISetupConfiguration2)query;
            var e = query2.EnumAllInstances();

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

            var vssdk = from instance in instances
                        let state = PrintInstance(instance, helper).GetState()
                        where state == InstanceState.Complete &&
                        (state & InstanceState.Local) == InstanceState.Local &&
                        instance.GetPackages().Any(package => package.GetId() == "Microsoft.VisualStudio.Workload.VisualStudioExtension")
                        select PrintInstance(instance, helper);

            vssdk.ToList();
        }

        static ISetupInstance2 PrintInstance(ISetupInstance2 instance, ISetupHelper helper)
        {
            var state = instance.GetState();
            Console.WriteLine($"InstanceId: {instance.GetInstanceId()} ({(state == InstanceState.Complete ? "Complete" : "Incomplete")})");

            var installationVersion = instance.GetInstallationVersion();
            var version = helper.ParseVersion(installationVersion);
            var catalog = instance as ISetupInstanceCatalog;

            Console.WriteLine($"InstallationVersion: {installationVersion} ({version})");

            if (catalog != null)
            {
                var value = (string)catalog?.GetCatalogInfo().GetValue("productSemanticVersion");
                if (value != null)
                {
                    var semver = NuGet.Versioning.SemanticVersion.Parse(value);
                    Console.WriteLine($"SemanticVersion: {semver.Major}.{semver.Minor}.{semver.Patch} (Normalized: {semver.ToNormalizedString()}, Full: {semver.ToFullString()})");
                }
            }

            if ((state & InstanceState.Local) == InstanceState.Local)
            {
                Console.WriteLine($"InstallationPath: {instance.GetInstallationPath()}");
            }

            if (catalog != null)
            {
                Console.WriteLine($"IsPrerelease: {catalog.IsPrerelease()}");
            }

            if ((state & InstanceState.Registered) == InstanceState.Registered)
            {
                Console.WriteLine($"Product: {instance.GetProduct().GetId()}");
                Console.WriteLine("Workloads:");

                PrintWorkloads(instance.GetPackages());
            }

            var properties = instance.GetProperties();
            if (properties != null)
            {
                Console.WriteLine("Custom properties:");
                PrintProperties(properties);
            }

            properties = catalog?.GetCatalogInfo();
            if (properties != null)
            {
                Console.WriteLine("Catalog properties:");
                PrintProperties(properties);
            }

            Console.WriteLine();
            return instance;
        }

        static void PrintProperties(ISetupPropertyStore store)
        {
            var properties = from name in store.GetNames()
                             orderby name
                             select new { Name = name, Value = store.GetValue(name) };

            foreach (var prop in properties)
            {
                Console.WriteLine($"    {prop.Name}: {prop.Value}");
            }
        }

        static void PrintWorkloads(ISetupPackageReference[] packages)
        {
            var workloads = from package in packages
                            where string.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                            orderby package.GetId()
                            select package;

            foreach (var workload in workloads)
            {
                Console.WriteLine($"    {workload.GetId()}");
            }
        }
    }
}
