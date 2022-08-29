using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.VisualStudio.Shell;
using Xunit;
using Xunit.Abstractions;

[assembly: VsixRunner(TraceLevel = SourceLevels.All)]

namespace Xunit.Vsix.Tests
{
    public class SolutionTests
    {
        ITestOutputHelper _output;

        public SolutionTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [VsixFact(RunOnUIThread = true)]
        public void when_executing_then_runs_on_main_thread()
        {
            Assert.Equal(Application.Current.Dispatcher.Thread.ManagedThreadId, Thread.CurrentThread.ManagedThreadId);
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

        [VsixFact(TimeoutSeconds = 2, Skip = "Can only be verified manually")]
        public void when_execution_times_out_then_restarts_vs_for_other_tests()
        {
            Thread.Sleep(TimeSpan.FromSeconds(3));
        }

        [VsixFact(RecycleOnFailure = true, Skip = "Can only be verified manually")]
        public void when_failed_and_recycle_then_runs_twice()
        {
            Assert.Equal("foo", "foobar");
        }

        [VsixFact(Skip = "Can only be verified manually")]
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
        public async Task when_loading_solution_then_succeedsAsync()
        {
            var dte = await ServiceProvider.GetGlobalServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();

            Assert.NotNull(dte);

            dte.Solution.Open(Path.Combine(
                ThisAssembly.Project.MSBuildProjectDirectory,
                "Content", "Blank.sln"));

            Assert.True(dte.Solution.IsOpen);
        }

        [VsixFact(VisualStudioVersion.VS2017)]
        public async Task when_specific_version_then_runs_on_itAsync()
        {
            var dte = await ServiceProvider.GetGlobalServiceAsync<EnvDTE.DTE, EnvDTE.DTE>();

            Assert.NotNull(dte);

            Assert.Equal("15.0", dte.Version);
        }
    }
}
