using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Xunit;
using Xunit.Abstractions;

namespace Xunit
{
	public class SolutionTests
	{
		IVsHierarchyItem item;

		public SolutionTests ()
		{
			var components = GlobalServices.GetService<SComponentModel, IComponentModel>();
			var manager = components.GetService<IVsHierarchyItemManager>();
			item = manager.GetHierarchyItem (GlobalServices.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
		}

		[VsixFact]
		public void when_retrieving_solution_then_succeeds ()
		{
			Assert.NotNull (item);
			Assert.True (item.HierarchyIdentity.IsRoot);
		}
	}

	public class EndToEnd
	{
		ITestOutputHelper output;

		public EndToEnd (ITestOutputHelper output)
		{
			this.output = output;
			//Tracer.Configuration.AddListener (Constants.TracerName, new TestOutpuTraceListwhen_succeeding_then_reportsener (output));
			//Tracer.Configuration.SetTracingLevel (Constants.TracerName, SourceLevels.All);
		}

		[VsixFact]
		public void when_executing_then_runs_on_main_thread ()
		{
			Assert.Equal (Application.Current.Dispatcher.Thread, Thread.CurrentThread);
		}

		[InlineData ("foo")]
		[InlineData ("base")]
		[Theory]
		public void when_theory_data_then_shows_multiple_tests (string message)
		{
			Assert.NotNull (message);
		}

		[Fact]
		public void when_running_regular_fact_then_succeeds ()
		{
			Assert.True (true);
		}

		[InlineData ("foo", "foo")]
		[InlineData ("bar", "bar")]
		[VsixTheory (VisualStudioVersion.All)]
		public void when_theory_for_vsix_then_executes_on_vs (string expected, string actual)
		{
			Assert.Equal (expected, actual);
		}

		[VsixFact (TimeoutSeconds = 2)]
		public void when_execution_times_out_then_restarts_vs_for_other_tests ()
		{
			Thread.Sleep (TimeSpan.FromSeconds (3));
		}

		[VsixFact(VisualStudioVersion.VS2015, RecycleOnFailure = true)]
		public void when_failed_and_recycle_then_runs_twice ()
		{
			Assert.Equal ("foo", "foobar");
		}

		[VsixFact]
		public void when_failing_then_reports ()
		{
			Assert.Equal ("foo", "foobar");
		}

		[VsixFact]
		public void when_succeeding_then_reports ()
		{
			Assert.Equal ("foo", "foo");
			output.WriteLine ("Exito!");
		}

		[VsixFact]
		public void when_loading_solution_then_succeeds ()
		{
			var dte = GlobalServices.GetService <EnvDTE.DTE>();

			Assert.NotNull (dte);

			var sln = Path.GetFullPath(@"..\..\..\Xunit.Vsix.sln");

			dte.Solution.Open (sln);

			Assert.True (dte.Solution.IsOpen);
		}

		[VsixFact (MinimumVisualStudioVersion = VisualStudioVersion.VS2015, MaximumVisualStudioVersion = VisualStudioVersion.VS2015)]
		public void when_annotating_with_minimum_and_maximum_then_excludes_other_versions ()
		{
			var dte = GlobalServices.GetService <EnvDTE.DTE>();

			Assert.NotNull (dte);

			Assert.Equal ("14.0", dte.Version);
		}
	}
}
