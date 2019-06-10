using System.Collections.Generic;
using Common;
using Common.YamlParsers;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class TestCycleDetector
    {
        [Test]
        public void TestSimpleCycle()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/full-build", "B/full-build", "A/full-build"}, cycle.ToArray());
            }
        }

        [Test]
        public void TestLongCycle()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("E")})}
                });
                env.CreateRepo("E", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "D/full-build", "E/full-build", "A/full-build"}, cycle.ToArray());
            }
        }

        [Test]
        public void TestNoCycleInDirectedGraph()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep>())}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.IsNull(cycle);
            }
        }

        [Test]
        public void TestCycleDontHaveSourceModule()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "D/full-build", "B/full-build"}, cycle.ToArray());
            }
        }

        [Test]
        public void TestFindAnyCycleFromManyCycles()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.IsNotNull(cycle);
            }
        }

        [Test]
        public void TestNoCycleIfDifferentConfigs()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})},
                    {"client", new DepsData(null, new List<Dep>())}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A", null, "client")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A/full-build");
                Assert.IsNull(cycle);
            }
        }

        [Test]
        public void TestHaveCycleIfSameConfigs()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})},
                    {"client", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A", null, "client")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A/client");
                Assert.AreEqual(new[] {"A/client", "B/full-build", "A/client"}, cycle);
            }
        }

        [Test]
        public void TestCycleWithDefaultConfig()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})},
                    {"client *default", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("A", null, "full-build")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/client", "B/full-build", "A/full-build", "B/full-build"}, cycle);
            }
        }

        [Test]
        public void TestCycleWithPredperiod()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                env.CreateRepo("B", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "B/full-build"}, cycle.ToArray());
            }
        }

        [Test]
        public void TestCycleWithTwoWays()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B"), new Dep("C")})}
                });
                env.CreateRepo("C", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("D")})}
                });
                env.CreateRepo("D", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                Helper.SetWorkspace(env.RemoteWorkspace);
                var cycle = CycleDetector.TryFindCycle("A");
                Assert.AreEqual(new[] {"A/full-build", "C/full-build", "D/full-build", "C/full-build"}, cycle.ToArray());
            }
        }
    }
}