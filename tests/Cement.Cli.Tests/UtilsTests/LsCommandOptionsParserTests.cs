using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class LsCommandOptionsParserTests
{
    private readonly LsCommandOptionsParser parser;

    public LsCommandOptionsParserTests()
    {
        parser = new LsCommandOptionsParser();
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, LsCommandOptions expected)
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
            const string branchName = "branch";

            var args1 = new[] {"ls", "-l"};
            var expected1 = new LsCommandOptions(false, ModuleProcessType.Local, false, false, null);
            yield return new TestCaseData(args1, expected1) {TestName = "+l|local"};

            var args2 = new[] {"ls", "-a"};
            var expected2 = new LsCommandOptions(false, ModuleProcessType.All, false, false, null);
            yield return new TestCaseData(args2, expected2) {TestName = "+a|all"};

            var args3 = new[] {"ls", "-b", branchName};
            var expected3 = new LsCommandOptions(false, ModuleProcessType.Local, false, false, branchName);
            yield return new TestCaseData(args3, expected3) {TestName = "+b|has-branch"};

            var args4 = new[] {"ls", "-u"};
            var expected4 = new LsCommandOptions(false, ModuleProcessType.All, true, false, null);
            yield return new TestCaseData(args4, expected4) {TestName = "+u|url"};

            var args5 = new[] {"ls", "-p"};
            var expected5 = new LsCommandOptions(false, ModuleProcessType.All, false, true, null);
            yield return new TestCaseData(args5, expected5) {TestName = "+p|pushurl"};

            var args6 = new[] {"ls", "--simple"};
            var expected6 = new LsCommandOptions(true, ModuleProcessType.All, false, false, null);
            yield return new TestCaseData(args6, expected6) {TestName = "+simple"};

            var args7 = new[] {"ls"};
            var expected7 = new LsCommandOptions(false, ModuleProcessType.All, false, false, null);
            yield return new TestCaseData(args7, expected7) {TestName = "<no args>"};

            var args8 = new[] {"ls", "-a", "-b", branchName, "-u", "-p", "--simple"};
            var expected8 = new LsCommandOptions(true, ModuleProcessType.All, true, true, branchName);
            yield return new TestCaseData(args8, expected8) {TestName = "+a|all +<all args>"};

            var args9 = new[] {"ls", "-l", "-b", branchName, "-u", "-p", "--simple"};
            var expected9 = new LsCommandOptions(true, ModuleProcessType.Local, true, true, branchName);
            yield return new TestCaseData(args9, expected9) {TestName = "+l|local +<all args>"};
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
            var args1 = (object)new[] {"ls", "-l", "-a"};
            yield return new TestCaseData(args1) {TestName = "exclusive arguments"};

            var args2 = (object)new[] {"ls", "-r"};
            yield return new TestCaseData(args2) {TestName = "extra_arguments"};
        }
    }
}
