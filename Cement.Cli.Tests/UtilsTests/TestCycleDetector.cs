using System.Collections.Generic;
using Cement.Cli.Tests.Helpers;
using Common;
using Common.DepsValidators;
using Common.YamlParsers;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestCycleDetector
{
    private readonly CycleDetector cycleDetector;

    public TestCycleDetector()
    {
        var consoleWriter = ConsoleWriter.Shared;
        var depsValidatorFactory = new DepsValidatorFactory();
        cycleDetector = new CycleDetector(consoleWriter, depsValidatorFactory);
    }

    [Test]
    public void TestSimpleCycle()
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
                {"full-build", new DepsData(null, new List<Dep> {new("A")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);

        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/full-build", "B/full-build", "A/full-build"}, cycle.ToArray());
    }

    [Test]
    public void TestLongCycle()
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
                {"full-build", new DepsData(null, new List<Dep> {new("C")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("E")})}
            });
        env.CreateRepo(
            "E", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("A")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "D/full-build", "E/full-build", "A/full-build"}, cycle.ToArray());
    }

    [Test]
    public void TestNoCycleInDirectedGraph()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B"), new("C")})}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep>())}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.IsNull(cycle);
    }

    [Test]
    public void TestCycleDontHaveSourceModule()
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
                {"full-build", new DepsData(null, new List<Dep> {new("C")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "D/full-build", "B/full-build"}, cycle.ToArray());
    }

    [Test]
    public void TestFindAnyCycleFromManyCycles()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B"), new("C")})}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("A")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.IsNotNull(cycle);
    }

    [Test]
    public void TestNoCycleIfDifferentConfigs()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})},
                {"client", new DepsData(null, new List<Dep>())}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("A", null, "client")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A/full-build");
        Assert.IsNull(cycle);
    }

    [Test]
    public void TestHaveCycleIfSameConfigs()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})},
                {"client", new DepsData(null, new List<Dep> {new("B")})}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("A", null, "client")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A/client");
        Assert.AreEqual(new[] {"A/client", "B/full-build", "A/client"}, cycle);
    }

    [Test]
    public void TestCycleWithDefaultConfig()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})},
                {"client *default", new DepsData(null, new List<Dep> {new("B")})}
            });
        env.CreateRepo(
            "B", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("A", null, "full-build")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/client", "B/full-build", "A/full-build", "B/full-build"}, cycle);
    }

    [Test]
    public void TestCycleWithPredperiod()
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
                {"full-build", new DepsData(null, new List<Dep> {new("C")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/full-build", "B/full-build", "C/full-build", "B/full-build"}, cycle.ToArray());
    }

    [Test]
    public void TestCycleWithTwoWays()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B"), new("C")})}
            });
        env.CreateRepo(
            "C", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("D")})}
            });
        env.CreateRepo(
            "D", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("C")})}
            });
        Helper.SetWorkspace(env.RemoteWorkspace);
        var cycle = cycleDetector.TryFindCycle("A");
        Assert.AreEqual(new[] {"A/full-build", "C/full-build", "D/full-build", "C/full-build"}, cycle.ToArray());
    }
}
