using System;
using System.IO;
using System.Threading;
using Xunit;
using Xunit.Abstractions;

namespace Xunit
{
	public class ClassFixture : IDisposable
	{
		public ClassFixture ()
		{

		}

		public void Dispose ()
		{
		}
	}

	public class TestsWithClassFixture : IClassFixture<ClassFixture>
	{
		ITestOutputHelper output;
		ClassFixture state;

		public TestsWithClassFixture (ClassFixture state, ITestOutputHelper output)
		{
			this.state = state;
			this.output = output;
		}

		[VsixFact]
		public void when_using_class_fixture_then_can_access_its_state ()
		{
			Assert.NotNull (state);
			output.WriteLine ("Success!!!!");
		}
	}

	[Vsix (RootSuffix = "Exp")]
	public class EndToEnd
	{
		ITestOutputHelper output;

		public EndToEnd (ITestOutputHelper output)
		{
			this.output = output;
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

		[VsixFact (VisualStudioVersion.Latest)]
		public void when_annotating_with_vsixattribute_then_can_set_all_vsversions ()
		{
			var dte = GlobalServices.GetService <EnvDTE.DTE>();

			Assert.NotNull (dte);

			dte.Solution.Open (Path.Combine (Directory.GetCurrentDirectory (), "Content\\ClassLibrary1\\ClassLibrary1.sln"));

			Assert.True (dte.Solution.IsOpen);
		}
	}
}
