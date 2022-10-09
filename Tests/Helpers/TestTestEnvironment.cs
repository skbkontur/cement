using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.YamlParsers;
using NUnit.Framework;

namespace Tests.Helpers;

[TestFixture]
public class TestTestEnvironment
{
    private readonly GitRepositoryFactory gitRepositoryFactory;

    public TestTestEnvironment()
    {
        var consoleWriter = ConsoleWriter.Shared;
        var buildHelper = BuildHelper.Shared;

        gitRepositoryFactory = new GitRepositoryFactory(consoleWriter, buildHelper);
    }

    [Test]
    public void TestRepoCreated()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        Assert.IsTrue(Directory.Exists(Path.Combine(env.RemoteWorkspace, "A", ".git")));
    }

    [Test]
    public void TestDepsCreatedYamlStyle()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})}
            });
        Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
        Assert.AreEqual(
            @"default:
full-build:
  build:
    target: None
    configuration: None
  deps:
    - B@/
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
    }

    [Test]
    public void TestDepsCreatedYamlStyleAdditionalConfig()
    {
        using var env = new TestEnvironment();
        env.CreateRepo(
            "A", new Dictionary<string, DepsData>
            {
                {"full-build", new DepsData(null, new List<Dep> {new("B")})},
                {"client", new DepsData(null, new List<Dep> {new("C")})}
            });
        Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
        Assert.AreEqual(
            @"default:
client:
  build:
    target: None
    configuration: None
  deps:
    - C@/

full-build:
  build:
    target: None
    configuration: None
  deps:
    - B@/
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
    }

    [Test]
    public void TestBranchesCreated()
    {
        using var env = new TestEnvironment();
        var branches = new[] {"b1", "b2", "b3"};
        env.CreateRepo("A", null, branches);
        var repo = gitRepositoryFactory.Create("A", env.RemoteWorkspace);
        Assert.IsTrue(repo.HasLocalBranch("b1"));
        Assert.IsTrue(repo.HasLocalBranch("b2"));
        Assert.IsTrue(repo.HasLocalBranch("b3"));
    }

    [Test]
    public void TestAppendInPackageConf()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.CreateRepo("B");
        Assert.AreEqual(
            $@"
[module A]
url={Path.Combine(env.RemoteWorkspace, "A")}

[module B]
url={
    Path.Combine(
        env.RemoteWorkspace, "B")
}
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, env.PackageFile)));
    }

    [Test]
    public void TestGetModules()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.CreateRepo("B");
        var modules = env.GetModules().Select(m => m.Name).ToArray();
        Assert.AreEqual(new[] {"A", "B"}, modules);
    }
}
