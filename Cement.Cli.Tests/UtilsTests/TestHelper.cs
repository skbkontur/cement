using System;
using System.Diagnostics.CodeAnalysis;
using Common;
using FluentAssertions;
using NUnit.Framework;

namespace Cement.Cli.Tests.UtilsTests;

[TestFixture]
public class TestHelper
{
    [Test]
    public void TestConvertTime()
    {
        var ts = new TimeSpan();
        ts += TimeSpan.FromDays(2);
        ts += TimeSpan.FromHours(3);
        ts += TimeSpan.FromMinutes(15);
        ts += TimeSpan.FromSeconds(12);
        ts += TimeSpan.FromMilliseconds(777);

        Helper.ConvertTime((long)ts.TotalMilliseconds).Should().Be("2:03:15:12.777");
    }

    [Test]
    public void TestConvertTimeOnlyMinutesSecondsAndMillis()
    {
        var ts = new TimeSpan();
        ts += TimeSpan.FromMinutes(15);
        ts += TimeSpan.FromSeconds(3);
        ts += TimeSpan.FromMilliseconds(777);

        Helper.ConvertTime((long)ts.TotalMilliseconds).Should().Be("15:03.777");
    }

    [Test]
    public void TestConvertTimeOnlySecondsAndMillis()
    {
        var ts = new TimeSpan();
        ts += TimeSpan.FromSeconds(3);
        ts += TimeSpan.FromMilliseconds(777);

        Helper.ConvertTime((long)ts.TotalMilliseconds).Should().Be("3.777");
    }

    [Test]
    public void TestConvertTimeOnlyMillis()
    {
        var ts = new TimeSpan();
        ts += TimeSpan.FromMilliseconds(777);

        Helper.ConvertTime((long)ts.TotalMilliseconds).Should().Be(".777");
    }

    [Test]
    public void TestConvertTimeZero()
    {
        Helper.ConvertTime(0).Should().Be(".000");
    }

    [Test]
    public void TestConvertTimeOneSecond()
    {
        Helper.ConvertTime(1000).Should().Be("1.000");
    }

    [Test]
    [Platform(Include = "Win", Reason = "Windows-only algorithm")]
    [SuppressMessage("ReSharper", "StringLiteralTypo")]
    public void TestEncrypt()
    {
        const string text = "a;sdldvdna;lgfuoawviaewhgf2354o5u34orofh4HGR:GL:LGJ";

        var enc = Helper.Encrypt(text);
        var dec = Helper.Decrypt(enc);

        dec.Should().BeEquivalentTo(text);
        enc.Should().NotBe(text);
    }
}
