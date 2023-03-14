using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class PackCommandOptionsParserTests
{
    private readonly PackCommandOptionsParser parser;

    public PackCommandOptionsParserTests()
    {
        parser = new PackCommandOptionsParser();
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, PackCommandOptions expected)
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
            const string projectName = "project.csproj";
            const string configuration = "configuration";
            var defaultBuildSettings = new BuildSettings
            {
                ShowAllWarnings = false,
                ShowObsoleteWarnings = false,
                ShowOutput = false,
                ShowProgress = false,
                ShowWarningsSummary = true,
                CleanBeforeBuild = false
            };

            var args1 = new[] {"pack", projectName};
            var expected1 = new PackCommandOptions(projectName, null, defaultBuildSettings, false);
            yield return new TestCaseData(args1, expected1) {TestName = "<projectName>"};

            var args2 = new[] {"pack", projectName, "-c", "configuration"};
            var expected2 = new PackCommandOptions(projectName, configuration, defaultBuildSettings, false);
            yield return new TestCaseData(args2, expected2) {TestName = "<projectName> -c <configuration>"};

            var args3 = new[] {"pack", projectName, "-w"};
            var buildSettings3 = defaultBuildSettings with {ShowAllWarnings = true};
            var expected3 = new PackCommandOptions(projectName, null, buildSettings3, false);
            yield return new TestCaseData(args3, expected3) {TestName = "<projectName> -w"};

            var args4 = new[] {"pack", projectName, "-W"};
            var buildSettings4 = defaultBuildSettings with {ShowObsoleteWarnings = true};
            var expected4 = new PackCommandOptions(projectName, null, buildSettings4, false);
            yield return new TestCaseData(args4, expected4) {TestName = "<projectName> -W"};

            var args5 = new[] {"pack", projectName, "-v"};
            var buildSettings5 = defaultBuildSettings with {ShowOutput = true};
            var expected5 = new PackCommandOptions(projectName, null, buildSettings5, false);
            yield return new TestCaseData(args5, expected5) {TestName = "<projectName> -v"};

            var args6 = new[] {"pack", projectName, "-p"};
            var buildSettings6 = defaultBuildSettings with {ShowProgress = true};
            var expected6 = new PackCommandOptions(projectName, null, buildSettings6, false);
            yield return new TestCaseData(args6, expected6) {TestName = "<projectName> -p"};

            var args7 = new[] {"pack", projectName, "--prerelease"};
            var expected7 = new PackCommandOptions(projectName, null, defaultBuildSettings, true);
            yield return new TestCaseData(args7, expected7) {TestName = "<projectName> --prerelease"};

            var args8 = new[] {"pack", projectName, "-c", configuration, "-w", "-W", "-v", "-p", "--prerelease"};
            var buildSettings8 = defaultBuildSettings with
            {
                ShowOutput = true,
                ShowAllWarnings = true,
                ShowProgress = true,
                ShowObsoleteWarnings = true
            };
            var expected8 = new PackCommandOptions(projectName, configuration, buildSettings8, true);
            yield return new TestCaseData(args8, expected8) {TestName = "<projectName> -c <configuration> -w -W -v -p --prerelease"};
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
            const string projectName = "project.csproj";

            var args1 = (object)new[] {"pack"};
            yield return new TestCaseData(args1) {TestName = "no_required_argument"};

            var args2 = (object)new[] {"pack", "something.not-a-csproj"};
            yield return new TestCaseData(args2) {TestName = "invalid_project_name"};

            var args3 = (object)new[] {"pack", projectName, "-r"};
            yield return new TestCaseData(args3) {TestName = "extra_arguments"};
        }
    }
}
