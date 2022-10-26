using System;
using Common;
using Common.DepsValidators;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.DepsValidatorsTests;

[TestFixture]
public class DepsValidatorTests
{
    private readonly DepsValidator validator;

    public DepsValidatorTests()
    {
        validator = new DepsValidator("full-build");
    }

    [Test]
    public void Should_validate_result_be_valid_when_configuration_has_no_deps()
    {
        // arrange
        var deps = Array.Empty<Dep>();

        // act
        var validateResult = validator.Validate(deps, out var validateErrors);

        //assert
        validateResult.Should().Be(DepsValidateResult.Valid);
        validateErrors.Should().BeEmpty();
    }

    [Test]
    public void Should_validate_result_be_valid_when_all_deps_are_unique()
    {
        // arrange
        var uniqueDeps = TestDepsFactory.GetUniqueDeps();

        // act
        var validateResult = validator.Validate(uniqueDeps, out var validateErrors);

        //assert
        validateResult.Should().Be(DepsValidateResult.Valid);
        validateErrors.Should().BeEmpty();
    }

    [Test]
    public void Should_validate_result_be_invalid_when_deps_are_non_unique()
    {
        // arrange
        var uniqueDeps = TestDepsFactory.GetNonUniqueDeps();

        // act
        var validateResult = validator.Validate(uniqueDeps, out var validateErrors);

        //assert
        validateResult.Should().Be(DepsValidateResult.Invalid);
        validateErrors.Should().NotBeEmpty();
    }
}
