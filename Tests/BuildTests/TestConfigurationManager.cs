using System.Collections.Generic;
using Common;
using Common.Exceptions;
using NSubstitute;
using NUnit.Framework;

namespace Tests.BuildTests
{
    [TestFixture]
    public class TestConfigurationManager
    {
        private ConfigurationManager manager;

        [SetUp]
        public void SetUp()
        {
            var fakeParser = Substitute.For<IConfigurationParser>();
            fakeParser.GetConfigurationsHierarchy().Returns(
                new Dictionary<string, IList<string>>
                {
                    {"client", new[] {"sdk", "full-build"}},
                    {"subclient", new[] {"client"}},
                    {"sdk", new[] {"full-build"}},
                    {"notests", new[] {"full-build"}},
                    {"full-build", new List<string>()}
                });

            fakeParser.GetDefaultConfigurationName().Returns("full-build");

            var deps = new List<Dep>
            {
                new("module", null, "sdk"),
                new("module", null, "subclient")
            };

            manager = new ConfigurationManager(deps, fakeParser);
        }

        [Test]
        public void TestProcessedParentIsProcessed()
        {
            Assert.True(manager.ProcessedParent(new Dep("module", null, "subclient")));
        }

        [Test]
        public void TestProcessedParentIsProcessedNull()
        {
            Assert.False(manager.ProcessedParent(new Dep("module", null)));
        }

        [Test]
        public void TestProcessedParentNotProcessed()
        {
            Assert.False(manager.ProcessedParent(new Dep("module", null, "notests")));
        }

        [Test]
        public void TestProcessedChildrens()
        {
            Assert.True(manager.ProcessedChildrenConfigurations(new Dep("module", null, "full-build")).Contains("sdk"));
            Assert.False(manager.ProcessedChildrenConfigurations(new Dep("module", null, "full-build")).Contains("client"));
            Assert.True(manager.ProcessedChildrenConfigurations(new Dep("module", null, "full-build")).Contains("subclient"));
        }
    }

    [TestFixture]
    public class TestTreeishManager
    {
        private readonly TreeishManager treeishManager;

        public TestTreeishManager()
        {
            treeishManager = new TreeishManager();
        }

        [Test]
        public void TestTreeishProceeded()
        {
            Assert.IsTrue(
                treeishManager.TreeishAlreadyProcessed(
                    new Dep("", "treeish"), new List<Dep>
                    {
                        new("", "A"),
                        new("", "treeish"),
                        new("", "C")
                    }));
        }

        [Test]
        public void TestThrowsOnTreeishConflict()
        {
            Assert.Throws<TreeishConflictException>(
                () => treeishManager.ThrowOnTreeishConflict(
                    new DepWithParent(new Dep("", "treeish1"), "A"), new List<DepWithParent>
                    {
                        new(new Dep("", "treeish1"), "B"),
                        new(new Dep("", "treeish2"), "C")
                    }));
        }

        [Test]
        public void TestThreeishNotProcceded()
        {
            Assert.IsFalse(
                treeishManager.TreeishAlreadyProcessed(
                    new Dep("", "treeish"), new List<Dep>
                    {
                        new("", "A"),
                        new("", "B")
                    }));
        }

        [Test]
        public void TestNotThrowsWithoutConflict()
        {
            Assert.DoesNotThrow(
                () => treeishManager.ThrowOnTreeishConflict(
                    new DepWithParent(new Dep("", "treeish"), "A"), new List<DepWithParent>
                    {
                        new(new Dep(""), "B"),
                        new(new Dep("", "treeish"), "C")
                    }));
        }
    }
}
