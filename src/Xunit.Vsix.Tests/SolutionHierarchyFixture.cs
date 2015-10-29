using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Xunit
{
	[CollectionDefinition ("SolutionState")]
	public class SolutionStateCollection : ICollectionFixture<SolutionState> { }

	public class SolutionState
	{
		public SolutionState ()
		{
			var components = GlobalServices.GetService<SComponentModel, IComponentModel>();
			var manager = components.GetService<IVsHierarchyItemManager>();
			Solution = manager.GetHierarchyItem (GlobalServices.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
		}

		public IVsHierarchyItem Solution { get; private set; }
	}

	[Collection("SolutionState")]
	public class SolutionHierarchyFixture
	{
		SolutionState solution;

		public SolutionHierarchyFixture (SolutionState solution)
		{
			this.solution = solution;
		}

		[VsixFact]
		public void when_running_then_solution_exists ()
		{
			Assert.NotNull (solution);
		}
	}
}
