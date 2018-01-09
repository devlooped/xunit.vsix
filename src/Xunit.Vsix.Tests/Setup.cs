using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Setup.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Xunit
{
    public class Setup
    {
        private const int REGDB_E_CLASSNOTREG = unchecked((int)0x80040154);
        ITestOutputHelper output;

        public Setup(ITestOutputHelper output) => this.output = output;

        [InlineData("Microsoft.VisualStudio.Component.Roslyn.Compiler")]
        //[InlineData("Microsoft.VisualStudio.Component.Merq")]
        [Theory(Skip = "Just testing the setup APIs")]
        public void when_enumerating_vs_then_retrieves_all_instances(string prerequisite)
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
                        let state = instance.GetState()
                        where state == InstanceState.Complete &&
                        (state & InstanceState.Local) == InstanceState.Local &&
                        instance.GetPackages().Any(package => package.GetId() == prerequisite)
                        select PrintInstance(instance, helper);

            vssdk.ToList();
        }

        ISetupInstance2 PrintInstance(ISetupInstance2 instance, ISetupHelper helper)
        {
            var state = instance.GetState();
            output.WriteLine(new string('=', 100));
            output.WriteLine($"InstanceId: {instance.GetInstanceId()} ({(state == InstanceState.Complete ? "Complete" : "Incomplete")})");

            var installationVersion = instance.GetInstallationVersion();
            var version = helper.ParseVersion(installationVersion);
            var catalog = instance as ISetupInstanceCatalog;

            output.WriteLine($"InstallationVersion: {installationVersion} ({version})");

            if (catalog != null)
            {
                var value = (string)catalog?.GetCatalogInfo().GetValue("productSemanticVersion");
                if (value != null)
                {
                    var semver = NuGet.Versioning.SemanticVersion.Parse(value);
                    output.WriteLine($"SemanticVersion: {semver.Major}.{semver.Minor}.{semver.Patch} (Normalized: {semver.ToNormalizedString()}, Full: {semver.ToFullString()})");
                }
            }

            if ((state & InstanceState.Local) == InstanceState.Local)
            {
                output.WriteLine($"InstallationPath: {instance.GetInstallationPath()}");
            }

            if (catalog != null)
            {
                output.WriteLine($"IsPrerelease: {catalog.IsPrerelease()}");
            }

            if ((state & InstanceState.Registered) == InstanceState.Registered)
            {
                output.WriteLine($"Product: {instance.GetProduct().GetId()}");

                PrintPackages(instance.GetPackages());
            }

            var properties = instance.GetProperties();
            if (properties != null)
            {
                output.WriteLine("Custom properties:");
                PrintProperties(properties);
            }

            properties = catalog?.GetCatalogInfo();
            if (properties != null)
            {
                output.WriteLine("Catalog properties:");
                PrintProperties(properties);
            }

            output.WriteLine("");
            return instance;
        }

        void PrintProperties(ISetupPropertyStore store)
        {
            var properties = from name in store.GetNames()
                             orderby name
                             select new { Name = name, Value = store.GetValue(name) };

            foreach (var prop in properties)
            {
                output.WriteLine($"    {prop.Name}: {prop.Value}");
            }
        }

        void PrintWorkloads(ISetupPackageReference[] packages)
        {
            var workloads = from package in packages
                            where string.Equals(package.GetType(), "Workload", StringComparison.OrdinalIgnoreCase)
                            orderby package.GetId()
                            select package;

            foreach (var workload in workloads)
            {
                output.WriteLine($"    {workload.GetId()}");
            }
        }

        void PrintPackages(ISetupPackageReference[] packages)
        {
            var groups = from package in packages
                         orderby package.GetId()
                         group package by package.GetType() into grouped
                         orderby grouped.Key
                         select grouped;

            foreach (var group in groups)
            {
                output.WriteLine($"{group.Key}:");
                foreach (var package in group)
                {
                    output.WriteLine($"    {package.GetId()}");
                }
            }
        }
    }
}
