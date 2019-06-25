using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Common;
using Common.YamlParsers;
using log4net;
using NUnit.Framework;

namespace Tests.Helpers
{
    public enum DepsFormatStyle
    {
        Ini,
        Yaml
    }

    public class TestEnvironment : IDisposable
    {
        public readonly TempDirectory WorkingDirectory;
        private ShellRunner runner;
        public readonly string PackageFile;
        public readonly string RemoteWorkspace;
        private static ILog Log = LogManager.GetLogger("TestEnvironment");

        public TestEnvironment()
        {
            runner = new ShellRunner();
            WorkingDirectory = new TempDirectory();
            Directory.CreateDirectory(Path.Combine(WorkingDirectory.Path, ".cement"));
            RemoteWorkspace = Path.Combine(WorkingDirectory.Path, "remote");
            Directory.CreateDirectory(Path.Combine(RemoteWorkspace, ".cement"));
            PackageFile = Path.Combine(WorkingDirectory.Path, "package.cmpkg");
            Helper.SetWorkspace(WorkingDirectory.Path);
        }

        public void CreateRepo(string moduleName, Dictionary<string, DepsData> depsByConfig = null, IList<string> branches = null, DepsFormatStyle depsStyle = DepsFormatStyle.Yaml, string pushUrl = null)
        {
            var modulePath = Path.Combine(RemoteWorkspace, moduleName);
            using (new DirectoryJumper(modulePath))
            {
                CreateRepoAndCommitReadme();
                CreateDepsAndCommitThem(modulePath, depsByConfig, depsStyle);
                CreateBranches(branches);
            }
            AppendModule(moduleName, modulePath, pushUrl);
        }

        public void Get(string module, string treeish = null, LocalChangesPolicy localChangesPolicy = LocalChangesPolicy.FailOnLocalChanges)
        {
            var getter = new ModuleGetter(
                GetModules().ToList(),
                new Dep(module, treeish),
                localChangesPolicy,
                null);

            getter.GetModule();
            getter.GetDeps();
        }

        public Module[] GetModules()
        {
            return ModuleIniParser.Parse(File.ReadAllText(PackageFile));
        }

        private void AppendModule(string moduleName, string modulePath, string pushUrl)
        {
            var sb = new StringBuilder()
                .AppendLine()
                .AppendLine($"[module {moduleName}]")
                .AppendLine($"url={modulePath}");

            if (pushUrl != null)
                sb.AppendLine($"pushurl={pushUrl}");


            File.AppendAllText(Path.Combine(RemoteWorkspace, PackageFile), sb.ToString());
        }

        private void CreateBranches(IList<string> branches)
        {
            if (branches == null)
                return;
            foreach (var branch in branches)
            {
                runner.Run("git branch " + branch);
            }
        }

        public void CreateDepsAndCommitThem(string path, Dictionary<string, DepsData> depsByConfig, DepsFormatStyle depsStyle = DepsFormatStyle.Yaml)
        {
            if (depsStyle == DepsFormatStyle.Yaml)
                CreateDepsYamlStyle(path, depsByConfig);
            if (depsStyle == DepsFormatStyle.Ini)
                CreateDepsIniStyle(path, depsByConfig);
        }

        private void CreateDepsYamlStyle(string path, Dictionary<string, DepsData> depsByConfig)
        {
            if (depsByConfig == null)
                return;

            var content = "default:";

            if (depsByConfig.Keys.Count == 0)
            {
                content = @"default:
full-build:
  build:
    target: None
    configuration: None";
            }

            foreach (var config in depsByConfig.Keys.OrderBy(x => x))
            {
                content += depsByConfig[config].Deps.Aggregate(
                    $@"
{config}:
  build:
    target: None
    configuration: None
  deps:{
                            (depsByConfig[config]
                                 .Force != null
                                ? $"\r\n    - force: " + string.Join(",", depsByConfig[config].Force)
                                : "")
                        }
", (current, dep) => current +
                     $"    - {dep.Name}@{dep.Treeish ?? ""}/{dep.Configuration ?? ""}\r\n");
            }


            File.WriteAllText(Path.Combine(path, "module.yaml"), content);
            runner.Run("git add module.yaml");
            runner.Run("git commit -am \"added deps\"");
        }

        private void CreateDepsIniStyle(string path, Dictionary<string, DepsData> depsByConfig)
        {
            if (depsByConfig == null)
                return;

            FillSpecFile(path, depsByConfig.Keys.ToList());

            foreach (var config in depsByConfig.Keys.OrderBy(x => x))
            {
                var content = "";
                if (depsByConfig[config] != null)
                {
                    content = depsByConfig[config].Force != null
                        ? @"[main]
force = " + string.Join(",", depsByConfig[config].Force) + "\r\n"
                        : "";
                    foreach (var dep in depsByConfig[config].Deps)
                    {
                        content += $"[module {dep.Name}]\r\n";
                        if (dep.Treeish != null)
                        {
                            content += $"treeish = {dep.Treeish}\r\n";
                        }
                        if (dep.Configuration != null)
                        {
                            content += $"configuration = {dep.Configuration}\r\n";
                        }
                    }
                }
                File.WriteAllText(Path.Combine(path,
                    $"deps{(config == "full-build" ? "" : "." + (config.StartsWith("*") ? config.Substring(1) : config))}"), content);
                runner.Run("git add " + $"deps{(config == "full-build" ? "" : "." + config)}");
            }
            runner.Run("git add .cm/");
            runner.Run("git add .cm/spec.xml");
            runner.Run("git commit -am \"added deps\"");
        }

        private void FillSpecFile(string path, IList<string> configs)
        {
            var defaultConfig = configs.FirstOrDefault(conf => conf.StartsWith("*"));
            var defaultConfigXmlSection = defaultConfig != null
                ? $"<default-config name = \"{defaultConfig.Substring(1)}\"/>\r\n"
                : "";
            var content = $@"
<configurations>
    {defaultConfigXmlSection}";
            foreach (var config in configs.Select(conf => conf.StartsWith("*") ? conf.Substring(1) : conf).OrderBy(x => x))
            {
                content += $"    <conf name = \"{config}\"/>\r\n";
            }
            content += "</configurations>";
            Directory.CreateDirectory(Path.Combine(path, ".cm"));
            File.WriteAllText(Path.Combine(path, ".cm", "spec.xml"), content);
        }

        private void CreateRepoAndCommitReadme()
        {
            runner.Run("git init");
            File.WriteAllText("README", "README");
            runner.Run("git add README");
            runner.Run("git commit -am \"initial commit\"");
        }

        public void Dispose()
        {
            Helper.SetWorkspace(null);
            WorkingDirectory.Dispose();
        }

        public void Checkout(string moduleName, string branch)
        {
            using (new DirectoryJumper(Path.Combine(RemoteWorkspace, moduleName)))
            {
                runner.Run("git checkout " + branch);
            }
        }

        public void AddBranch(string moduleName, string branch)
        {
            using (new DirectoryJumper(Path.Combine(RemoteWorkspace, moduleName)))
            {
                runner.Run("git branch " + branch);
            }
        }

        public void ChangeUrl(string repoPath, string destPath)
        {
            var content = File.ReadAllText(PackageFile);
            content = content.Replace(Path.Combine(RemoteWorkspace, repoPath), Path.Combine(RemoteWorkspace, destPath));
            File.Delete(PackageFile);
            File.WriteAllText(PackageFile, content);
        }

        public void CommitIntoLocal(string moduleName, string newfile, string content)
        {
            Commit(Path.Combine(WorkingDirectory.Path, moduleName), newfile, content);
        }

        public void CommitIntoRemote(string moduleName, string newfile, string content)
        {
            Commit(Path.Combine(RemoteWorkspace, moduleName), newfile, content);
        }

        private void Commit(string repoPath, string fileName, string content)
        {
            File.WriteAllText(Path.Combine(repoPath, fileName), content);
            using (new DirectoryJumper(repoPath))
            {
                runner.Run("git add " + fileName);
                runner.Run("git commit -am \"some commit\"");
            }
        }

        public void MakeLocalChanges(string moduleName, string file, string content)
        {
            File.WriteAllText(Path.Combine(WorkingDirectory.Path, moduleName, file), content);
        }
    }

    [TestFixture]
    public class TestTestEnvironment
    {
        [Test]
        public void TestRepoCreated()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A");
                Assert.IsTrue(Directory.Exists(Path.Combine(env.RemoteWorkspace, "A", ".git")));
            }
        }

        [Test]
        public void TestDepsCreatedYamlStyle()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                });
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
                Assert.AreEqual(@"default:
full-build:
  build:
    target: None
    configuration: None
  deps:
    - B@/
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
            }
        }

        [Test]
        public void TestDepsCreatedYamlStyleAdditionalConfig()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})},
                    {"client", new DepsData(null, new List<Dep> {new Dep("C")})}
                });
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
                Assert.AreEqual(@"default:
client:
  build:
    target: None
    configuration: None
  deps:
    - C@/

full-build:
  build:
    target: None
    configuration: None
  deps:
    - B@/
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "module.yaml")));
            }
        }

        [Test]
        public void TestDepsCreatedIniStyle()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B")})}
                }, null, DepsFormatStyle.Ini);
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "deps")));
                Assert.AreEqual(@"[module B]
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "deps")));
            }
        }

        [Test]
        public void TestDepsCreatedIniStyleComplex()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", "develop"), new Dep("C", null, "client"), new Dep("D", "release", "sdk")})}
                }, null, DepsFormatStyle.Ini);
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "deps")));
                Assert.AreEqual(@"[module B]
treeish = develop
[module C]
configuration = client
[module D]
treeish = release
configuration = sdk
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "deps")));
            }
        }

        [Test]
        public void TestDepsCreatedIniStyleAddedConfiguration()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", null},
                    {"client", new DepsData(null, new List<Dep> {new Dep("B")})}
                }, null, DepsFormatStyle.Ini);
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "deps.client")));
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", ".cm", "spec.xml")));
                Assert.IsTrue(File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", ".cm", "spec.xml")).Contains("<conf name = \"client\"/>"));
            }
        }

        [Test]
        public void TestDepsCreatedIniStyleAddedDefaultConfiguration()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", null},
                    {"*client", new DepsData(null, new List<Dep> {new Dep("B")})}
                }, null, DepsFormatStyle.Ini);
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "deps.client")));
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", ".cm", "spec.xml")));
                Assert.IsTrue(File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", ".cm", "spec.xml")).Contains("<conf name = \"client\"/>"));
                Assert.IsTrue(File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", ".cm", "spec.xml")).Contains("<default-config name = \"client\"/>"));
            }
        }

        [Test]
        public void TestDepsCreatedIniStyleComplexWithAdditionalConfiguration()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A", new Dictionary<string, DepsData>
                {
                    {"full-build", new DepsData(null, new List<Dep> {new Dep("B", "develop"), new Dep("C", null, "client"), new Dep("D", "release", "sdk")})},
                    {"client", new DepsData(null, new List<Dep> {new Dep("B", "develop"), new Dep("C", null, "client"), new Dep("D", "release", "sdk")})}
                }, null, DepsFormatStyle.Ini);
                Assert.IsTrue(File.Exists(Path.Combine(env.RemoteWorkspace, "A", "deps.client")));
                Assert.AreEqual(@"[module B]
treeish = develop
[module C]
configuration = client
[module D]
treeish = release
configuration = sdk
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, "A", "deps.client")));
            }
        }

        [Test]
        public void TestBranchesCreated()
        {
            using (var env = new TestEnvironment())
            {
                var branches = new[] {"b1", "b2", "b3"};
                env.CreateRepo("A", null, branches);
                var repo = new GitRepository("A", env.RemoteWorkspace, LogManager.GetLogger("Test"));
                Assert.IsTrue(repo.HasLocalBranch("b1"));
                Assert.IsTrue(repo.HasLocalBranch("b2"));
                Assert.IsTrue(repo.HasLocalBranch("b3"));
            }
        }

        [Test]
        public void TestAppendInPackageConf()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A");
                env.CreateRepo("B");
                Assert.AreEqual(
                    $@"
[module A]
url={Path.Combine(env.RemoteWorkspace, "A")}

[module B]
url={
                            Path.Combine(
                                env.RemoteWorkspace, "B")
                        }
", File.ReadAllText(Path.Combine(env.RemoteWorkspace, env.PackageFile)));
            }
        }

        [Test]
        public void TestGetModules()
        {
            using (var env = new TestEnvironment())
            {
                env.CreateRepo("A");
                env.CreateRepo("B");
                var modules = env.GetModules().Select(m => m.Name).ToArray();
                Assert.AreEqual(new[] {"A", "B"}, modules);
            }
        }
    }
}