using System;
using System.IO;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Xunit.Abstractions;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using System.Windows;

namespace Xamarin.VisualStudio
{
	public class Misc
	{
		ITestOutputHelper output;

		public Misc (ITestOutputHelper output)
		{
			this.output = output;
		}

		[VsixFact]
		public void when_reopenening_solution_then_vssolution_is_same ()
		{
			var dte = GlobalServices.GetService<DTE>();
			var solutionEmpty = GlobalServices.GetService<SVsSolution, IVsSolution>();

			dte.Solution.Open (new FileInfo (Constants.SingleProjectSolution).FullName);

			var solution1 = GlobalServices.GetService<SVsSolution, IVsSolution>();

			dte.Solution.Close ();
			dte.Solution.Open(new FileInfo (Constants.BlankSolution).FullName);

			var solution2 = GlobalServices.GetService<SVsSolution, IVsSolution>();

			Assert.Same (solutionEmpty, solution1);
			Assert.Same (solution1, solution2);
			Assert.Same (solutionEmpty as IVsHierarchy, solution1 as IVsHierarchy);
			Assert.Same (solution1 as IVsHierarchy, solution2 as IVsHierarchy);
		}

		[VsixFact]
		public void when_reopenening_solution_then_hierarchy_item_is_same ()
		{
			var dte = GlobalServices.GetService<DTE>();
			var solutionEmpty = GlobalServices.GetService<SVsSolution, IVsSolution>();
			var manager = GlobalServices.GetService<SComponentModel, IComponentModel>().GetService<IVsHierarchyItemManager>();

			var solutionEmptyItem = manager.GetHierarchyItem (solutionEmpty as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);
			Assert.NotNull (solutionEmptyItem);

			dte.Solution.Open (new FileInfo (Constants.SingleProjectSolution).FullName);

			var solution1 = GlobalServices.GetService<SVsSolution, IVsSolution>();
			var solution1Item = manager.GetHierarchyItem (solution1 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

			dte.Solution.Close ();

			dte.Solution.Open (new FileInfo (Constants.BlankSolution).FullName);

			var solution2 = GlobalServices.GetService<SVsSolution, IVsSolution>();
			var solution2Item = manager.GetHierarchyItem (solution2 as IVsHierarchy, (uint)VSConstants.VSITEMID.Root);

			Assert.NotNull (solution1Item);
			Assert.NotNull (solutionEmptyItem);
			Assert.NotNull (solution2Item);

			Assert.Equal (solutionEmptyItem.HierarchyIdentity, solution1Item.HierarchyIdentity);
			Assert.Equal (solution1Item.HierarchyIdentity, solution2Item.HierarchyIdentity);
		}

		[Fact]
		public void when_comparing_versions_then_can_compare_greater_lower ()
		{
			Assert.Equal (-1, "11.0".CompareTo ("12.0"));
			Assert.Equal (1, "14.0".CompareTo ("12.0"));

			//if (!string.IsNullOrEmpty (minimumVersion))
			//	vsVersions.RemoveAll (vs => vs.CompareTo (minimumVersion) == -1);
			//if (!string.IsNullOrEmpty (maximumVersion))
			//	vsVersions.RemoveAll (vs => vs.CompareTo (maximumVersion) == 1);

		}
	}
}