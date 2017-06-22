using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class TestDepFromYamlString
    {
        [Test]
        public void TestNoTreeishNoConfig()
        {
            var dep = new Dep("module");
            Assert.AreEqual("module", dep.Name);
        }

        [Test]
        public void TestTreeishNoConfig()
        {
            var dep = new Dep("module@branch");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("branch", dep.Treeish);
        }

        [Test]
        public void TestNoTreeishConfig()
        {
            var dep = new Dep("module/config");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config", dep.Configuration);
        }

        [Test]
        public void TestTreeishFirstConfig()
        {
            var dep = new Dep("module@branch/config");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config", dep.Configuration);
            Assert.AreEqual("branch", dep.Treeish);
        }

        [Test]
        public void TestTreeishConfigFirst()
        {
            var dep = new Dep("module/config@branch");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config", dep.Configuration);
            Assert.AreEqual("branch", dep.Treeish);
        }

        [Test]
        public void TestSlashInBranch1()
        {
            var dep = new Dep(@"module/config@branch\/1");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config", dep.Configuration);
            Assert.AreEqual("branch/1", dep.Treeish);
        }

        [Test]
        public void TestSlashInBranch2()
        {
            var dep = new Dep(@"module@branch\/1/config");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config", dep.Configuration);
            Assert.AreEqual("branch/1", dep.Treeish);
        }

        [Test]
        public void TestSlashAndAtChars()
        {
            var dep = new Dep(@"module/config\/1\@a@branch\/1");
            Assert.AreEqual("module", dep.Name);
            Assert.AreEqual("config/1@a", dep.Configuration);
            Assert.AreEqual("branch/1", dep.Treeish);
        }
    }
}