using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Commands;
using Common;
using Common.YamlParsers;
using NUnit.Framework;

namespace Tests.CommandsTests
{
    [TestFixture]
    public class TestConvertSpec
    {
        private TempDirectory tempDirectory;
        private string startDirectory;

        [SetUp]
        public void SetUp()
        {
            startDirectory = Directory.GetCurrentDirectory();
            tempDirectory = new TempDirectory();
            Directory.SetCurrentDirectory(tempDirectory.Path);
            Helper.SetWorkspace(tempDirectory.Path);
            Directory.CreateDirectory(".cement");
            Directory.CreateDirectory("module");
            Directory.SetCurrentDirectory("module");
        }

        [TearDown]
        public void TearDown()
        {
            Console.WriteLine("\n" + File.ReadAllText(Helper.YamlSpecFile));
            Directory.SetCurrentDirectory(startDirectory);
        }

        [Test]
        public void TestGetBuildSection()
        {
            AddBuildScript("kanso.sln", null);
            new ConvertSpecCommand().Run(new[] {"convert-spec"});

            var data = Yaml.BuildParser("module").Get();
            Assert.That(data.Count == 1);
            Assert.That(data[0].Configuration == "Release");
            Assert.That(data[0].Target == "kanso.sln");
        }

        [Test]
        public void TestSimpleDepsWithForce()
        {
            AddBuildScript("kanso.sln", null);
            var deps = new List<Dep> {new Dep("A@branch"), new Dep("B"), new Dep("C@conf"), new Dep("D/conf@branch")};
            AddDeps(deps, null, "%CURRENT_BRANCH%");
            new ConvertSpecCommand().Run(new[] {"convert-spec"});

            var yamlDeps = Yaml.DepsParser("module").Get();
            CollectionAssert.AreEqual(deps, yamlDeps.Deps);
            Assert.AreEqual("$CURRENT_BRANCH", yamlDeps.Force.Single());
        }

        [Test]
        public void TestSimpleWithTwoConfigs()
        {
            var deps = new List<Dep> {new Dep("A"), new Dep("B"), new Dep("C")};
            var depsClient = deps.Take(2).ToList();
            AddDeps(deps);
            AddDeps(depsClient, "client");
            AddBuildScript("kanso.sln", null);
            AddBuildScript("kanso.sln", "client");
            AddSpec(new List<string> {"client"});

            new ConvertSpecCommand().Run(new[] {"convert-spec"});
            var yamlDeps = Yaml.DepsParser("module").Get();
            var yamlDepsClient = Yaml.DepsParser("module").Get("client");
            CollectionAssert.AreEqual(deps, yamlDeps.Deps);
            CollectionAssert.AreEqual(depsClient, yamlDepsClient.Deps);
        }

        [Test]
        public void TestWithDepsOnOff()
        {
            var deps = new List<Dep> {new Dep("A"), new Dep("B"), new Dep("C")};
            var depsClient = new List<Dep> {new Dep("B/client"), new Dep("C")};
            AddDeps(deps);
            AddDeps(depsClient, "client");
            AddBuildScript("kanso.sln", null);
            AddBuildScript("kanso.sln", "client");
            AddSpec(new List<string> {"client"});

            new ConvertSpecCommand().Run(new[] {"convert-spec"});
            var yamlDeps = Yaml.DepsParser("module").Get();
            var yamlDepsClient = Yaml.DepsParser("module").Get("client");
            CollectionAssert.AreEquivalent(deps, yamlDeps.Deps);
            CollectionAssert.AreEquivalent(depsClient, yamlDepsClient.Deps);
        }

        private static void AddSpec(List<string> configurations)
        {
            Directory.CreateDirectory(".cm");
            using (var writer = File.CreateText(Path.Combine(".cm", "spec.xml")))
            {
                writer.WriteLine("<configurations>");
                foreach (var configuration in configurations)
                    writer.WriteLine("<conf name = \"" + configuration + "\"/>");
                writer.WriteLine("</configurations>");
            }
        }

        private static void AddBuildScript(string target, string configuration)
        {
            File.WriteAllLines(
                "build" + (configuration == null ? "" : "." + configuration) + ".cmd",
                new[]
                {
                    "target  =  " + target,
                    $"msbuild d ;lk  wqel k ;lkdf   /p:Configuration={configuration ?? "Release"} target  asdf"
                });
        }

        private static void AddDeps(List<Dep> deps, string configuration = null, string force = null)
        {
            var depsFileName = "deps" + (configuration == null ? "" : "." + configuration);
            WriteDeps(deps, force, depsFileName);

            var readedDeps = new DepsIniParser(new FileInfo(depsFileName)).Get();
            CollectionAssert.AreEquivalent(readedDeps.Deps, deps);
            Assert.AreEqual(force, readedDeps.Force?.Single());
        }

        private static void WriteDeps(List<Dep> deps, string force, string depsFileName)
        {
            using (var writer = File.CreateText(depsFileName))
            {
                if (force != null)
                    writer.WriteLine("[main]\nforce = " + force);
                foreach (var dep in deps)
                {
                    writer.WriteLine("[module " + dep.Name + "]");
                    if (dep.Configuration != null)
                        writer.WriteLine("conf=" + dep.Configuration);
                    if (dep.Treeish != null)
                        writer.WriteLine("treeish=" + dep.Treeish);
                }
            }
        }
    }
}
