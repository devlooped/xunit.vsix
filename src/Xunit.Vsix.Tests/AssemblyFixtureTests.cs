using System;
using Xunit.Abstractions;

namespace Xunit.Vsix.Tests.AssemblyFixtures
{
    public class AssemblyFixture : IDisposable
    {
        public AssemblyFixture()
        {
            ConstructedTimes++;
        }

        public int ConstructedTimes { get; private set; }

        public void Dispose()
        {
        }
    }

    public class TestWithAssemblyFixture : IAssemblyFixture<AssemblyFixture>
    {
        ITestOutputHelper _output;
        AssemblyFixture _state;

        public TestWithAssemblyFixture(AssemblyFixture state, ITestOutputHelper output)
        {
            _state = state;
            _output = output;
        }

        [VsixFact]
        public void when_using_assembly_fixture_then_can_access_its_state()
        {
            Assert.NotNull(_state);
            Assert.Equal(1, _state.ConstructedTimes);
        }
    }

    public class TestWithAssemblyFixture2 : IAssemblyFixture<AssemblyFixture>
    {
        ITestOutputHelper _output;
        AssemblyFixture _state;

        public TestWithAssemblyFixture2(AssemblyFixture state, ITestOutputHelper output)
        {
            _state = state;
            _output = output;
        }

        [VsixFact]
        public void when_using_assembly_fixture_then_can_access_its_state()
        {
            Assert.NotNull(_state);
            Assert.Equal(1, _state.ConstructedTimes);
        }
    }
}
