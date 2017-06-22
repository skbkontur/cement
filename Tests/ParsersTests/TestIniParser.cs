using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    class TestIniParser
    {
        [Test]
        public void TestOneSectionsManyValues()
        {
            const string txt = @"[package localpackage]
url = C:\temp
type = file
a = b
";
            var parsed = new IniParser().ParseString(txt);
            var sections = parsed.GetSections();
            Assert.AreEqual(new[] {"package localpackage"}, sections);
            var options = parsed.GetKeys(sections[0]);
            Assert.AreEqual(new[] {"url", "type", "a"}, options);
        }

        [Test]
        public void TestManySections()
        {
            const string txt = @"[package s1]
url = C:\temp\A\

[package s2]
url = C:\temp\B\

[package s3]
url = C:\temp\C\

";
            var parsed = new IniParser().ParseString(txt);
            var sections = parsed.GetSections();
            Assert.AreEqual(new[] {"package s1", "package s2", "package s3"}, sections);
        }

        [Test]
        public void TestMultiline()
        {
            const string txt = @"
[module A]
multiline = 
 Q
 W
 E";
            var parsed = new IniParser().ParseString(txt);
            var ans = parsed.GetValue("multiline", "module A");
            Assert.AreEqual("\r\nQ\r\nW\r\nE", ans);
        }
    }
}