using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class UsagesBuildCommandOptionsParserTests
{
    private readonly UsagesBuildCommandOptionsParser parser;

    public UsagesBuildCommandOptionsParserTests()
    {
        parser = new UsagesBuildCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"usages", "build"};
            var expected1 = new UsagesBuildCommandOptions(false, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string branch = "branch";

            var args2 = new[] {"usages", "build", "-b", branch};
            var expected2 = new UsagesBuildCommandOptions(false, branch);
            yield return new TestCaseData(args2, expected2) {TestName = "-b <branch>"};

            var args3 = new[] {"usages", "build", "-p"};
            var expected3 = new UsagesBuildCommandOptions(true, null);
            yield return new TestCaseData(args3, expected3) {TestName = "-p"};

            var args4 = new[] {"usages", "build", "-b", branch, "-p"};
            var expected4 = new UsagesBuildCommandOptions(true, branch);
            yield return new TestCaseData(args4, expected4) {TestName = "-b <branch> -p"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"usages", "build", "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, UsagesBuildCommandOptions expected)
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
