using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Xunit.CollectionFixtures
{
	[CollectionDefinition ("MyCollection")]
	public class MyCollection : ICollectionFixture<CollectionFixture> { }

	public class CollectionFixture : IDisposable
	{
		public CollectionFixture ()
		{
			this.ConstructedTimes++;
		}

		public int ConstructedTimes { get; private set; }

		public void Dispose ()
		{
		}
	}

	[Collection ("MyCollection")]
	public class TestWithCollectionFixture
	{
		ITestOutputHelper output;
		CollectionFixture state;

		public TestWithCollectionFixture (CollectionFixture state, ITestOutputHelper output)
		{
			this.state = state;
			this.output = output;
		}

		[VsixFact]
		public void when_using_collection_fixture_then_can_access_its_state ()
		{
			Assert.NotNull (state);
			Assert.Equal (1, state.ConstructedTimes);
		}
	}

	[Collection ("MyCollection")]
	public class TestWithCollectionFixture2
	{
		ITestOutputHelper output;
		CollectionFixture state;

		public TestWithCollectionFixture2 (CollectionFixture state, ITestOutputHelper output)
		{
			this.state = state;
			this.output = output;
		}

		[VsixFact]
		public void when_using_collection_fixture_then_can_access_its_state ()
		{
			Assert.NotNull (state);
			Assert.Equal (1, state.ConstructedTimes);
		}
	}
}
