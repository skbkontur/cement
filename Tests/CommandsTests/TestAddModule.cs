using System.IO;
using Commands;
using Common;
using Common.Logging;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.CommandsTests
{
    [TestFixture]
    public class TestAddModule
    {
        private readonly ModuleHelper moduleHelper;

        public TestAddModule()
        {
            moduleHelper = new ModuleHelper(LogManager.GetLogger<ModuleHelper>(), ConsoleWriter.Shared);
        }

        [Test]
        public void TestAddInEmpty()
        {
            var oldModules = @"";
            var answer = @"
[module kanso]
url = k@fetch
pushurl = k@push
";

            TestAddGit(oldModules, "kanso", "k@push", "k@fetch", answer);
        }

        [Test]
        public void TestAddAppend()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git";

            var answer = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git
[module kanso]
url = k@fetch
pushurl = k@push
";

            TestAddGit(oldModules, "kanso", "k@push", "k@fetch", answer);
        }

        [Test]
        public void TestAddAppendWithNewLine()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git
";

            var answer = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git

[module kanso]
url = k@fetch
pushurl = k@push
";

            TestAddGit(oldModules, "kanso", "k@push", "k@fetch", answer);
        }

        [Test]
        public void TestAddWithoutPushUrl()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git
";

            var answer = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git

[module kanso]
url = k@fetch

";

            TestAddGit(oldModules, "kanso", null, "k@fetch", answer);
        }

        [Test]
        public void TestAddExisting()
        {
            var oldModules = @"[module protobuf]
url = git@git.skbkontur.ru:ke/protobuf.git
pushurl = git@git.skbkontur.ru:ke/protobuf.git
";

            var answer = @"";

            Assert.Throws<CementException>(() => TestAddGit(oldModules, "protobuf", null, "k@fetch", answer));
        }

        private void TestAddGit(string oldContent, string moduleName, string push, string fetch, string answer)
        {
            using (var env = new TestEnvironment())
            {


                env.CreateRepo("modulesRepo");
                env.CommitIntoRemote("modulesRepo", "modules", oldContent);

                env.AddBranch("modulesRepo", "tmp");
                env.Checkout("modulesRepo", "tmp");
                var package = new Package("test_package", Path.Combine(env.RemoteWorkspace, "modulesRepo"));
                if (moduleHelper.AddModule(package, moduleName, push, fetch) != 0)
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
