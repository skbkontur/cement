using System.IO;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class DirectoryJumperTests
    {
        [Test]
        public void TestJumpToExistingDirectory()
        {
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                Assert.AreEqual(Directory.GetCurrentDirectory(), tempDir.Path);
            }
        }

        [Test]
        public void TestJumpBack()
        {
            var startCwd = Directory.GetCurrentDirectory();
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
            }

            Assert.AreEqual(startCwd, Directory.GetCurrentDirectory());
        }
    }
}
