using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
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

[TestFixture]
public class TestParseLs
{
    private readonly LsCommandOptionsParser parser;

    public TestParseLs()
    {
        parser = new LsCommandOptionsParser();
    }

    [Test]
    public void TestParseAllArgsWithLocalKey()
    {
        // arrange
        const string branchName = "branch";
        var expected = new LsCommandOptions(false, ModuleProcessType.Local, true, true, branchName);

        // act
        var actual = parser.Parse(new[] {"ls", "-l", "-b", branchName, "-u", "-p"});

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void TestParseAllArgsPartWithAllKey()
    {
        // arrange
        const string branchName = "branch";
        var expected = new LsCommandOptions(false, ModuleProcessType.All, true, true, branchName);

        // act
        var actual = parser.Parse(new[] {"ls", "-a", "-b", branchName, "-u", "-p"});

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void TestParseBranchName()
    {
        // arrange
        const string branchName = "branch";
        var expected = new LsCommandOptions(false, ModuleProcessType.Local, false, false, branchName);

        // act
        var actual = parser.Parse(new[] {"ls", "-b", branchName});

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [Test]
    public void TestThrowsOnMutuallyExclusiveKeys()
    {
        // arrange
        // act
        var act = () => parser.Parse(new[] {"ls", "-l", "-a"});

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }

    [Test]
    public void TestThrowsOnExtraKeys()
    {
        // arrange
        // act
        var act = () => parser.Parse(new[] {"ls", "-r"});

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }
}

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
