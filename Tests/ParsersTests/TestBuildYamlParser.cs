using Common;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestBuildYamlParser
    {
        [Test]
        public void TestEmtyInstall()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
full-build:
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("msbuild", result[0].Tool.Name);
        }

        [Test]
        public void TestGetToolDefault()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
full-build:
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("msbuild", result[0].Tool.Name);
        }

        [Test]
        public void TestNonDefaultTool()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
        tool: ant
full-build:
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("ant", result[0].Tool.Name);
        }

        [Test]
        public void TestRedefinedTool()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
        tool: ant
full-build:
    build:
        tool: gcc
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("gcc", result[0].Tool.Name);
        }

        [Test]
        public void TestRedefinedTarget()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
        tool: ant
full-build:
    build:
        target: b
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("b", result[0].Target);
        }

        [Test]
        public void TestRedefinedConfiguration()
        {
            var text = @"
default:
    build:
        target: a
        configuration: c
        tool: ant
full-build:
    build:
        configuration: b
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("b", result[0].Configuration);
        }

        [Test]
        public void TestNoThrowsWithNoTarget()
        {
            var text = @"
full-build:
    build:
        tool: gcc
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual("gcc", result[0].Tool.Name);
            Assert.AreEqual("", result[0].Target);
        }

        [Test]
        public void TestWithParameters()
        {
            var text = @"
default:
    build:
        target: a
        configuration: r
        parameters:
            - a:b
            - /c:d
            - e
full-build:
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(new[] {"a:b", "/c:d", "e"}, result[0].Parameters.ToArray());
        }

        [Test]
        public void TestWithOneLineParameters()
        {
            var text = @"
full-build:
	build:
        target: a
        configuration: r
        parameters: asdf
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(new[] {"asdf"}, result[0].Parameters.ToArray());
        }

        [Test]
        public void TestWithOneLineParametersAndQuotes()
        {
            var text = @"
default:
    build:
        target: a
        configuration: r
        parameters: ""asdf \""q\""wer""
full-build:
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual(new[] {"asdf \"q\"wer"}, result[0].Parameters.ToArray());
        }

        [Test]
        public void TestTargetAndConfiguration()
        {
            var text = @"
default:
    build:
        target: Kanso.sln
        configuration: Debug
client:
    build:
        target: Kanso.Client.sln
        configuration: Client
full-build > client:
    build:
        configuration: Release
";
            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("Release", result[0].Configuration);
            Assert.AreEqual("Kanso.sln", result[0].Target);
        }

        [Test]
        public void TestTabsInYaml()
        {
            var text = @"
default:
	build:
		target: a
		configuration: c
full-build:
";
            YamlFromText.BuildParser(text).Get();
        }

        [Test]
        public void TestMsBuildToolVersionDictStyle()
        {
            var text = @"
default:
  build:
    target: a
    configuration: c
    tool:
      name: msbuild
      version: ""14.0""
full-build:
";
            var result = YamlFromText.BuildParser(text).Get();
            Assert.AreEqual(result.Count, 1);
            Assert.AreEqual("msbuild", result[0].Tool.Name);
            Assert.AreEqual("14.0", result[0].Tool.Version);
        }

        [Test]
        public void TestMsBuildToolVersionOldStyleThrows()
        {
            var text = @"
default:
  build:
    target: a
    configuration: c
    tool:
      - name: msbuild
      - version: ""14.0""
full-build:
";
            Assert.Throws<BadYamlException>(() => YamlFromText.BuildParser(text).Get());
        }

        [Test]
        public void TestEmptyBuildToolThrows()
        {
            var text = @"
default:
  build:
    target: a
    configuration: c
    tool:
full-build:
";
            Assert.Throws<BadYamlException>(() => YamlFromText.BuildParser(text).Get());
        }

        [Test]
        public void TestMultiBuildSection()
        {
            var text = @"
default:
    build:
      configuration: def
client:
    build:
      - name: A
        target: a
      - name: B
        target: b
        configuration: debug
";
            var result = YamlFromText.BuildParser(text).Get("client");
            Assert.AreEqual(result.Count, 2);
            Assert.AreEqual(result[0].Configuration, "def");
            Assert.AreEqual(result[1].Configuration, "debug");
            Assert.AreEqual(result[0].Name, "A");
            Assert.AreEqual(result[1].Name, "B");
        }

        [Test]
        public void TestWithoutConfiguration()
        {
            var text = @"
full-build:
  build:
    target: Logging.sln";

            Assert.Throws<BadYamlException>(() => YamlFromText.BuildParser(text).Get("full-build"));
        }

        [Test]
        public void TestWhenConfigurationDoesNotNecessary()
        {
            var text = @"
full-build:
  deps:
  build:
    tool: cmd
    parameters: /C
    target: build.cmd";

            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("cmd", result[0].Tool.Name);
            CollectionAssert.AreEqual(new[] {"/C"}, result[0].Parameters);
            Assert.AreEqual("build.cmd", result[0].Target);
            Assert.AreEqual(null, result[0].Configuration);
        }

        [Test]
        public void TestWithOnlyTarget()
        {
            var text = @"
full-build:
  deps:
  build:
    target: build.xproj";

            var result = YamlFromText.BuildParser(text).Get("full-build");
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(null, result[0].Configuration);
        }
    }
}
