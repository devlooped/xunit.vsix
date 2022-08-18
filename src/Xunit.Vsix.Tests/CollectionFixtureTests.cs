using System;
using Xunit.Abstractions;

namespace Xunit.Vsix.Tests.CollectionFixtures
{
    [CollectionDefinition("MyCollection")]
    public class MyCollection : ICollectionFixture<CollectionFixture> { }

    public class CollectionFixture : IDisposable
    {
        public CollectionFixture()
        {
            this.ConstructedTimes++;
        }

        public int ConstructedTimes { get; private set; }

        public void Dispose()
        {
        }
    }

    [Collection("MyCollection")]
    public class TestWithCollectionFixture
    {
        ITestOutputHelper _output;
        CollectionFixture _state;

        public TestWithCollectionFixture(CollectionFixture state, ITestOutputHelper output)
        {
            _state = state;
            _output = output;
        }

        [VsixFact]
        public void when_using_collection_fixture_then_can_access_its_state()
        {
            Assert.NotNull(_state);
            Assert.Equal(1, _state.ConstructedTimes);
        }
    }

    [Collection("MyCollection")]
    public class TestWithCollectionFixture2
    {
        ITestOutputHelper _output;
        CollectionFixture _state;

        public TestWithCollectionFixture2(CollectionFixture state, ITestOutputHelper output)
        {
            _state = state;
            _output = output;
        }

        [VsixFact]
        public void when_using_collection_fixture_then_can_access_its_state()
        {
            Assert.NotNull(_state);
            Assert.Equal(1, _state.ConstructedTimes);
        }
    }
}
