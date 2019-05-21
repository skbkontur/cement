using System.IO;
using Common;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.UtilsTests
{
    [TestFixture]
    public class TestInstallCollector
    {
        [Test]
        public void TestWithExternals()
        {
            var externalModuleText = @"
full-build:
    install:
        - external
        - nuget pExternal
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext
        - nuget pCurrent";
            using (var tempDir = new TempDirectory())
            {
                using (new DirectoryJumper(tempDir.Path))
                {
                    CreateModule("ext", externalModuleText);
                    CreateModule("cur", moduleText);
                    var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                    var installFiles = installData.InstallFiles.ToArray();
                    var nugetPackages = installData.NuGetPackages.ToArray();
                    Assert.AreEqual(new[] {@"cur\current", @"ext\external"}, installFiles);
                    Assert.AreEqual(new[] {"pCurrent", "pExternal"}, nugetPackages);
                }
            }
        }

        [Test]
        public void TestCollectInstallWithExternalClientConfig()
        {
            var externalModuleText = @"
full-build:
    install:
        - external
client:
    install:
        - external.client
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext/client
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(new[] {@"cur\current", @"ext\external.client"}, result.InstallFiles.ToArray());
            }
        }

        [Test]
        public void TestLongNestingsWithConfigs()
        {
            var qText = @"
full-build:
    install:
sdk:
    install:
        - q.sdk
        - module ext/client
";
            var externalModuleText = @"
full-build:
    install:
        - external
        - module q/sdk
client:
    install:
        - external.client
";
            var moduleText = @"
full-build:
    deps:
        - ext
    install:
        - current
        - module ext
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("q", qText);
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var result = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(new[] {@"cur\current", @"ext\external", @"q\q.sdk", @"ext\external.client"},
                    result.InstallFiles.ToArray());
            }
        }

        [Test]
        public void TestLongNestingsWithConfigsNexting()
        {
            var qText = @"
full-build:
    install:
sdk:
    install:
        - q.sdk
        - module ext/client
        - nuget q.sdk
";
            var externalModuleText = @"
full-build:
    install:
        - external
        - module q/sdk
        - nuget external
client:
    install:
        - external.client
        - nuget external.client
";
            var moduleText = @"
full-build > client:
    deps:
        - ext
    install:
        - current
        - module ext
        - nuget current
client:
    install:
        - current.client
        - module ext/client
        - nuget client
";
            using (var tempDir = new TempDirectory())
            using (new DirectoryJumper(tempDir.Path))
            {
                CreateModule("q", qText);
                CreateModule("ext", externalModuleText);
                CreateModule("cur", moduleText);
                var installData = new InstallCollector(Path.Combine(tempDir.Path, "cur")).Get();
                Assert.AreEqual(
                    new[] {@"cur\current", @"cur\current.client", @"ext\external", @"ext\external.client", @"q\q.sdk"},
                    installData.InstallFiles.ToArray());
                Assert.AreEqual(
                    new[] { "current", "client", "external", "external.client", "q.sdk" },
                    installData.NuGetPackages.ToArray());
            }
        }

        private void CreateModule(string moduleName, string content)
        {
            if (!Directory.Exists(moduleName))
            {
                Directory.CreateDirectory(moduleName);
            }
            var filePath = Path.Combine(moduleName, "module.yaml");
            File.WriteAllText(filePath, content);
        }
    }
}