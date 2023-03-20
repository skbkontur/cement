using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class BuildDepsCommandOptionsParserTests
{
    private readonly BuildDepsCommandOptionsParser parser;

    public BuildDepsCommandOptionsParserTests()
    {
        parser = new BuildDepsCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var defaultBuildSettings = new BuildSettings
            {
                ShowAllWarnings = false,
                ShowOutput = false,
                ShowProgress = false,
                CleanBeforeBuild = false
            };

            var args1 = new[] {"build"};
            var expected1 = new BuildDepsCommandOptions(null, false, false, defaultBuildSettings);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string configuration = "configuration";

            var args2 = new[] {"build", "-c", configuration};
            var expected2 = new BuildDepsCommandOptions(configuration, false, false, defaultBuildSettings);
            yield return new TestCaseData(args2, expected2) {TestName = "-c <configuration>"};

            var args3 = new[] {"build", "-w"};
            var buildSettings3 = defaultBuildSettings with {ShowAllWarnings = true};
            var expected3 = new BuildDepsCommandOptions(null, false, false, buildSettings3);
            yield return new TestCaseData(args3, expected3) {TestName = "-w"};

            var args4 = new[] {"build", "-v"};
            var buildSettings4 = defaultBuildSettings with {ShowOutput = true};
            var expected4 = new BuildDepsCommandOptions(null, false, false, buildSettings4);
            yield return new TestCaseData(args4, expected4) {TestName = "-v"};

            var args5 = new[] {"build", "-p"};
            var buildSettings5 = defaultBuildSettings with {ShowProgress = true};
            var expected5 = new BuildDepsCommandOptions(null, false, false, buildSettings5);
            yield return new TestCaseData(args5, expected5) {TestName = "-p"};

            var args6 = new[] {"build", "--cleanBeforeBuild"};
            var buildSettings6 = defaultBuildSettings with {CleanBeforeBuild = true};
            var expected6 = new BuildDepsCommandOptions(null, false, false, buildSettings6);
            yield return new TestCaseData(args6, expected6) {TestName = "--cleanBeforeBuild"};

            var args7 = new[] {"build", "-r"};
            var expected7 = new BuildDepsCommandOptions(null, true, false, defaultBuildSettings);
            yield return new TestCaseData(args7, expected7) {TestName = "-r"};

            var args8 = new[] {"build", "-c", configuration, "-w"};
            var buildSettings8 = defaultBuildSettings with {ShowAllWarnings = true};
            var expected8 = new BuildDepsCommandOptions(configuration, false, false, buildSettings8);
            yield return new TestCaseData(args8, expected8) {TestName = "-c <configuration> -w"};

            var args9 = new[] {"build", "-w", "-p"};
            var buildSettings9 = defaultBuildSettings with {ShowAllWarnings = true, ShowProgress = true};
            var expected9 = new BuildDepsCommandOptions(null, false, false, buildSettings9);
            yield return new TestCaseData(args9, expected9) {TestName = "-w -p"};

            var args10 = new[] {"build", "-v", "--cleanBeforeBuild"};
            var buildSettings10 = defaultBuildSettings with {ShowOutput = true, CleanBeforeBuild = true};
            var expected10 = new BuildDepsCommandOptions(null, false, false, buildSettings10);
            yield return new TestCaseData(args10, expected10) {TestName = "-v --cleanBeforeBuild"};

            var args11 = new[] {"build", "-v", "-r"};
            var buildSettings11 = defaultBuildSettings with {ShowOutput = true};
            var expected11 = new BuildDepsCommandOptions(null, true, false, buildSettings11);
            yield return new TestCaseData(args11, expected11) {TestName = "-v -r"};

            var args12 = new[] {"build", "-q"};
            var expected12 = new BuildDepsCommandOptions(null, false, true, defaultBuildSettings);
            yield return new TestCaseData(args12, expected12) {TestName = "-q"};

            var args13 = new[] {"build", "-q", "-r"};
            var expected13 = new BuildDepsCommandOptions(null, true, true, defaultBuildSettings);
            yield return new TestCaseData(args13, expected13) {TestName = "-q -r"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)new[] {"build", "--extra_argument1"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};

            var args2 = (object)new[] {"build", "-v", "-w"};
            yield return new TestCaseData(args2) {TestName = "-v -w"};

            var args3 = (object)new[] {"build", "-v", "-p"};
            yield return new TestCaseData(args3) {TestName = "-v -p"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, BuildDepsCommandOptions expected)
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
