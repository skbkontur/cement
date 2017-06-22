using System.Collections.Generic;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests
{
    [TestFixture]
    public class TestInstallYamlParser
    {
        [Test]
        public void TestEmptyInstall()
        {
            var text = @"
full-build:
	install:
";
            var result = YamlFromText.InstallParser(text).Get();
            Assert.AreEqual(new List<string>(), result.BuildFiles);
        }

        [Test]
        public void TestEmptySpec()
        {
            var text = @"
default:
full-build:
";
            var result = YamlFromText.InstallParser(text).Get();
            Assert.AreEqual(new List<string>(), result.BuildFiles);
        }

        [Test]
        public void TestGetInstallFromDefault()
        {
            var text = @"
default:
    install:
        - A
full-build:
";
            var result = YamlFromText.InstallParser(text).Get();
            Assert.AreEqual(1, result.BuildFiles.Count);
            Assert.AreEqual("A", result.BuildFiles[0]);
        }

        [Test]
        public void TestGetInstallFromDefaultAndConfig()
        {
            var text = @"
default:
    install:
        - A
full-build:
    install:
        - B
";
            var result = YamlFromText.InstallParser(text).Get().BuildFiles;
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("B", result[0]);
            Assert.AreEqual("A", result[1]);
        }

        [Test]
        public void TestGetInstallWithoutDefault()
        {
            var text = @"
full-build:
    install:
        - A
";
            var result = YamlFromText.InstallParser(text).Get().BuildFiles;
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual("A", result[0]);
        }

        [Test]
        public void TestGetInstallNesting()
        {
            var text = @"
client:
    install:
        - A
full-build > client:
    install:
        - B
";
            var result = YamlFromText.InstallParser(text).Get().BuildFiles;
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("B", result[0]);
            Assert.AreEqual("A", result[1]);
        }

        [Test]
        public void TestLongNesting()
        {
            var text = @"
default:
client:
    install:
        - B
sdk > client:
    install:
        - C
full-build > sdk:
    install:
        - D
";
            var result = YamlFromText.InstallParser(text).Get().BuildFiles.ToArray();
            Assert.AreEqual(new[] {"D", "C", "B"}, result);
        }

        [Test]
        public void TestGetArtifactsFromDefault()
        {
            var text = @"
default:
	artifacts:
		- A
full-build:
";
            var result = YamlFromText.InstallParser(text).Get();
            Assert.AreEqual(1, result.Artifacts.Count);
            Assert.AreEqual("A", result.Artifacts[0]);
        }

        [Test]
        public void TestGetArtifactsWithAlias()
        {
            var text = @"
full-build:
	artifacts:
		- A
    artefacts:
        - B
";
            var result = YamlFromText.InstallParser(text).Get();
            Assert.AreEqual(2, result.Artifacts.Count);
            Assert.AreEqual("A", result.Artifacts[0]);
            Assert.AreEqual("B", result.Artifacts[1]);
        }

        [Test]
        public void TestGetArtifactsNesting()
        {
            var text = @"
client:
    install:
        - A
	artifacts:
		- A1
		- A2
full-build > client:
    install:
        - B
	artifacts:
		- B1
";
            var result = YamlFromText.InstallParser(text).Get();
            var builds = result.BuildFiles;
            Assert.AreEqual(2, builds.Count);
            Assert.AreEqual("B", builds[0]);
            Assert.AreEqual("A", builds[1]);

            var arifacts = result.Artifacts;
            CollectionAssert.AreEquivalent(new[] {"A1", "A2", "B1", "A", "B"}, arifacts);
        }
    }
}
