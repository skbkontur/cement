using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class SelfUpdateCommandOptionsParserTests
{
    private readonly SelfUpdateCommandOptionsParser parser;

    public SelfUpdateCommandOptionsParserTests()
    {
        parser = new SelfUpdateCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"self-update"};
            var expected1 = new SelfUpdateCommandOptions(null, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string branch = "branch";

            var args2 = new[] {"self-update", "-b", branch};
            var expected2 = new SelfUpdateCommandOptions(branch, null);
            yield return new TestCaseData(args2, expected2) {TestName = "-b <branch>"};

            const string server = "server";

            var args3 = new[] {"self-update", "-s", server};
            var expected3 = new SelfUpdateCommandOptions(null, server);
            yield return new TestCaseData(args3, expected3) {TestName = "-s <server>"};

            var args4 = new[] {"self-update", "-b", branch, "-s", server};
            var expected4 = new SelfUpdateCommandOptions(branch, server);
            yield return new TestCaseData(args4, expected4) {TestName = "-b <branch> -s <server>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"self-update", "--extra_argument1"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, SelfUpdateCommandOptions expected)
    {
        // arrange
        // act
        var actual = parser.Parse(args);

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [TestCaseSource(nameof(FaultTestCases))]
    public void Should_fail(string[] args)
    {
        // arrange
        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }
}
