using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.ArgumentParsers;

[TestFixture]
public sealed class GetCommandOptionsParserTests
{
    private readonly GetCommandOptionsParser parser;

    public GetCommandOptionsParserTests()
    {
        parser = new GetCommandOptionsParser();
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, GetCommandOptions expected)
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
            const string module = "module";

            var args1 = new[] {"get", module};
            var expected1 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, null, null, false, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<module>"};

            var args2 = new[] {"get", module, "-r"};
            var expected2 = new GetCommandOptions(null, LocalChangesPolicy.Reset, module, null, null, false, null);
            yield return new TestCaseData(args2, expected2) {TestName = "<module> -r"};

            var args3 = new[] {"get", module, "-p"};
            var expected3 = new GetCommandOptions(null, LocalChangesPolicy.Pull, module, null, null, false, null);
            yield return new TestCaseData(args3, expected3) {TestName = "<module> -p"};

            var args4 = new[] {"get", module, "-f"};
            var expected4 = new GetCommandOptions(null, LocalChangesPolicy.ForceLocal, module, null, null, false, null);
            yield return new TestCaseData(args4, expected4) {TestName = "<module> -f"};

            var args5 = new[] {"get", module, "-m"};
            var expected5 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, null, "master", false, null);
            yield return new TestCaseData(args5, expected5) {TestName = "<module> -m"};

            const string branch = "branch";

            var args6 = new[] {"get", module, $"-m:{branch}"};
            var expected6 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, null, branch, false, null);
            yield return new TestCaseData(args6, expected6) {TestName = "<module> -m:<branch>"};

            var args7 = new[] {"get", module, "-v"};
            var expected7 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, null, null, true, null);
            yield return new TestCaseData(args7, expected7) {TestName = "<module> -v"};

            const int gitDepth = 13;
            var args8 = new[] {"get", module, $"--git-depth={gitDepth}"};
            var expected8 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, null, null, false, gitDepth);
            yield return new TestCaseData(args8, expected8) {TestName = "<module> --git-depth=<git-depth>"};

            const string configuration = "client";

            var args9 = new[] {"get", module, "-c", configuration};
            var expected9 = new GetCommandOptions(configuration, LocalChangesPolicy.FailOnLocalChanges, module, null, null, false, null);
            yield return new TestCaseData(args9, expected9) {TestName = "<module> -c <configuration>"};

            var args10 = new[] {"get", $"{module}@{branch}"};
            var expected10 = new GetCommandOptions(null, LocalChangesPolicy.FailOnLocalChanges, module, branch, null, false, null);
            yield return new TestCaseData(args10, expected10) {TestName = "<module@branch>"};

            var args11 = new[] {"get", $"{module}/{configuration}"};
            var expected11 = new GetCommandOptions(configuration, LocalChangesPolicy.FailOnLocalChanges, module, null, null, false, null);
            yield return new TestCaseData(args11, expected11) {TestName = "<module/configuration>"};

            var args12 = new[] {"get", $"{module}/{configuration}@{branch}"};
            var expected12 = new GetCommandOptions(configuration, LocalChangesPolicy.FailOnLocalChanges, module, branch, null, false, null);
            yield return new TestCaseData(args12, expected12) {TestName = "<module/configuration@branch>"};

            const string configuration2 = "core";

            var args13 = new[] {"get", $"{module}/{configuration}", "-c", configuration2};
            var expected13 = new GetCommandOptions(configuration, LocalChangesPolicy.FailOnLocalChanges, module, null, null, false, null);
            yield return new TestCaseData(args13, expected13) {TestName = "<module/configuration@branch> -c <configuration2>"};
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

            var args1 = (object)new[] {"get", module, "-f", "-r"};
            yield return new TestCaseData(args1) {TestName = "-f -r"};

            var args2 = (object)new[] {"get", module, "-f", "-p"};
            yield return new TestCaseData(args2) {TestName = "-f -p"};

            var args3 = (object)new[] {"get", module, "-r", "-p"};
            yield return new TestCaseData(args3) {TestName = "-r -p"};

            var args4 = (object)new[] {"get", module, "-f", "-r", "-p"};
            yield return new TestCaseData(args4) {TestName = "-f -r -p"};

            var args5 = (object)new[] {"get", module, "--extra_argument1", "--extra_arguments2"};
            yield return new TestCaseData(args5) {TestName = "extra_arguments"};

            /*var args6 = (object)new[] {"get", module, "-n"};
            yield return new TestCaseData(args6) {TestName = "-n (old_key)"};*/
        }
    }

    [Test]
    public void Should_fault_when_module_is_invalid()
    {
        // arrange
        const string invalidModule = "@branch";
        var args = new[] {"get", invalidModule};

        // act
        var act = () => parser.Parse(args);

        // assert
        act.Should().ThrowExactly<CementException>();
    }
}
