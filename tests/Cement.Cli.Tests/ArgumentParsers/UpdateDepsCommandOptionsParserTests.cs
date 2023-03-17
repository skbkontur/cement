using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.ArgumentParsers;

[TestFixture]
public sealed class UpdateDepsCommandOptionsParserTests
{
    private readonly UpdateDepsCommandOptionsParser parser;

    public UpdateDepsCommandOptionsParserTests()
    {
        parser = new UpdateDepsCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"update-deps"};
            var expected1 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.FailOnLocalChanges, false, false, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            var args2 = new[] {"update-deps", "-r"};
            var expected2 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.Reset, false, false, null);
            yield return new TestCaseData(args2, expected2) {TestName = "-r"};

            var args3 = new[] {"update-deps", "-p"};
            var expected3 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.Pull, false, false, null);
            yield return new TestCaseData(args3, expected3) {TestName = "-p"};

            var args4 = new[] {"update-deps", "-f"};
            var expected4 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.ForceLocal, false, false, null);
            yield return new TestCaseData(args4, expected4) {TestName = "-f"};

            var args5 = new[] {"update-deps", "-m"};
            var expected5 = new UpdateDepsCommandOptions(null, "master", LocalChangesPolicy.FailOnLocalChanges, false, false, null);
            yield return new TestCaseData(args5, expected5) {TestName = "-m"};

            const string branch = "branch";

            var args6 = new[] {"update-deps", $"-m:{branch}"};
            var expected6 = new UpdateDepsCommandOptions(null, branch, LocalChangesPolicy.FailOnLocalChanges, false, false, null);
            yield return new TestCaseData(args6, expected6) {TestName = "-m:<branch>"};

            var args7 = new[] {"update-deps", "-v"};
            var expected7 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.FailOnLocalChanges, false, true, null);
            yield return new TestCaseData(args7, expected7) {TestName = "-v"};

            var args8 = new[] {"update-deps", "--allow-local-branch-force"};
            var expected8 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.FailOnLocalChanges, true, false, null);
            yield return new TestCaseData(args8, expected8) {TestName = "--allow-local-branch-force"};

            var args9 = new[] {"update-deps", "--git-depth=13"};
            var expected9 = new UpdateDepsCommandOptions(null, null, LocalChangesPolicy.FailOnLocalChanges, false, false, 13);
            yield return new TestCaseData(args9, expected9) {TestName = "--git-depth=<git-depth>"};

            const string configuration = "client";

            var args30 = new[] {"update-deps", "-c", configuration};
            var expected30 = new UpdateDepsCommandOptions(configuration, null, LocalChangesPolicy.FailOnLocalChanges, false, false, null);
            yield return new TestCaseData(args30, expected30) {TestName = "-c <configuration>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"update-deps", "--force", "--reset"};
            yield return new TestCaseData(args1) {TestName = "--force --reset"};

            var args2 = (object)new[] {"update-deps", "--force", "--pull-anyway"};
            yield return new TestCaseData(args2) {TestName = "--force --pull-anyway"};

            var args3 = (object)new[] {"update-deps", "--reset", "--pull-anyway"};
            yield return new TestCaseData(args3) {TestName = "--reset --pull-anyway"};

            var args4 = (object)new[] {"update-deps", "--force", "--reset", "--pull-anyway"};
            yield return new TestCaseData(args4) {TestName = "--force --reset --pull-anyway"};

            var args5 = (object)new[] {"update-deps", "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args5) {TestName = "extra_arguments"};

            var args6 = (object)new[] {"update-deps", "-n"};
            yield return new TestCaseData(args6) {TestName = "-n (old_key)"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, UpdateDepsCommandOptions expected)
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
}
