using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
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
            Solution = new AsyncLazy<IVsHierarchyItem>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var components = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>();
                var manager = components.GetService<IVsHierarchyItemManager>();
                return manager.GetHierarchyItem(ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
            });
        }

        public AsyncLazy<IVsHierarchyItem> Solution { get; private set; }
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
        public async Task when_running_then_solution_existsAsync()
        {
            Assert.NotNull(await _solution.Solution.GetValueAsync());
        }
    }
}
