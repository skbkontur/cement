using System;
using Common;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestLastUpdateTime
{
    [Test]
    public void TestSaveCurrent()
    {
        var now = DateTime.UtcNow;
        Helper.SaveLastUpdateTime();
        var last = Helper.GetLastUpdateTime();

        last.Should().BeAfter(now);
        last.Should().BeWithin(TimeSpan.FromMinutes(2)).After(now);
    }
}
