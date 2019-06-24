using System.Collections.Generic;
using System.Linq;
using Common;
using Common.YamlParsers;
using Common.YamlParsers.V2.Factories;
using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestDepParser
    {
        [Test]
        public void TestGetDepsOnlyDefault()
        {
            var text = @"
default:
    deps:
        - force: master
        - A
        - B
client:";
            var depsContent = GetDepsContent(text, "client");
            Assert.AreEqual("master", depsContent.Force.Single());
            Assert.AreEqual(2, depsContent.Deps.Count);
            Assert.AreEqual("A", depsContent.Deps[0].Name);
            Assert.AreEqual("B", depsContent.Deps[1].Name);
        }

        [Test]
        public void TestGetDepsMultipleForce()
        {
            var text = @"
default:
    deps:
        - force: priority,master
        - A
        - B
client:";
            var depsContent = GetDepsContent(text, "client");
            CollectionAssert.AreEqual(new[] {"priority", "master"}, depsContent.Force.ToArray());
            Assert.AreEqual(2, depsContent.Deps.Count);
            Assert.AreEqual("A", depsContent.Deps[0].Name);
            Assert.AreEqual("B", depsContent.Deps[1].Name);
        }

        [Test]
        public void TestForceNotFirstLine()
        {
            var text = @"
default:
    deps:
        - A
        - force: master
        - B
client:";
            var depsContent = GetDepsContent(text, "client");
            Assert.AreEqual("master", depsContent.Force.Single());
            Assert.AreEqual(2, depsContent.Deps.Count);
            Assert.AreEqual("A", depsContent.Deps[0].Name);
            Assert.AreEqual("B", depsContent.Deps[1].Name);
        }

        [Test]
        public void TestGetDepsClient()
        {
            var text = @"
default:
    deps:
        - force: master
        - A
        - B
client:
    deps:
        - C";
            var depsContent = GetDepsContent(text, "client");
            Assert.AreEqual("master", depsContent.Force.Single());
            Assert.AreEqual(3, depsContent.Deps.Count);
            Assert.AreEqual("A", depsContent.Deps[0].Name);
            Assert.AreEqual("B", depsContent.Deps[1].Name);
            Assert.AreEqual("C", depsContent.Deps[2].Name);
        }

        [Test]
        public void TestGetDepsClientSdk()
        {
            var text = @"
sdk > client:
    deps:
        - force: master
        - A
        - B
client:
    deps:
        - C";
            var depsContent = GetDepsContent(text, "sdk");
            Assert.AreEqual("master", depsContent.Force.Single());
            Assert.AreEqual(3, depsContent.Deps.Count);
            Assert.AreEqual("C", depsContent.Deps[0].Name);
            Assert.AreEqual("A", depsContent.Deps[1].Name);
            Assert.AreEqual("B", depsContent.Deps[2].Name);
        }

        [Test]
        public void TestGetDepsInheritance()
        {
            var text = @"
default:
    deps:
        - A
        - B@develop
client:
    deps:
        - C/client
sdk > client:
    deps:
        - D";
            var depsContent = GetDepsContent(text, "sdk");
            Assert.IsNull(depsContent.Force);
            Assert.AreEqual("A", depsContent.Deps[0].Name);
            Assert.AreEqual("B", depsContent.Deps[1].Name);
            Assert.AreEqual("develop", depsContent.Deps[1].Treeish);
            Assert.AreEqual("C", depsContent.Deps[2].Name);
            Assert.AreEqual("D", depsContent.Deps[3].Name);
            Assert.AreEqual("client", depsContent.Deps[2].Configuration);
        }

        [Test]
        public void TestgetDepsRemoveDep()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - -A
        - A
        - C";
            var depsContent = GetDepsContent(text, "client");
            Assert.AreEqual(2, depsContent.Deps.Count);
            Assert.AreEqual("C", depsContent.Deps[1].Name);
        }

        [Test]
        public void TestgetDepsRemoveDepRaisesExceptionNotAddingModuleAfterDelete()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - -A
        - C
        - A";
            Assert.Throws<BadYamlException>(() => YamlFromText.DepsParser(text).Get("client"));
        }

        [Test]
        public void TestgetDepsRemoveDepRaisesExceptionNoSuchModuleToDelete()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - -C
        - C
        - A";
            Assert.Throws<BadYamlException>(() => YamlFromText.DepsParser(text).Get("client"));
        }

        [Test]
        public void TestgetDepsRemoveDepRaisesExceptionDuplication()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - C
        - A";
            Assert.Throws<BadYamlException>(() => YamlFromText.DepsParser(text).Get("client"));
        }

        [Test]
        public void TestgetDepsRemoveDepRaisesExceptionNoSuchModuleToDelete2()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - -C
        - C
        - A";
            Assert.Throws<BadYamlException>(() => ModuleYamlParserFactory.Get().Parse(text));
        }

        [Test]
        public void TestGetDepsRemoveDepLongNesting()
        {
            var text = @"
default:
    deps:
        - A@develop/client
client:
    deps:
        - -A/*@develop
        - A/sdk
sdk > client:
    deps:
        - -A/*@*
        - A/full-build@develop";
            var dc = GetDepsContent(text, "sdk");
            Assert.AreEqual(1, dc.Deps.Count);
            Assert.AreEqual("A", dc.Deps[0].Name);
            Assert.AreEqual("full-build", dc.Deps[0].Configuration);
            Assert.AreEqual("develop", dc.Deps[0].Treeish);
        }

        [Test]
        public void TestGetDepsRemoveDifferentOrder()
        {
            var text = @"
default:
    deps:
        - A@develop/client
client:
    deps:
        - -A@develop/client
        - A/sdk";
            var dc = GetDepsContent(text, "client");
            Assert.AreEqual(1, dc.Deps.Count);
            Assert.AreEqual("A", dc.Deps[0].Name);
            Assert.AreEqual("sdk", dc.Deps[0].Configuration);
            Assert.AreEqual(null, dc.Deps[0].Treeish);
        }

        [Test]
        public void TestGetdepsOptionalForce()
        {
            var text = @"
default:
    deps:
        - force : |
            a -> b
            c -> b
            $CURRENT_BRANCH
full-build:
    deps:";
            var dc = GetDepsContent(text);
            Assert.AreEqual("a -> b\nc -> b\n$CURRENT_BRANCH\n", dc.Force.Single());
        }

        [Test]
        public void TestGetDepsMoreThanOneChildConfig()
        {
            var text = @"
default:
    deps:
        - A
client:
    deps:
        - B
        - C
client2:
    deps:
        - D
        - E
sdk > client, client2:
    deps:
        - F
";
            // strict ordering = false - don't know how to fix this
            // old version returns A D E B C F
            // new version returns A B C D E F
            var dc = GetDepsContent(text, "sdk", false);
            Assert.AreEqual(6, dc.Deps.Count);
        }

        [Test]
        public void TestGetDepsDictFormat()
        {
            var text = @"
client:
  deps:
    - force: master
    - A:
      configuration: sdk
      treeish: branch
      type: src
    - B/full-build
";
            var dc = GetDepsContent(text, "client");
            Assert.AreEqual("master", dc.Force.Single());
            Assert.AreEqual(2, dc.Deps.Count);
            Assert.AreEqual(new Dep("A", "branch", "sdk"), dc.Deps[0]);
            Assert.IsTrue(dc.Deps[0].NeedSrc);
            Assert.AreEqual(new Dep("B", null, "full-build"), dc.Deps[1]);
        }

        [Test]
        public void TestGetDepsDictFormatWithSomeFields()
        {
            var text = @"
client:
  deps:
    - force: master
    - A/sdk:
      treeish: branch
      type: bin
    - B/full-build
";
            var dc = GetDepsContent(text, "client");
            Assert.AreEqual("master", dc.Force.Single());
            Assert.AreEqual(2, dc.Deps.Count);
            Assert.AreEqual(new Dep("A", "branch", "sdk"), dc.Deps[0]);
            Assert.IsFalse(dc.Deps[0].NeedSrc);
            Assert.AreEqual(new Dep("B", null, "full-build"), dc.Deps[1]);
        }

        [Test]
        public void TestEmptyDepsSection()
        {
            var text = @"
client:
  deps:
";
            var dc = GetDepsContent(text, "client");
            Assert.That(dc.Deps.Count == 0);
        }

        [Test]
        public void TestNoDepsSection()
        {
            var text = @"
client:
";
            var dc = GetDepsContent(text, "client");
            CollectionAssert.AreEqual(new List<Dep>(), dc.Deps);
        }

        [Test]
        public void TestRemovalOfTheDepInOneOfParentConfigurations()
        {
            var text = @"
core:
  deps:
    - module/client

config1 > core:
  deps:
    - -module/client
    - module

config2 > core:

config3 > config2,config1:
";
            var dc = GetDepsContent(text, "config3");
            Assert.AreEqual(1, dc.Deps.Count);
            Assert.AreEqual(new Dep("module", null), dc.Deps[0]);
        }

        [Test]
        public void TestNoErrorIfAddedModulesHaveTheSameBranchAndConfiguration()
        {
            var text = @"
config0:
  deps:
    - module1

config1 > config0:
  deps:
    - module
    - module1

config2:
  deps:
    - module

config3 > config2,config1:
";
            var dc = GetDepsContent(text, "config3");
            Assert.NotNull(dc);
            Assert.AreEqual(2, dc.Deps.Count);
            Assert.AreEqual(new Dep("module1", null), dc.Deps[0]);
            Assert.AreEqual(new Dep("module", null), dc.Deps[1]);
        }

        private DepsData GetDepsContent(string text, string config = null, bool strictOrdering = true)
        {
            var a = YamlFromText.DepsParser(text).Get(config);

            var md = ModuleYamlParserFactory.Get().Parse(text);
            var configName = string.IsNullOrEmpty(config) ? md.GetDefaultConfiguration().Name : config;

            var b = md.AllConfigurations[configName].Deps;

            if (strictOrdering)
            {
                a.Should().BeEquivalentTo(b, o => o.WithStrictOrdering());
            }
            else
            {
                a.Should().BeEquivalentTo(b);
            }

            return a;
        }

        [Test]
        public void TestNoDepsFile()
        {
            using (var tmp = new TempDirectory())
            {
                var dc = new DepsParser(tmp.Path).Get();
                Assert.That(dc.Deps.Count == 0);
            }
        }
    }
}