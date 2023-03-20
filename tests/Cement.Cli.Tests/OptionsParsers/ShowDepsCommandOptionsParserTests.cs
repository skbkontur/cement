using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class ShowDepsCommandOptionsParserTests
{
    private readonly ShowDepsCommandOptionsParser parser;

    public ShowDepsCommandOptionsParserTests()
    {
        parser = new ShowDepsCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"show-deps"};
            var expected1 = new ShowDepsCommandOptions(null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string configuration = "configuration";

            var args2 = new[] {"show-deps", "-c", configuration};
            var expected2 = new ShowDepsCommandOptions(configuration);
            yield return new TestCaseData(args2, expected2) {TestName = "-c <configuration>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"show-deps", "--extra_argument1"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, ShowDepsCommandOptions expected)
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
