﻿using System;
using Xunit.Abstractions;

namespace Xunit.Vsix.Tests.ClassFixtures
{
    public class ClassFixture : IDisposable
    {
        public ClassFixture()
        {
        }

        public void Dispose()
        {
        }
    }

    public class TestsWithClassFixture : IClassFixture<ClassFixture>
    {
        ITestOutputHelper _output;
        ClassFixture _state;

        public TestsWithClassFixture(ClassFixture state, ITestOutputHelper output)
        {
            _state = state;
            _output = output;
        }

        [VsixFact]
        public void when_using_class_fixture_then_can_access_its_state()
        {
            Assert.NotNull(_state);
            _output.WriteLine("Success!!!!");
        }
    }
}
