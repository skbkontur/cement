using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class CheckDepsCommandOptionsParserTests
{
    private readonly CheckDepsCommandOptionsParser parser;

    public CheckDepsCommandOptionsParserTests()
    {
        parser = new CheckDepsCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"check-deps"};
            var expected1 = new CheckDepsCommandOptions(null, false, false, false);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string configuration = "configuration";

            var args2 = new[] {"check-deps", "-c", configuration};
            var expected2 = new CheckDepsCommandOptions(configuration, false, false, false);
            yield return new TestCaseData(args2, expected2) {TestName = "-c <configuration>"};

            var args3 = new[] {"check-deps", "-a"};
            var expected3 = new CheckDepsCommandOptions(null, true, false, false);
            yield return new TestCaseData(args3, expected3) {TestName = "-a"};

            var args4 = new[] {"check-deps", "-c", configuration, "-a"};
            var expected4 = new CheckDepsCommandOptions(configuration, true, false, false);
            yield return new TestCaseData(args4, expected4) {TestName = "-c <configuration> -a"};

            var args5 = new[] {"check-deps", "-s"};
            var expected5 = new CheckDepsCommandOptions(null, false, false, true);
            yield return new TestCaseData(args5, expected5) {TestName = "-s"};

            var args6 = new[] {"check-deps", "-s", "-a"};
            var expected6 = new CheckDepsCommandOptions(null, true, false, true);
            yield return new TestCaseData(args6, expected6) {TestName = "-s -a"};

            var args7 = new[] {"check-deps", "-e"};
            var expected7 = new CheckDepsCommandOptions(null, false, true, false);
            yield return new TestCaseData(args7, expected7) {TestName = "-e"};

            var args8 = new[] {"check-deps", "-e", "-s"};
            var expected8 = new CheckDepsCommandOptions(null, false, true, true);
            yield return new TestCaseData(args8, expected8) {TestName = "-e -s"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"check-deps", "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, CheckDepsCommandOptions expected)
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
