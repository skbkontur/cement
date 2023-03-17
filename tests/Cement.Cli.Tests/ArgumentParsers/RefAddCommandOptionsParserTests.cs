using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.ArgumentParsers;

[TestFixture]
public sealed class RefAddCommandOptionsParserTests
{
    private readonly RefAddCommandOptionsParser parser;

    public RefAddCommandOptionsParserTests()
    {
        parser = new RefAddCommandOptionsParser();
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, RefAddCommandOptions expected)
    {
        // arrange
        // act
        var actual = parser.Parse(args);

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            const string module = "project";
            const string project = "project.csproj";

            var args1 = new[] {"ref", "add", module, project};
            var expected1 = new RefAddCommandOptions(project, new Dep(module), false, false);
            yield return new TestCaseData(args1, expected1) {TestName = "<module> <project>"};

            const string configuration = "configuration";
            var args2 = new[] {"ref", "add", module, project, "-c", configuration};
            var expected2 = new RefAddCommandOptions(project, new Dep(module) {Configuration = configuration}, false, false);
            yield return new TestCaseData(args2, expected2) {TestName = "<module> <project> -c <configuration>"};

            var args3 = new[] {"ref", "add", module, project, "--testReplaces"};
            var expected3 = new RefAddCommandOptions(project, new Dep(module), true, false);
            yield return new TestCaseData(args3, expected3) {TestName = "<module> <project> --testReplaces"};

            var args4 = new[] {"ref", "add", module, project, "--force"};
            var expected4 = new RefAddCommandOptions(project, new Dep(module), false, true);
            yield return new TestCaseData(args4, expected4) {TestName = "<module> <project> --force"};

            var args5 = new[] {"ref", "add", module, project, "-c", configuration, "--force"};
            var expected5 = new RefAddCommandOptions(project, new Dep(module) {Configuration = configuration}, false, true);
            yield return new TestCaseData(args5, expected5) {TestName = "<module> <project> -c <configuration> --force"};

            var args6 = new[] {"ref", "add", module, project, "--force", "--testReplaces"};
            var expected6 = new RefAddCommandOptions(project, new Dep(module), true, true);
            yield return new TestCaseData(args6, expected6) {TestName = "<module> <project> --force --testReplaces"};
        }
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

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            const string module = "module";

            var args1 = (object)new[] {"ref", "add", module, "something.not-a-csproj"};
            yield return new TestCaseData(args1) {TestName = "invalid_project_name"};

            const string project = "project";

            var args2 = (object)new[] {"analyzer", "add", module, project, "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args2) {TestName = "extra_arguments"};
        }
    }
}
