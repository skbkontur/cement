using System;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
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
            var expected = "2:03:15:12.777";
            Assert.AreEqual(expected, Helper.ConvertTime((long) ts.TotalMilliseconds));
        }


        [Test]
        public void TestConvertTimeOnlyMinutesSecondsAndMillis()
        {
            var ts = new TimeSpan();
            ts += TimeSpan.FromMinutes(15);
            ts += TimeSpan.FromSeconds(3);
            ts += TimeSpan.FromMilliseconds(777);
            var expected = "15:03.777";
            Assert.AreEqual(expected, Helper.ConvertTime((long) ts.TotalMilliseconds));
        }

        [Test]
        public void TestConvertTimeOnlySecondsAndMillis()
        {
            var ts = new TimeSpan();
            ts += TimeSpan.FromSeconds(3);
            ts += TimeSpan.FromMilliseconds(777);
            var expected = "3.777";
            Assert.AreEqual(expected, Helper.ConvertTime((long) ts.TotalMilliseconds));
        }

        [Test]
        public void TestConvertTimeOnlyMillis()
        {
            var ts = new TimeSpan();
            ts += TimeSpan.FromMilliseconds(777);
            var expected = ".777";
            Assert.AreEqual(expected, Helper.ConvertTime((long) ts.TotalMilliseconds));
        }

        [Test]
        public void TestConvertTimeZero()
        {
            Assert.AreEqual(".000", Helper.ConvertTime(0));
        }

        [Test]
        public void TestConvertTimeOneSecond()
        {
            Assert.AreEqual("1.000", Helper.ConvertTime(1000));
        }

        [Test]
        public void TestEncrypt()
        {
            var text = "a;sdldvdna;lgfuoawviaewhgf2354o5u34orofh4HGR:GL:LGJ";
            var enc = Helper.Encrypt(text);
            var dec = Helper.Decrypt(enc);
            Assert.AreEqual(text, dec);
            Assert.AreNotEqual(text, enc);
        }
    }
}
