﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Stashbox.Tests.IssueTests
{
    [TestClass]
    public class ComposeByWithInstanceOrInjection
    {
        [TestMethod]
        public void ComposeByWithInstance()
        {
            var mock = new Mock<ICompositionRoot>();
            var container = new StashboxContainer().ComposeBy(mock.Object);
            mock.Verify(m => m.Compose(container), Times.Once);
        }

        [TestMethod]
        public void ComposeByWithInjection()
        {
            new StashboxContainer()
                .Register<TestDep>()
                .ComposeBy<TestRoot1>();
        }

        [TestMethod]
        public void ComposeByWithMemberInjection()
        {
            new StashboxContainer(c => c.WithMemberInjectionWithoutAnnotation())
                .Register<TestDep>()
                .ComposeBy<TestRoot>();
        }

        [TestMethod]
        public void ComposeByWithInjectionWithDependencyOverride()
        {
            new StashboxContainer()
                .ComposeBy<TestRoot2>(5);
        }

        class TestDep { }

        class TestRoot : ICompositionRoot
        {
            public TestDep Test { get; set; }

            public void Compose(IStashboxContainer container)
            {
                if (this.Test == null)
                    Assert.Fail("Dependency not resolved");
            }
        }

        class TestRoot1 : ICompositionRoot
        {
            public TestDep Test { get; set; }

            public TestRoot1(TestDep test)
            {
                this.Test = test;
            }

            public void Compose(IStashboxContainer container)
            {
                if (this.Test == null)
                    Assert.Fail("Dependency not resolved");
            }
        }

        class TestRoot2 : ICompositionRoot
        {
            public int Test { get; set; }

            public TestRoot2(int test)
            {
                this.Test = test;
            }

            public void Compose(IStashboxContainer container)
            {
                if (this.Test != 5)
                    Assert.Fail("Dependency not resolved");
            }
        }
    }
}