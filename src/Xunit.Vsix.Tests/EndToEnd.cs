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

[assembly: VsixRunner(TraceLevel = SourceLevels.All)]

namespace Xunit
{
    public class SolutionTests
    {
        IVsHierarchyItem _item;

        public SolutionTests()
        {
            var components = ServiceProvider.GlobalProvider.GetService<SComponentModel, IComponentModel>();
            var manager = components.GetService<IVsHierarchyItemManager>();
            _item = manager.GetHierarchyItem(ServiceProvider.GlobalProvider.GetService<SVsSolution, IVsHierarchy>(), (uint)VSConstants.VSITEMID.Root);
            Trace.WriteLine("SolutionTests Created");
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "Exp", RunOnUIThread = true)]
        public void when_using_external_assembly_then_resolves()
        {
            var client = new Octokit.GitHubClient(new Octokit.ProductHeaderValue("xunit.vsix"));

            Assert.NotNull(client);
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "Exp", RunOnUIThread = true)]
        public async System.Threading.Tasks.Task when_requesting_ui_thread_then_runs_on_UI_thread()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            var uiThreadId = await Application.Current.Dispatcher.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);

            Assert.Equal(currentThreadId, uiThreadId);
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "Exp")]
        public async System.Threading.Tasks.Task when_executing_then_does_not_run_on_UI_thread()
        {
            var currentThreadId = Thread.CurrentThread.ManagedThreadId;
            var uiThreadId = await Application.Current.Dispatcher.InvokeAsync(() => Thread.CurrentThread.ManagedThreadId);

            Assert.NotEqual(currentThreadId, uiThreadId);
        }

        [VsixFact(VisualStudioVersion.Current, RootSuffix = "")]
        public void when_retrieving_solution_then_succeeds()
        {
            Trace.WriteLine("Hello world!");
            Assert.NotNull(_item);
            Assert.True(_item.HierarchyIdentity.IsRoot);
        }
    }

    public class EndToEnd
    {
        ITestOutputHelper _output;

        public EndToEnd(ITestOutputHelper output)
        {
            _output = output;
        }

        [VsixFact]
        public void when_executing_then_runs_on_main_thread()
        {
            Assert.Equal(Application.Current.Dispatcher.Thread, Thread.CurrentThread);
        }

        [InlineData("foo")]
        [InlineData("base")]
        [Theory]
        public void when_theory_data_then_shows_multiple_tests(string message)
        {
            Assert.NotNull(message);
        }

        [Fact]
        public void when_running_regular_fact_then_succeeds()
        {
            Assert.True(true);
        }

        [InlineData("foo", "foo")]
        [InlineData("bar", "bar")]
        [VsixTheory(VisualStudioVersion.All)]
        public void when_theory_for_vsix_then_executes_on_vs(string expected, string actual)
        {
            Assert.Equal(expected, actual);
        }

        [VsixFact(TimeoutSeconds = 2)]
        public void when_execution_times_out_then_restarts_vs_for_other_tests()
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }

        [VsixFact(RecycleOnFailure = true, Skip = "Can only be verified manually")]
        public void when_failed_and_recycle_then_runs_twice()
        {
            Assert.Equal("foo", "foobar");
        }

        [VsixFact]
        public void when_failing_then_reports()
        {
            Assert.Equal("foo", "foobar");
        }

        [VsixFact]
        public void when_succeeding_then_reports()
        {
            Assert.Equal("foo", "foo");
            _output.WriteLine("Exito!");
        }

        [VsixFact]
        public void when_loading_solution_then_succeeds()
        {
            var dte = ServiceProvider.GlobalProvider.GetService<EnvDTE.DTE>();

            Assert.NotNull(dte);

            var sln = Path.GetFullPath(@"Content\\Blank.sln");

            dte.Solution.Open(sln);

            Assert.True(dte.Solution.IsOpen);
        }

        [VsixFact(MinimumVisualStudioVersion = VisualStudioVersion.VS2017, MaximumVisualStudioVersion = VisualStudioVersion.VS2017)]
        public void when_annotating_with_minimum_and_maximum_then_excludes_other_versions()
        {
            var dte = ServiceProvider.GlobalProvider.GetService<EnvDTE.DTE>();

            Assert.NotNull(dte);

            Assert.Equal("14.0", dte.Version);
        }
    }
}
