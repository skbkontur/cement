using System;
using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class UpdateCommandOptionsParserTests
{
    private readonly UpdateCommandOptionsParser parser;

    public UpdateCommandOptionsParserTests()
    {
        parser = new UpdateCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"update"};
            var expected1 = new UpdateCommandOptions(null, false, LocalChangesPolicy.FailOnLocalChanges, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            var args2 = new[] {"update", "-r"};
            var expected2 = new UpdateCommandOptions(null, false, LocalChangesPolicy.Reset, null);
            yield return new TestCaseData(args2, expected2) {TestName = "-r"};

            var args3 = new[] {"update", "-p"};
            var expected3 = new UpdateCommandOptions(null, false, LocalChangesPolicy.Pull, null);
            yield return new TestCaseData(args3, expected3) {TestName = "-p"};

            var args4 = new[] {"update", "-f"};
            var expected4 = new UpdateCommandOptions(null, false, LocalChangesPolicy.ForceLocal, null);
            yield return new TestCaseData(args4, expected4) {TestName = "-f"};

            const string treeish = "treeish";

            var args5 = new[] {"update", treeish};
            var expected5 = new UpdateCommandOptions(treeish, false, LocalChangesPolicy.FailOnLocalChanges, null);
            yield return new TestCaseData(args5, expected5) {TestName = "<treeish>"};

            var args6 = new[] {"update", treeish, "-r"};
            var expected6 = new UpdateCommandOptions(treeish, false, LocalChangesPolicy.Reset, null);
            yield return new TestCaseData(args6, expected6) {TestName = "<treeish> -r"};

            var args7 = new[] {"update", "-v"};
            var expected7 = new UpdateCommandOptions(null, true, LocalChangesPolicy.FailOnLocalChanges, null);
            yield return new TestCaseData(args7, expected7) {TestName = "-v"};

            var args8 = new[] {"update", treeish, "-v"};
            var expected8 = new UpdateCommandOptions(treeish, true, LocalChangesPolicy.FailOnLocalChanges, null);
            yield return new TestCaseData(args8, expected8) {TestName = "<treeish> -v"};

            var args9 = new[] {"update", "--git-depth=13"};
            var expected9 = new UpdateCommandOptions(null, false, LocalChangesPolicy.FailOnLocalChanges, 13);
            yield return new TestCaseData(args9, expected9) {TestName = "--git-depth=<git-depth>"};

            var args10 = new[] {"update", "--git-depth=13", "-f"};
            var expected10 = new UpdateCommandOptions(null, false, LocalChangesPolicy.ForceLocal, 13);
            yield return new TestCaseData(args10, expected10) {TestName = "--git-depth=<git-depth> -r"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"update", "--force", "--reset"};
            yield return new TestCaseData(args1) {TestName = "--force --reset"};

            var args2 = (object)new[] {"update", "--force", "--pull-anyway"};
            yield return new TestCaseData(args2) {TestName = "--force --pull-anyway"};

            var args3 = (object)new[] {"update", "--reset", "--pull-anyway"};
            yield return new TestCaseData(args3) {TestName = "--reset --pull-anyway"};

            var args4 = (object)new[] {"update", "--force", "--reset", "--pull-anyway"};
            yield return new TestCaseData(args4) {TestName = "--force --reset --pull-anyway"};

            var args5 = (object)new[] {"update", "--extra_argument1"};
            yield return new TestCaseData(args5) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, UpdateCommandOptions expected)
    {
        // arrange
        // act
        var actual = parser.Parse(args);

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [TestCaseSource(nameof(FaultTestCases))]
    public void Should_fault(string[] args)
    {
        // arrange
        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }

    [Test]
    public void Should_fault_when_git_depth_is_not_a_number()
    {
        // arrange
        var args = new[] {"update", "--git-depth", "ten"};

        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<FormatException>();
    }
}
