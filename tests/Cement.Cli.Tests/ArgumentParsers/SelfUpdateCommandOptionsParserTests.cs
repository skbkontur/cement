using System.Collections.Generic;
using Cement.Cli.Commands;
using Cement.Cli.Commands.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.ArgumentParsers;

[TestFixture]
public sealed class SelfUpdateCommandOptionsParserTests
{
    private readonly SelfUpdateCommandOptionsParser parser;

    public SelfUpdateCommandOptionsParserTests()
    {
        parser = new SelfUpdateCommandOptionsParser();
    }

    [TestCaseSource(nameof(TestCases))]
    public void Should_parse(string[] args, SelfUpdateCommandOptions expected)
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
            //todo(dstarasov): нужны тесткейсы
            yield break;
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
            //todo(dstarasov): нужны тесткейсы
            yield break;
        }
    }
}
