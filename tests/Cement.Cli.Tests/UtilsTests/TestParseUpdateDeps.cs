using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestParseUpdateDeps
{
    [Test]
    public void TestParseUpdateDepsAllArgs()
    {
        var args = new[]
        {
            "update-deps",
            "--force",
            "-c",
            "client"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.AreEqual("client", parsedArgs["configuration"]);
    }

    [Test]
    public void TestParseGetAllArgs()
    {
        var args = new[]
        {
            "get",
            "keweb",
            "nonmaster",
            "--force",
            "-c",
            "client"
        };
        var parsedArgs = ArgumentParser.ParseGet(args);
        Assert.AreEqual("client", parsedArgs["configuration"]);
    }

    [Test]
    public void TestParseGetDepFormat()
    {
        var args = new[]
        {
            "get",
            "keweb/client@develop"
        };
        var parsedArgs = ArgumentParser.ParseGet(args);
        Assert.AreEqual("client", parsedArgs["configuration"]);
        Assert.AreEqual("develop", parsedArgs["treeish"]);
    }

    [Test]
    public void TestParseGetDepFormatNull()
    {
        var args = new[]
        {
            "get",
            "keweb"
        };
        var parsedArgs = ArgumentParser.ParseGet(args);
        Assert.AreEqual(null, parsedArgs["configuration"]);
        Assert.AreEqual(null, parsedArgs["treeish"]);
    }

    [Test]
    public void TestParseBadArgs()
    {
        var args = new[]
        {
            "update-deps",
            "--force",
            "--reset"
        };
        Assert.Throws<BadArgumentException>(() => ArgumentParser.ParseUpdateDeps(args));
    }

    [Test]
    public void TestWitoutMerge()
    {
        var args = new[]
        {
            "update-deps"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.IsNull(parsedArgs["merged"]);
    }

    [Test]
    public void TestMergedDefaultIsMaster()
    {
        var args = new[]
        {
            "update-deps",
            "-m"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.AreEqual("master", parsedArgs["merged"]);
    }

    [Test]
    public void TestMerged()
    {
        var args = new[]
        {
            "update-deps",
            "--merged=new"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.AreEqual("new", parsedArgs["merged"]);
    }

    [Test]
    public void TestDefaultLocalForceIsFalse()
    {
        var args = new[]
        {
            "update-deps"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.IsFalse((bool)parsedArgs["localBranchForce"]);
    }

    [Test]
    public void TestForceLocalBranchKey()
    {
        var args = new[]
        {
            "update-deps",
            "--allow-local-branch-force"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.IsTrue((bool)parsedArgs["localBranchForce"]);
    }

    [Test]
    public void TestVerbose()
    {
        var args = new[]
        {
            "update-deps",
            "--verbose"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.IsTrue((bool)parsedArgs["verbose"]);
    }

    [Test]
    public void TestNoVerbose()
    {
        var args = new[]
        {
            "update-deps"
        };
        var parsedArgs = ArgumentParser.ParseUpdateDeps(args);
        Assert.IsFalse((bool)parsedArgs["verbose"]);
    }
}
