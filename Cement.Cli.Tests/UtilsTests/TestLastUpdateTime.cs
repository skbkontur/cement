using System;
using System.Threading.Tasks;
using Common;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestLastUpdateTime
{
    [Test, NonParallelizable]
    public async Task TestSaveCurrent()
    {
        var now = DateTime.Now;
        await Task.Delay(TimeSpan.FromSeconds(1));
        Helper.SaveLastUpdateTime();

        var last = Helper.GetLastUpdateTime();

        last.Should().BeAfter(now);
        last.Should().BeWithin(TimeSpan.FromMinutes(2)).After(now);
    }
}
