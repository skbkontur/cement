using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.YamlParsers;
using log4net;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.BuildTests
{
    [TestFixture]
    public class TestBuildDepsOrder
    {
        private static readonly ILog Log = LogManager.GetLogger("TestBuildDepsOrder");

        [Test]
        public void TestTopSortCycle()
        {
            var graph = new Dictionary<Dep, List<Dep>>
            {
                {new Dep("A/full-build"), new List<Dep> {new Dep("B/full-build")}},
                {new Dep("B/full-build"), new List<Dep> {new Dep("C/full-build")}},
                {new Dep("C/full-build"), new List<Dep> {new Dep("D/full-build")}},
                {new Dep("D/full-build"), new List<Dep> {new Dep("A/full-build")}}
            };
            Assert.Throws<CementException>(() => BuildPreparer.GetTopologicallySortedGraph(graph, "A", "full-build"));
        }

        [Test]
        public void TestTopSortNoCycle()
        {
            var graph = new Dictionary<Dep, List<Dep>>
            {
                {new Dep("A/full-build"), new List<Dep> {new Dep("B/full-build"), new Dep("C/client")}},
                {new Dep("B/full-build"), new List<Dep> {new Dep("D/full-build"), new Dep("E/full-build")}},
                {new Dep("C/full-build"), new List<Dep> {new Dep("D/full-build")}},
                {new Dep("C/client"), new List<Dep> {new Dep("D/client")}},
                {new Dep("D/full-build"), new List<Dep> {new Dep("E/full-build")}},
                {new Dep("D/client"), new List<Dep>()},
                {new Dep("E/full-build"), new List<Dep>()}
            };
            Assert.AreEqual(new[]
            {
                new Dep("E/full-build"),
                new Dep("D/full-build"),
                new Dep("B/full-build"),
                new Dep("D/client"),
                new Dep("C/client"),
                new Dep("A/full-build")
            }, BuildPreparer.GetTopologicallySortedGraph(graph, "A", "full-build").ToArray());
        }

        [Test]
        public void TestConfigGraph()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("B"), new Dep("C", null, "client")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("D")})},
                    {"client", new DepsContent(null, new List<Dep> {new Dep("D", null, "client")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep>())},
                    {"client", new DepsContent(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var result = BuildPreparer.BuildConfigsGraph("A", null);
                Assert.AreEqual(new[] {new Dep("B", null, "full-build"), new Dep("C/client")}, result[new Dep("A", null, "full-build")].ToArray());
                Assert.AreEqual(new[] {new Dep("D", null, "full-build")}, result[new Dep("B", null, "full-build")].ToArray());
                Assert.AreEqual(new Dep[] { }, result[new Dep("D", null, "full-build")].ToArray());
                Assert.AreEqual(new[] {new Dep("D/client")}, result[new Dep("C/client")].ToArray());
                Assert.AreEqual(new string[] { }, result[new Dep("D/client")].ToArray());
            }
        }

        [Test]
        public void TestRelaxNesting()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("B"), new Dep("C", null, "client")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsContent>
                {
                    {"full-build > client *default", new DepsContent(null, new List<Dep>())},
                    {"client", new DepsContent(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                Directory.CreateDirectory(Path.Combine(env.RemoteWorkspace, ".cement"));

                List<Dep> modulesToUpdate;
                Dictionary<string, string> currentCommitHashes;
                List<Dep> topSortedDeps;
                new BuildPreparer(Log).GetModulesOrder("A", null, out topSortedDeps, out modulesToUpdate, out currentCommitHashes);
                Assert.IsFalse(topSortedDeps.Contains(new Dep("C/client")));
                Assert.IsTrue(topSortedDeps.Contains(new Dep("C", null, "full-build")));
                Assert.IsTrue(topSortedDeps.Contains(new Dep("B", null, "full-build")));
                Assert.AreEqual(2, topSortedDeps.Count);
            }
        }

        [Test]
        public void TestNestingSkip()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("X", null, "client")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("X"), new Dep("B")})}
                });
                env.CreateRepo("X", new Dictionary<string, DepsContent>
                {
                    {"full-build > client *default", new DepsContent(null, new List<Dep>())},
                    {"client", new DepsContent(null, new List<Dep>())}
                });

                Helper.SetWorkspace(env.RemoteWorkspace);
                Directory.CreateDirectory(Path.Combine(env.RemoteWorkspace, ".cement"));

                List<Dep> modulesToUpdate;
                Dictionary<string, string> currentCommitHashes;
                List<Dep> topSortedDeps;
                new BuildPreparer(Log).GetModulesOrder("A", null, out topSortedDeps, out modulesToUpdate, out currentCommitHashes);
                Assert.IsFalse(topSortedDeps.Contains(new Dep("X/client")));
                Assert.IsTrue(topSortedDeps.Contains(new Dep("B", null, "full-build")));
                Assert.IsTrue(topSortedDeps.Contains(new Dep("C", null, "full-build")));
                Assert.AreEqual(3, topSortedDeps.Count);
                CollectionAssert.AreEqual(new List<Dep>
                {
                    new Dep("X", null, "full-build"),
                    new Dep("B", null, "full-build"),
                    new Dep("C", null, "full-build")
                }, topSortedDeps);
            }
        }

        [Test]
        public void TestNestingOnlyClient()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"full-build *default", new DepsContent(null, new List<Dep> {new Dep("X", null, "client")})}
                });
                env.CreateRepo("X", new Dictionary<string, DepsContent>
                {
                    {"full-build > client *default", new DepsContent(null, new List<Dep>())},
                    {"client", new DepsContent(null, new List<Dep>())}
                });

                Helper.SetWorkspace(env.RemoteWorkspace);
                Directory.CreateDirectory(Path.Combine(env.RemoteWorkspace, ".cement"));

                List<Dep> modulesToUpdate;
                Dictionary<string, string> currentCommitHashes;
                List<Dep> topSortedDeps;
                new BuildPreparer(Log).GetModulesOrder("A", null, out topSortedDeps, out modulesToUpdate, out currentCommitHashes);
                Assert.IsFalse(topSortedDeps.Contains(new Dep("X", null, "full-build")));
                Assert.IsTrue(topSortedDeps.Contains(new Dep("X", null, "client")));
                Assert.AreEqual(1, topSortedDeps.Count);
            }
        }

        [Test]
        public void TestNestingNeedBuildBoth()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"client", new DepsContent(null, new List<Dep>())},
                    {"full-build > client *default", new DepsContent(null, new List<Dep> {new Dep("X")})}
                });
                env.CreateRepo("X", new Dictionary<string, DepsContent>
                {
                    {"full-build", new DepsContent(null, new List<Dep> {new Dep("A", null, "client")})}
                });

                Helper.SetWorkspace(env.RemoteWorkspace);
                Directory.CreateDirectory(Path.Combine(env.RemoteWorkspace, ".cement"));

                List<Dep> modulesToUpdate;
                Dictionary<string, string> currentCommitHashes;
                List<Dep> topSortedDeps;
                new BuildPreparer(Log).GetModulesOrder("A", null, out topSortedDeps, out modulesToUpdate, out currentCommitHashes);

                CollectionAssert.AreEqual(new List<Dep>
                {
                    new Dep("A", null, "client"),
                    new Dep("X", null, "full-build")
                }, topSortedDeps);
            }
        }

        [Test]
        public void TestNestingLotChildren()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsContent>
                {
                    {"full-build", new DepsContent(null, new List<Dep> {new Dep("C1"), new Dep("C2"), new Dep("C3"), new Dep("P1")})}
                });
                env.CreateRepo("C1", new Dictionary<string, DepsContent>
                    {{"full-build", new DepsContent(null, new List<Dep> {new Dep("X/child1")})}});
                env.CreateRepo("C2", new Dictionary<string, DepsContent>
                    {{"full-build", new DepsContent(null, new List<Dep> {new Dep("X/child2")})}});
                env.CreateRepo("C3", new Dictionary<string, DepsContent>
                    {{"full-build", new DepsContent(null, new List<Dep> {new Dep("X/child3")})}});
                env.CreateRepo("P1", new Dictionary<string, DepsContent>
                    {{"full-build", new DepsContent(null, new List<Dep> {new Dep("X/parent1")})}});
                env.CreateRepo("X", new Dictionary<string, DepsContent>
                {
                    {"child1", new DepsContent(null, new List<Dep>())},
                    {"child2 > child1", new DepsContent(null, new List<Dep>())},
                    {"child3 > child1", new DepsContent(null, new List<Dep>())},
                    {"parent1 > child1, child2", new DepsContent(null, new List<Dep>())},
                    {"parent2 > parent1, child3", new DepsContent(null, new List<Dep>())}
                });

                Helper.SetWorkspace(env.RemoteWorkspace);
                Directory.CreateDirectory(Path.Combine(env.RemoteWorkspace, ".cement"));

                List<Dep> modulesToUpdate;
                Dictionary<string, string> currentCommitHashes;
                List<Dep> topSortedDeps;
                new BuildPreparer(Log).GetModulesOrder("A", null, out topSortedDeps, out modulesToUpdate, out currentCommitHashes);

                CollectionAssert.AreEqual(new List<Dep>
                {
                    new Dep("X", null, "parent1"),
                    new Dep("X", null, "child3")
                }, topSortedDeps.Where(d => d.Name == "X"));
            }
        }
    }
}