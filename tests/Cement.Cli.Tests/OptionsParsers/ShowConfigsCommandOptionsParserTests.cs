using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class ShowConfigsCommandOptionsParserTests
{
    private readonly ShowConfigsCommandOptionsParser parser;

    public ShowConfigsCommandOptionsParserTests()
    {
        parser = new ShowConfigsCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"show-configs"};
            var expected1 = new ShowConfigsCommandOptions(null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string module = "module";

            var args2 = new[] {"show-configs", module};
            var expected2 = new ShowConfigsCommandOptions(module);
            yield return new TestCaseData(args2, expected2) {TestName = "<module>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            const string module = "module";

            var args1 = (object)new[] {"show-configs", module, "--extra_argument1"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, ShowConfigsCommandOptions expected)
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
