using System;
using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class UsagesGrepCommandOptionsParserTests
{
    private readonly UsagesGrepCommandOptionsParser parser;

    public UsagesGrepCommandOptionsParserTests()
    {
        parser = new UsagesGrepCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            var defaultGitArgs = new[] {"usages", "grep"};

            var args1 = new[] {"usages", "grep"};
            var expected1 = new UsagesGrepCommandOptions(defaultGitArgs, Array.Empty<string>(), false, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<no-args>"};

            const string branch = "branch";

            var args2 = new[] {"usages", "grep", "-b", branch};
            var expected2 = new UsagesGrepCommandOptions(defaultGitArgs, Array.Empty<string>(), false, branch);
            yield return new TestCaseData(args2, expected2) {TestName = "-b <branch>"};

            var args3 = new[] {"usages", "grep", "-s"};
            var expected3 = new UsagesGrepCommandOptions(defaultGitArgs, Array.Empty<string>(), true, null);
            yield return new TestCaseData(args3, expected3) {TestName = "-s"};

            var args4 = new[] {"usages", "grep", "-b", branch, "-s"};
            var expected4 = new UsagesGrepCommandOptions(defaultGitArgs, Array.Empty<string>(), true, branch);
            yield return new TestCaseData(args4, expected4) {TestName = "-b <branch> -s"};

            var args5 = new[] {"usages", "grep", "--"};
            var expected5 = new UsagesGrepCommandOptions(defaultGitArgs, Array.Empty<string>(), false, null);
            yield return new TestCaseData(args5, expected5) {TestName = "-- <no-file-masks>"};

            var args6 = new[] {"usages", "grep", "--", "file-mask1", "file-mask2"};
            var fileMasks6 = new[] {"file-mask1", "file-mask2"};
            var expected6 = new UsagesGrepCommandOptions(defaultGitArgs, fileMasks6, false, null);
            yield return new TestCaseData(args6, expected6) {TestName = "-- <file-masks>"};

            var args7 = new[] {"usages", "grep", "-s", "--", "file-mask1", "file-mask2"};
            var fileMasks7 = new[] {"file-mask1", "file-mask2"};
            var expected7 = new UsagesGrepCommandOptions(defaultGitArgs, fileMasks7, true, null);
            yield return new TestCaseData(args7, expected7) {TestName = "-s -- <file-masks>"};

            var args8 = new[] {"usages", "grep", "git-argument1", "git-argument2", "--", "file-mask1", "file-mask2"};
            var gitArgs8 = new[] {"usages", "grep", "git-argument1", "git-argument2"};
            var fileMasks8 = new[] {"file-mask1", "file-mask2"};
            var expected8 = new UsagesGrepCommandOptions(gitArgs8, fileMasks8, false, null);
            yield return new TestCaseData(args8, expected8) {TestName = "<git-arguments> -- <file-masks>"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, UsagesGrepCommandOptions expected)
    {
        // arrange
        // act
        var actual = parser.Parse(args);

        // assert
        actual.Should().BeEquivalentTo(expected);
    }
}
