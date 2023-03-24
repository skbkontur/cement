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
public sealed class AnalyzerAddCommandOptionsParserTests
{
    private readonly AnalyzerAddCommandOptionsParser parser;

    public AnalyzerAddCommandOptionsParserTests()
    {
        parser = new AnalyzerAddCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            const string moduleName = "module";

            var args1 = new[] {"analyzer", "add", moduleName};
            var expected1 = new AnalyzerAddCommandOptions(null, new Dep(moduleName));
            yield return new TestCaseData(args1, expected1) {TestName = "<module>"};

            const string solution = "solution";

            var args2 = new[] {"analyzer", "add", moduleName, solution};
            var expected2 = new AnalyzerAddCommandOptions(solution, new Dep(moduleName));
            yield return new TestCaseData(args2, expected2) {TestName = "<module> <solutionName>"};

            const string configuration = "configuration";

            var args3 = new[] {"analyzer", "add", moduleName, "-c", configuration};
            var expected3 = new AnalyzerAddCommandOptions(null, new Dep(moduleName) {Configuration = configuration});
            yield return new TestCaseData(args3, expected3) {TestName = "<module> -c <configuration>"};

            var args4 = new[] {"analyzer", "add", moduleName, solution, "-c", configuration};
            var expected4 = new AnalyzerAddCommandOptions(solution, new Dep(moduleName) {Configuration = configuration});
            yield return new TestCaseData(args4, expected4) {TestName = "<module> <solutionName> -c <configuration>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"analyzer", "add"};
            yield return new TestCaseData(args1) {TestName = "no_required_argument"};

            const string moduleName = "module";
            const string solution = "solution";

            var args2 = (object)new[] {"analyzer", "add", moduleName, solution, "--extra_argument1"};
            yield return new TestCaseData(args2) {TestName = "extra_arguments"};

            var args3 = (object)Array.Empty<string>();
            yield return new TestCaseData(args3) {TestName = "no_arguments"};

            var args4 = (object)new[] {"analyzer", "remove"};
            yield return new TestCaseData(args4) {TestName = "unknown_analyzer_subcommand"};

            var args5 = (object)new[] {"modules", "remove"};
            yield return new TestCaseData(args5) {TestName = "unknown_command"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, AnalyzerAddCommandOptions expected)
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
