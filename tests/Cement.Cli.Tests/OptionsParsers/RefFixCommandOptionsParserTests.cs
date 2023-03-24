using System;
using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class RefFixCommandOptionsParserTests
{
    private readonly RefFixCommandOptionsParser parser;

    public RefFixCommandOptionsParserTests()
    {
        parser = new RefFixCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var args1 = new[] {"ref", "fix"};
            var expected1 = new RefFixCommandOptions(false);
            yield return new TestCaseData(args1, expected1) {TestName = "<not-args>"};

            var args2 = new[] {"ref", "fix", "--external"};
            var expected2 = new RefFixCommandOptions(true);
            yield return new TestCaseData(args2, expected2) {TestName = "--external"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            const string module = "module";

            var args1 = (object)new[] {"ref", "fix", module, "something.not-a-csproj"};
            yield return new TestCaseData(args1) {TestName = "invalid_project_name"};

            const string project = "project";

            var args2 = (object)new[] {"ref", "fix", module, project, "--extra_argument1"};
            yield return new TestCaseData(args2) {TestName = "extra_arguments"};

            var args3 = (object)Array.Empty<string>();
            yield return new TestCaseData(args3) {TestName = "no_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, RefFixCommandOptions expected)
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
