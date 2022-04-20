using System.Diagnostics;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace Xunit
{
    // All of these tests need to run on the UI thread because we use the IVsHierarchyItemManager 
    // which is not thread-safe and should always be accessed from the UI thread.
    [Vsix(RunOnUIThread = true)]
    public class SolutionHierarchyTests
    {
        IVsHierarchyItem _item;

        public SolutionHierarchyTests()
        {
            var components = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>();
            var manager = components.GetService<IVsHierarchyItemManager>();
            _item = manager.GetHierarchyItem(ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
            Trace.WriteLine("SolutionTests Created");
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "Exp")]
        public void when_using_external_assembly_then_resolves()
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("xunit.vsix"));

            Assert.NotNull(client);
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "")]
        public void when_retrieving_solution_then_succeeds()
        {
            Trace.WriteLine("Hello world!");
            Assert.NotNull(_item);
            Assert.True(_item.HierarchyIdentity.IsRoot);
        }
    }

}
