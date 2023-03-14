using Cement.Cli.Commands.ArgumentsParsing;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestParseRefAddArgs
{
    [Test]
    public void TestWithoutConfiguration()
    {
        var args = new[] {"ref", "add", "kanso", "project.csproj"};
        var result = ArgumentParser.ParseRefAdd(args);
        Assert.AreEqual("kanso", result["module"]);
        Assert.AreEqual(null, result["configuration"]);
        Assert.AreEqual(false, result["testReplaces"]);
        Assert.AreEqual("project.csproj", result["project"]);
    }

    [Test]
    public void TestWithSlashConfig()
    {
        var args = new[] {"ref", "add", "kanso/client", "project.csproj"};
        var result = ArgumentParser.ParseRefAdd(args);
        Assert.AreEqual("kanso/client", result["module"]);
        Assert.AreEqual(false, result["testReplaces"]);
        Assert.AreEqual("project.csproj", result["project"]);
    }

    [Test]
    public void TestWithKeyConfig()
    {
        var args = new[] {"ref", "add", "kanso", "-c=client", "project.csproj"};
        var result = ArgumentParser.ParseRefAdd(args);
        Assert.AreEqual("kanso", result["module"]);
        Assert.AreEqual("client", result["configuration"]);
        Assert.AreEqual(false, result["testReplaces"]);
        Assert.AreEqual("project.csproj", result["project"]);
    }

    [Test]
    public void TestTestReplaces()
    {
        var args = new[] {"ref", "add", "kanso", "-c=client", "project.csproj", "--testReplaces"};
        var result = ArgumentParser.ParseRefAdd(args);
        Assert.AreEqual("kanso", result["module"]);
        Assert.AreEqual("client", result["configuration"]);
        Assert.AreEqual(true, result["testReplaces"]);
        Assert.AreEqual("project.csproj", result["project"]);
    }
}