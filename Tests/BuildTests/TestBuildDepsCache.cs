using System.Collections.Generic;
using System.IO;
using Commands;
using Common;
using Common.YamlParsers;
using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.BuildTests
{
    [TestFixture]
    public class TestBuildDepsCache
    {
        private readonly BuildPreparer buildPreparer;
        private readonly IGitRepositoryFactory gitRepositoryFactory;

        public TestBuildDepsCache()
        {
            var consoleWriter = ConsoleWriter.Shared;
            var buildHelper = BuildHelper.Shared;

            gitRepositoryFactory = new GitRepositoryFactory(consoleWriter, buildHelper);
            buildPreparer = BuildPreparer.Shared;
        }

        [Test]
        public void TestOneModule()
        {
            using var env = new TestEnvironment();
            env.CreateRepo(
                "A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
            Helper.SetWorkspace(env.RemoteWorkspace);

            CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            Build(new Dep("A/full-build"));
            Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);
        }

        [Test]
        [Retry(3)]
        public void TestGitClean()
        {
            // arrange
            using var env = new TestEnvironment();
            env.CreateRepo(
                "A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
            Helper.SetWorkspace(env.RemoteWorkspace);

            GetUpdatedModules(new Dep("A")).Should().BeEquivalentTo(new Dep("A/full-build"));

            Build(new Dep("A/full-build"));

            GetUpdatedModules(new Dep("A")).Should().BeEmpty();

            // act

            gitRepositoryFactory.Create("A", Helper.CurrentWorkspace).Clean();

            // assert

            GetUpdatedModules(new Dep("A")).Should().BeEquivalentTo(new Dep("A/full-build"));

            Build(new Dep("A/full-build"));

            GetUpdatedModules(new Dep("A")).Should().BeEmpty();
        }

        [Test]
        public void TestModuleWithDep()
        {
            using var env = new TestEnvironment();

            env.CreateRepo(
                "A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new("B")})}
                });
            env.CreateRepo(
                "B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
            Helper.SetWorkspace(env.RemoteWorkspace);

            CollectionAssert.AreEquivalent(new[] {new Dep("A/full-build"), new Dep("B/full-build")}, GetUpdatedModules(new Dep("A")));
            BuildDeps(new Dep("A/full-build"));
            CollectionAssert.AreEquivalent(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            Build(new Dep("A/full-build"));
            Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);

            //change dep
            env.CommitIntoRemote("B", "1.txt", "changes");
            CollectionAssert.AreEquivalent(new[] {new Dep("A/full-build"), new Dep("B/full-build")}, GetUpdatedModules(new Dep("A")));
            BuildDeps(new Dep("A/full-build"));
            CollectionAssert.AreEquivalent(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            Build(new Dep("A/full-build"));
            Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);

            //change root
            env.CommitIntoRemote("A", "1.txt", "changes");
            CollectionAssert.AreEquivalent(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            CollectionAssert.AreEquivalent(new Dep[] {}, GetUpdatedModules(new Dep("B")));
            Build(new Dep("A/full-build"));
            Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);
        }

        [Test]
        public void TestNeedBuildHugeConfig()
        {
            using var env = new TestEnvironment();
            env.CreateRepo(
                "A", new Dictionary<string, DepsData>
                {
                    {"full-build > client", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep>())}
                });
            Helper.SetWorkspace(env.RemoteWorkspace);

            CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            Build(new Dep("A/client"));
            CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
        }

        [Test]
        public void TestNoNeedBuildSmallerConfig()
        {
            using var env = new TestEnvironment();
            env.CreateRepo(
                "A", new Dictionary<string, DepsData>
                {
                    {"full-build > client", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep>())}
                });
            Helper.SetWorkspace(env.RemoteWorkspace);

            CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            Build(new Dep("A/full-build"));
            CollectionAssert.AreEqual(new Dep[] {}, GetUpdatedModules(new Dep("A")));
            CollectionAssert.AreEqual(new Dep[] {}, GetUpdatedModules(new Dep("A/client")));
            CollectionAssert.AreEqual(new Dep[] {}, GetUpdatedModules(new Dep("A/full-build")));
        }

        private List<Dep> GetUpdatedModules(Dep moduleToBuild)
        {
            return buildPreparer.GetModulesOrder(moduleToBuild.Name, moduleToBuild.Configuration).UpdatedModules;
        }

        private void Build(Dep module)
        {
            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
            {
                var command = new BuildCommand(ConsoleWriter.Shared, FeatureFlags.Default, buildPreparer);
                command.Run(new[] {"build", "-c", module.Configuration});
            }
        }

        private void BuildDeps(Dep module)
        {
            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
            {
                var command = new BuildDepsCommand(ConsoleWriter.Shared, FeatureFlags.Default, buildPreparer);
                command.Run(new[] {"build-deps", "-c", module.Configuration});
            }
        }
    }
}
