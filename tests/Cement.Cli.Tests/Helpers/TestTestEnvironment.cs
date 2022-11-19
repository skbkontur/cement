using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.YamlParsers;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.Helpers;

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

        Directory.Exists(Path.Combine(env.RemoteWorkspace, "A", ".git")).Should().BeTrue();
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

        var moduleFile = @"default:
full-build:
  build:
    target: None
    configuration: None
  deps:
    - B@/
".ReplaceLineEndings();

        File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")).Should().BeTrue();
        File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")).Should().BeEquivalentTo(moduleFile);
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

        var packageFile = @"default:
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
".ReplaceLineEndings();

        File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")).Should().BeTrue();
        File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")).Should().BeEquivalentTo(packageFile);
    }

    [Test]
    public void TestBranchesCreated()
    {
        using var env = new TestEnvironment();
        var branches = new[] {"b1", "b2", "b3"};

        env.CreateRepo("A", null, branches);
        var repo = gitRepositoryFactory.Create("A", env.RemoteWorkspace);

        repo.HasLocalBranch("b1").Should().BeTrue();
        repo.HasLocalBranch("b2").Should().BeTrue();
        repo.HasLocalBranch("b3").Should().BeTrue();
    }

    [Test]
    public void TestAppendInPackageConf()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.CreateRepo("B");

        var packageFile = @$"
[module A]
url={Path.Combine(env.RemoteWorkspace, "A")}

[module B]
url={Path.Combine(env.RemoteWorkspace, "B")}
".ReplaceLineEndings();

        File.ReadAllText(Path.Combine(env.RemoteWorkspace, env.PackageFile)).Should().BeEquivalentTo(packageFile);
    }

    [Test]
    public void TestGetModules()
    {
        using var env = new TestEnvironment();
        env.CreateRepo("A");
        env.CreateRepo("B");

        var modules = env.GetModules().Select(m => m.Name).ToArray();

        modules.Should().BeEquivalentTo("A", "B");
    }
}
