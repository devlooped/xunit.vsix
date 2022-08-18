using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Xunit.Vsix.Tests
{
    [CollectionDefinition("SolutionState")]
    public class SolutionStateCollection : ICollectionFixture<SolutionState> { }

    public class SolutionState
    {
        public SolutionState()
        {
            var components = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>();
            var manager = components.GetService<IVsHierarchyItemManager>();
            Solution = manager.GetHierarchyItem(ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
        }

        public IVsHierarchyItem Solution { get; private set; }
    }

    [Collection("SolutionState")]
    public class SolutionHierarchyFixture
    {
        SolutionState _solution;

        public SolutionHierarchyFixture(SolutionState solution)
        {
            _solution = solution;
        }

        [VsixFact]
        public void when_running_then_solution_exists()
        {
            Assert.NotNull(_solution);
        }
    }
}
