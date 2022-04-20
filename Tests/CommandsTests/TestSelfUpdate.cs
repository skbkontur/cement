using System.IO;
using Commands;
using Common;
using NUnit.Framework;

namespace Tests.CommandsTests
{
    [TestFixture]
    public class TestSelfUpdate
    {
        [Test]
        public void TestDoSelfUpdate()
        {
            using (var tmp = new TempDirectory())
            {
                using (new DirectoryJumper(tmp.Path))
                {
                    new Init().Run(new[] {"init"});
                    if (Directory.Exists(Path.Combine(tmp.Path, Helper.CementDirectory)))
                    {
                        var exitCode = new SelfUpdate().Run(new[] {"self-update"});
                        Assert.AreEqual(0, exitCode);
                    }
                }
            }
        }
    }
}