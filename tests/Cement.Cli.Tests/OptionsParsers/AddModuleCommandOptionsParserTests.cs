﻿using System;
using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.OptionsParsers;

[TestFixture]
public sealed class AddModuleCommandOptionsParserTests
{
    private readonly AddModuleCommandOptionsParser parser;

    public AddModuleCommandOptionsParserTests()
    {
        parser = new AddModuleCommandOptionsParser();
    }

    public static IEnumerable<TestCaseData> TestCases
    {
        get
        {
            const string module = "module";
            const string fetchUrl = "fetch-url";

            var args1 = new[] {"module", "add", module, fetchUrl};
            var expected1 = new AddModuleCommandOptions(module, null, fetchUrl, null);
            yield return new TestCaseData(args1, expected1) {TestName = "<module> <fetch-url>"};

            const string pushUrl = "push-url";

            var args2 = new[] {"module", "add", module, fetchUrl, "-p", pushUrl};
            var expected2 = new AddModuleCommandOptions(module, pushUrl, fetchUrl, null);
            yield return new TestCaseData(args2, expected2) {TestName = "<module> <fetch-url> -p <push-url>"};

            const string package = "package";

            var args3 = new[] {"module", "add", module, fetchUrl, "--package", package};
            var expected3 = new AddModuleCommandOptions(module, null, fetchUrl, package);
            yield return new TestCaseData(args3, expected3) {TestName = "<module> <fetch-url> --package <package>"};

            var args4 = new[] {"module", "add", module, fetchUrl, "-p", pushUrl, "--package", package};
            var expected4 = new AddModuleCommandOptions(module, pushUrl, fetchUrl, package);
            yield return new TestCaseData(args4, expected4) {TestName = "<module> <fetch-url> -p <push-url> --package <package>"};
        }
    }

    public static IEnumerable<TestCaseData> FaultTestCases
    {
        get
        {
            var args1 = (object)Array.Empty<string>();
            yield return new TestCaseData(args1) {TestName = "no_arguments"};

            const string module = "module";

            var args2 = (object)new[] {"module", "add", module};
            yield return new TestCaseData(args2) {TestName = "no_required_arguments"};

            const string fetchUrl = "fetch-url";

            var args3 = (object)new[] {"module", "add", module, fetchUrl, "--extra_argument1"};
            yield return new TestCaseData(args3) {TestName = "extra_arguments"};
        }
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, AddModuleCommandOptions expected)
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
