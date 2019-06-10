using System.Collections.Generic;
using System.IO;
using Commands;
using Common;
using Common.YamlParsers;
using log4net;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.BuildTests
{
    [TestFixture]
    public class TestBuildDepsCache
    {
        private static readonly ILog Log = LogManager.GetLogger("TestBuildDepsCache");

        private List<Dep> GetUpdatedModules(Dep moduleToBuild)
        {
            return new BuildPreparer(Log).GetModulesOrder(moduleToBuild.Name, moduleToBuild.Configuration).UpdatedModules;
        }

        private void Build(Dep module)
        {
            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
                new Build().Run(new[] {"build", "-c", module.Configuration});
        }

        private void BuildDeps(Dep module)
        {
            using (new DirectoryJumper(Path.Combine(Helper.CurrentWorkspace, module.Name)))
                new BuildDeps().Run(new[] {"build-deps", "-c", module.Configuration});
        }

        [Test]
        public void TestOneModule()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);

                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
                Build(new Dep("A/full-build"));
                Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);
            }
        }

        [Test]
        [Retry(3)]
        public void TestGitClean()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);

                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
                Build(new Dep("A/full-build"));
                Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);

                new GitRepository("A", Helper.CurrentWorkspace, Log).Clean();
                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
                Build(new Dep("A/full-build"));
                Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);
            }
        }

        [Test]
        public void TestModuleWithDep()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
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
                CollectionAssert.AreEquivalent(new Dep[] { }, GetUpdatedModules(new Dep("B")));
                Build(new Dep("A/full-build"));
                Assert.That(GetUpdatedModules(new Dep("A")), Is.Empty);
            }
        }

        [Test]
        public void TestNeedBuildHugeConfig()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build > client", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);

                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
                Build(new Dep("A/client"));
                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
            }
        }

        [Test]
        public void TestNoNeedBuildSmallerConfig()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build > client", new DepsData(null, new List<Dep>())},
                    {"client", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);

                CollectionAssert.AreEqual(new[] {new Dep("A/full-build")}, GetUpdatedModules(new Dep("A")));
                Build(new Dep("A/full-build"));
                CollectionAssert.AreEqual(new Dep[] { }, GetUpdatedModules(new Dep("A")));
                CollectionAssert.AreEqual(new Dep[] { }, GetUpdatedModules(new Dep("A/client")));
                CollectionAssert.AreEqual(new Dep[] { }, GetUpdatedModules(new Dep("A/full-build")));
            }
        }
    }
}