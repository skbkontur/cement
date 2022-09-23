using System.IO;
using Commands;
using Common;
using Common.Logging;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CommandsTests
{
    public class TestChangeModule
    {
        private readonly ModuleHelper moduleHelper;

        public TestChangeModule()
        {
            moduleHelper = new ModuleHelper(LogManager.GetLogger<ModuleHelper>(), ConsoleWriter.Shared);
        }

        [Test]
        public void TestChangeUnexisting()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git
";

            var answer = @"";

            Assert.Throws<CementException>(() => TestChangeGit(oldModules, "asdf", null, "k@fetch", answer));
        }

        [Test]
        public void TestChangeNoChanges()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
";

            TestChangeGit(oldModules, "protobuf", null, "git@git.skbkontur.ru:ke/protobuf.git", oldModules);
        }

        [Test]
        public void TestChangeAll()
        {
            var oldModules = @"[module hello]
url = murl
pushurl = mpurl

";
            var answer = @"[module hello]
url = new_url
pushurl = new_purl

";

            TestChangeGit(oldModules, "hello", "new_purl", "new_url", answer);
        }

        [Test]
        public void TestChangeNoPush()
        {
            var oldModules = @"[module hello]
url = murl
pushurl = mpurl

";
            var answer = @"[module hello]
url = new_url


";

            TestChangeGit(oldModules, "hello", null, "new_url", answer);
        }

        private void TestChangeGit(string oldContent, string moduleName, string push, string fetch, string answer)
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("modulesRepo");
                env.CommitIntoRemote("modulesRepo", "modules", oldContent);

                env.AddBranch("modulesRepo", "tmp");
                env.Checkout("modulesRepo", "tmp");
                var package = new Package("test_package", Path.Combine(env.RemoteWorkspace, "modulesRepo"));
                if (moduleHelper.ChangeModule(package, moduleName, push, fetch) != 0)
                    throw new CementException("");
                env.Checkout("modulesRepo", "master");

                var path = Path.Combine(env.RemoteWorkspace, "modulesRepo", "modules");
                var text = File.ReadAllText(path);
                text = Helper.FixLineEndings(text);
                answer = Helper.FixLineEndings(answer);
                Assert.AreEqual(answer, text);
            }
        }
    }
}
