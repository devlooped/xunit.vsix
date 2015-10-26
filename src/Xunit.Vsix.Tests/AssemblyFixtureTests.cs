using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit.AssemblyFixtures
{
	public class AssemblyFixture : IDisposable
	{
		public AssemblyFixture ()
		{
			ConstructedTimes++;
		}

		public int ConstructedTimes { get; private set; }

		public void Dispose ()
		{
		}
	}

	public class TestWithAssemblyFixture : IAssemblyFixture<AssemblyFixture>
	{
		ITestOutputHelper output;
		AssemblyFixture state;

		public TestWithAssemblyFixture (AssemblyFixture state, ITestOutputHelper output)
		{
			this.state = state;
			this.output = output;
		}

		[VsixFact]
		public void when_using_assembly_fixture_then_can_access_its_state ()
		{
			Assert.NotNull (state);
			Assert.Equal (1, state.ConstructedTimes);
		}
	}

	public class TestWithAssemblyFixture2 : IAssemblyFixture<AssemblyFixture>
	{
		ITestOutputHelper output;
		AssemblyFixture state;

		public TestWithAssemblyFixture2 (AssemblyFixture state, ITestOutputHelper output)
		{
			this.state = state;
			this.output = output;
		}

		[VsixFact]
		public void when_using_assembly_fixture_then_can_access_its_state ()
		{
			Assert.NotNull (state);
			Assert.Equal (1, state.ConstructedTimes);
		}
	}
}
