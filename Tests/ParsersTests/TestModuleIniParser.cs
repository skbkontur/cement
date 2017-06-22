using System.Linq;
using Common;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestModuleIniParser
    {
        [Test]
        public void TestSampleModuleConfigWithComment()
        {
            var txt = @"[module localmodule]
;some comment
url = C:\temp\modules
pushurl = C:\temp\modules
[module abc]
url = C:\temp\abc\";
            var result = ModuleIniParser.Parse(txt);
            Assert.AreEqual(new[] {"localmodule", "abc"}, result.Select(p => p.Name).ToArray());
        }

        [Test]
        public void TestSkipsNonModuleSections()
        {
            var txt = @"[module local]
url = C:\temp\modules
type = file
[group abc]
modules =
 a
 b
 c
[module m]
url = C:\temp\abc\";
            var result = ModuleIniParser.Parse(txt);
            Assert.AreEqual(new[] {"local", "m"}, result.Select(m => m.Name));
        }

        [Test]
        public void TestSkipsModuleWithoutUrl()
        {
            var txt = @"[module withoutUrl]
type = git";
            Assert.AreEqual(0, ModuleIniParser.Parse(txt).Length);
        }

        [Test]
        public void TestDefaultTypeIsGit()
        {
            var txt = @"[module local]
url = C:\temp\modules";
            var type = ModuleIniParser.Parse(txt)[0].Type;
            Assert.AreEqual("git", type);
        }
    }
}
