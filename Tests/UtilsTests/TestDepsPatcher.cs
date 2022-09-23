using System.IO;
using Common;
using NUnit.Framework;

namespace Tests.UtilsTests
{
    public class TestDepsPatcher
    {
        [Test]
        public void SimpleTest()
        {
            var yamlA = @"
full-build:
  deps:
    - X
    - Y
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B
    - X
    - Y
";
            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NoDepsSection()
        {
            var yamlA = @"
full-build:
client:
  deps:
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B
client:
  deps:
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void SimpleTestWithConfig()
        {
            var yamlA = @"
full-build:
  deps:
    - X
    - Y
";
            var yamlB = @"
client:
full-bulid > client *default:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B/client
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "client")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void SimpleTestWithSpaces()
        {
            var yamlA = @"
full-build:
    deps:
      - X
      - Y
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
    deps:
      - B
      - X
      - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void SimpleTestWithTabs()
        {
            var yamlA = @"
full-build:
	deps:
		- X
		- Y
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
    deps:
        - B
        - X
        - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NeedReplaceToHugeConfig()
        {
            var yamlA = @"
full-build:
  deps:
    - B
    - X
    - Y
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NoNeedReplaceToSmallerConfig()
        {
            var yamlA = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "client")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void AlreadyHasSameConfig()
        {
            var yamlA = @"
full-build:
  deps:
    - B/client
    - X
    - Y
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "client")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParent()
        {
            var yamlA = @"
client *default:
  deps:
    - B
full-build > client:
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - -B
    - B/full-build
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NoNeedReplaceToSmallerConfigFromParent()
        {
            var yamlA = @"
client *default:
  deps:
    - B/full-build
full-build > client:
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void TestBranchWithSlashes()
        {
            var yamlA = @"
full-build:
  deps:
    - X
    - Y
    - B@feature\/status
";
            var yamlB = @"
client:
full-bulid > client *default:
";
            var yamlAExpected = @"
full-build:
  deps:
    - X
    - Y
    - B@feature\/status
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "client")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParentWithBranch()
        {
            var yamlA = @"
client *default:
  deps:
    - B@branch
full-build > client:
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B@branch
full-build > client:
  deps:
    - -B@branch
    - B/full-build@branch
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParentWithBranchWithSlashes()
        {
            var yamlA = @"
client *default:
  deps:
    - B@feature\/status
full-build > client:
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B@feature\/status
full-build > client:
  deps:
    - -B@feature\/status
    - B/full-build@feature\/status
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void LcaTest()
        {
            var yamlA = @"
full-build:
  deps:
    - B/client1
";
            var yamlB = @"
client1 *default:
client2:
full-build > client1, client2:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B/full-build
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "client2")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void LcaTestWithNesting()
        {
            var yamlA = @"
client:
  deps:
    - B/client1
";
            var yamlB = @"
client1:
client2:
sdk > client1, client2:
full-build > sdk:
";
            var yamlAExpected = @"
client:
  deps:
    - B/sdk
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B", null, "client2")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NoLcaThrows()
        {
            var yamlA = @"
client:
  deps:
    - B/client1
";
            var yamlB = @"
client1:
client2:
";
            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.Throws<CementException>(() => Patch(dir, "client", new Dep("B", null, "client2")));
        }

        [Test]
        public void OnOffReplace()
        {
            var yamlA = @"
client *default:
  deps:
    - B/client
full-build > client:
  deps:
    - -B/client
    - B/sdk
    - C
";
            var yamlB = @"
client *default:
sdk > client:
full-build > sdk:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B/client
full-build > client:
  deps:
    - -B/client
    - B/full-build
    - C
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void ChildHasntSameDep()
        {
            var yamlA = @"
client *default:
full-build > client:
  deps:
    - X
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
  deps:
    - X
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void ChildHasSmallerConfig()
        {
            var yamlA = @"
client *default:
full-build > client:
  deps:
    - X
    - B
    - Y
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
  deps:
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void ChildHasHugeConfig()
        {
            var yamlA = @"
client *default:
full-build > client:
  deps:
    - B/full-build
    - X
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - -B
    - B/full-build
    - X
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B", null, "client")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void ChildHasSameConfig()
        {
            var yamlA = @"
client *default:
full-build > client:
  deps:
    - B/client
    - X/full-build
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - X/full-build
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void ChildOnOff()
        {
            var yamlA = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - X
    - -B
    - B/full-build
    - Y
";
            var yamlB = @"
client *default:
sdk > client:
full-build > sdk:
";
            var yamlAExpected = @"
client *default:
  deps:
    - B/sdk
full-build > client:
  deps:
    - -B/sdk
    - B/full-build
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client", new Dep("B", null, "sdk")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NestingHell()
        {
            var yamlA = @"
client1 *default:
client2 > client1:
  deps:
    - B/client1
client3 > client1, client2:
  deps:
    - B/client2
client4 > client3:
  deps:
    - B/client3
";
            var yamlB = @"
client1:
client2 > client1:
client3 > client2:
client4 > client3:
";
            var yamlAExpected = @"
client1 *default:
  deps:
    - B/client4
client2 > client1:
  deps:
client3 > client1, client2:
  deps:
client4 > client3:
  deps:
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client1", new Dep("B", null, "client4")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NestingHell2()
        {
            var yamlA = @"
client1 *default:
client2 > client1:
  deps:
    - B/client1
client3 > client1, client2:
  deps:
    - B/client4
client4 > client3:
  deps:
    - B/client4
";
            var yamlB = @"
client1:
client2 > client1:
client3 > client2:
client4 > client3:
";
            var yamlAExpected = @"
client1 *default:
  deps:
    - B/client4
client2 > client1:
  deps:
client3 > client1, client2:
  deps:
client4 > client3:
  deps:
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "client1", new Dep("B", null, "client4")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void NeedReplaceToHugeConfigWithSrcType()
        {
            var yamlA = @"
full-build:
  deps:
    - B:
      type: src
    - X
    - Y
";
            var yamlB = @"
client *default:
full-build > client:
";
            var yamlAExpected = @"
full-build:
  deps:
    - B/full-build:
      type: src
    - X
    - Y
";

            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B", null, "full-build")),
                Is.EqualTo(yamlAExpected));
        }

        [Test]
        public void TestWithBuildSection()
        {
            var yamlA = @"
full-build:
  build:
    target: sicklistTransport.sln
    configuration: Release

  deps:
    - kanso
";
            var yamlB = @"
client *default:
full-bulid > client:
";
            var yamlAExpected = @"
full-build:
  build:
    target: sicklistTransport.sln
    configuration: Release

  deps:
    - B
    - kanso
";
            using var dir = MakeTestFolder(yamlA, yamlB);
            Assert.That(
                Patch(dir, "full-build", new Dep("B")),
                Is.EqualTo(yamlAExpected));
        }

        private TempDirectory MakeTestFolder(string yamlA, string yamlB)
        {
            var dir = new TempDirectory();
            Directory.CreateDirectory(Path.Combine(dir.Path, "A"));
            File.WriteAllText(Path.Combine(dir.Path, "A", Helper.YamlSpecFile), yamlA);
            Directory.CreateDirectory(Path.Combine(dir.Path, "B"));
            File.WriteAllText(Path.Combine(dir.Path, "B", Helper.YamlSpecFile), yamlB);
            return dir;
        }

        private string Patch(TempDirectory workspace, string configuration, Dep dep)
        {
            new DepsPatcher(workspace.Path, "A", dep).Patch(configuration);
            var result = File.ReadAllText(Path.Combine(workspace.Path, "A", Helper.YamlSpecFile));
            return result;
        }
    }
}
