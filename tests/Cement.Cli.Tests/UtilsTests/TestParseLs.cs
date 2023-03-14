using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

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