using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class UsagesShowCommandOptionsParserTests
{
    private readonly UsagesShowCommandOptionsParser parser;

    public UsagesShowCommandOptionsParserTests()
    {
        parser = new UsagesShowCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            const string module = "module";

            var args1 = new[] {"usages", "show", "-m", module};
            var expected1 = new UsagesShowCommandOptions(module, "*", "*", false, false);
            yield return new TestCaseData(args1, expected1) {TestName = "-m <module>"};

            const string configuration = "configuration";

            var args2 = new[] {"usages", "show", "-m", module, "-c", configuration};
            var expected2 = new UsagesShowCommandOptions(module, "*", configuration, false, false);
            yield return new TestCaseData(args2, expected2) {TestName = "-m <module> -c <configuration>"};

            var args3 = new[] {"usages", "show", "-m", $"{module}/{configuration}"};
            var expected3 = new UsagesShowCommandOptions(module, "*", configuration, false, false);
            yield return new TestCaseData(args3, expected3) {TestName = "-m <module>/<configuration>"};

            var args4 = new[] {"usages", "show", "-m", $"{module}/{configuration}", "-c", "configuration2"};
            var expected4 = new UsagesShowCommandOptions(module, "*", configuration, false, false);
            yield return new TestCaseData(args4, expected4) {TestName = "-m <module>/<configuration> -c <other-configuration>"};

            var args5 = new[] {"usages", "show", "-m", module, "-a"};
            var expected5 = new UsagesShowCommandOptions(module, "*", "*", true, false);
            yield return new TestCaseData(args5, expected5) {TestName = "-m <module> -a"};

            var args6 = new[] {"usages", "show", "-m", module, "-e"};
            var expected6 = new UsagesShowCommandOptions(module, "*", "*", false, true);
            yield return new TestCaseData(args6, expected6) {TestName = "-m <module> -e"};

            var args7 = new[] {"usages", "show", "-m", module, "-e", "-a"};
            var expected7 = new UsagesShowCommandOptions(module, "*", "*", true, true);
            yield return new TestCaseData(args7, expected7) {TestName = "-m <module> -e -a"};

            const string branch = "branch";

            var args8 = new[] {"usages", "show", "-m", module, "-b", branch};
            var expected8 = new UsagesShowCommandOptions(module, branch, "*", false, false);
            yield return new TestCaseData(args8, expected8) {TestName = "-m <module> -b <branch>"};

            var args9 = new[] {"usages", "show", "-m", module, "-b", branch, "-a"};
            var expected9 = new UsagesShowCommandOptions(module, branch, "*", true, false);
            yield return new TestCaseData(args9, expected9) {TestName = "-m <module> -b <branch> -a"};

            var args10 = new[] {"usages", "show", "-m", module, "-b", branch, "-c", configuration};
            var expected10 = new UsagesShowCommandOptions(module, branch, configuration, false, false);
            yield return new TestCaseData(args10, expected10) {TestName = "-m <module> -b <branch> -c <configuration>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            const string module = "module";

            var args1 = (object)new[] {"usages", "show", "-m", module, "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args1) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, UsagesShowCommandOptions expected)
    {
        // arrange
        // dstarasov: тесты в IDE по умолчанию запускаются с WorkingDirectory в корне проекта.
        // dstarasov: сement.cli сам по себе является cement-модулем и это влияет на то, как парсер определяет moduleName

        using var tempDirectory = new TempDirectory();
        using var _ = new DirectoryJumper(tempDirectory.Path);

        // act
        var actual = parser.Parse(args);

        // assert
        actual.Should().BeEquivalentTo(expected);
    }

    [TestCaseSource(nameof(FaultTestCases))]
    public void Should_fault(string[] args)
    {
        // arrange
        // dstarasov: тесты в IDE по умолчанию запускаются с WorkingDirectory в корне проекта.
        // dstarasov: сement.cli сам по себе является cement-модулем и это влияет на то, как парсер определяет moduleName

        using var tempDirectory = new TempDirectory();
        using var _ = new DirectoryJumper(tempDirectory.Path);

        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }

    [Test]
    public void Should_fault_when_current_directory_is_not_a_module_and_module_name_is_not_specified()
    {
        // arrange
        // dstarasov: тесты в IDE по умолчанию запускаются с WorkingDirectory в корне проекта.
        // dstarasov: сement.cli сам по себе является cement-модулем и это влияет на то, как парсер определяет moduleName

        using var tempDirectory = new TempDirectory();
        using var _ = new DirectoryJumper(tempDirectory.Path);

        const string module = "module";
        var args = new[] {"usages", "show", "-m", module, "--extra_argument1", "--extra_arguments2"};

        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<BadArgumentException>();
    }
}
