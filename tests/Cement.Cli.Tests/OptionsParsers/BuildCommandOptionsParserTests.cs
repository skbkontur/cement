using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class BuildCommandOptionsParserTests
{
    private readonly BuildCommandOptionsParser parser;

    public BuildCommandOptionsParserTests()
    {
        parser = new BuildCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var defaultBuildSettings = new BuildSettings
            {
                ShowAllWarnings = false,
                ShowObsoleteWarnings = false,
                ShowOutput = false,
                ShowProgress = false,
                ShowWarningsSummary = true,
                CleanBeforeBuild = false
            };

            var args1 = new[] {"build"};
            var expected1 = new BuildCommandOptions(null, defaultBuildSettings);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string configuration = "configuration";

            var args2 = new[] {"build", "-c", configuration};
            var expected2 = new BuildCommandOptions(configuration, defaultBuildSettings);
            yield return new TestCaseData(args2, expected2) {TestName = "-c <configuration>"};

            var args3 = new[] {"build", "-w"};
            var expected3 = new BuildCommandOptions(null, defaultBuildSettings with {ShowAllWarnings = true});
            yield return new TestCaseData(args3, expected3) {TestName = "-w"};

            var args4 = new[] {"build", "-v"};
            var expected4 = new BuildCommandOptions(null, defaultBuildSettings with {ShowOutput = true});
            yield return new TestCaseData(args4, expected4) {TestName = "-v"};

            var args5 = new[] {"build", "-p"};
            var expected5 = new BuildCommandOptions(null, defaultBuildSettings with {ShowProgress = true});
            yield return new TestCaseData(args5, expected5) {TestName = "-p"};

            var args6 = new[] {"build", "--cleanBeforeBuild"};
            var expected6 = new BuildCommandOptions(null, defaultBuildSettings with {CleanBeforeBuild = true});
            yield return new TestCaseData(args6, expected6) {TestName = "--cleanBeforeBuild"};

            var args7 = new[] {"build", "-W"};
            var expected7 = new BuildCommandOptions(null, defaultBuildSettings with {ShowObsoleteWarnings = true});
            yield return new TestCaseData(args7, expected7) {TestName = "-W"};

            var args8 = new[] {"build", "-c", configuration, "-w"};
            var expected8 = new BuildCommandOptions(configuration, defaultBuildSettings with {ShowAllWarnings = true});
            yield return new TestCaseData(args8, expected8) {TestName = "-c <configuration> -w"};

            var args9 = new[] {"build", "-w", "-p"};
            var buildSettings9 = defaultBuildSettings with {ShowAllWarnings = true, ShowProgress = true};
            var expected9 = new BuildCommandOptions(null, buildSettings9);
            yield return new TestCaseData(args9, expected9) {TestName = "-w -p"};

            var args10 = new[] {"build", "-v", "--cleanBeforeBuild"};
            var buildSettings10 = defaultBuildSettings with {ShowOutput = true, CleanBeforeBuild = true};
            var expected10 = new BuildCommandOptions(null, buildSettings10);
            yield return new TestCaseData(args10, expected10) {TestName = "-v --cleanBeforeBuild"};

            var args11 = new[] {"build", "-v", "-W"};
            var buildSettings11 = defaultBuildSettings with {ShowOutput = true, ShowObsoleteWarnings = true};
            var expected11 = new BuildCommandOptions(null, buildSettings11);
            yield return new TestCaseData(args11, expected11) {TestName = "-v -W"};
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
    public void Should_parse(string[] args, BuildCommandOptions expected)
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
