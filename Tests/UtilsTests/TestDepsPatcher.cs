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
            string yamlA = @"
full-build:
  deps:
    - X
    - Y
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B
    - X
    - Y
";
            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NoDepsSection()
        {
            string yamlA = @"
full-build:
client:
  deps:
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B
client:
  deps:
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void SimpleTestWithConfig()
        {
            string yamlA = @"
full-build:
  deps:
    - X
    - Y
";
            string yamlB = @"
client:
full-bulid > client *default:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B/client
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "client")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void SimpleTestWithSpaces()
        {
            string yamlA = @"
full-build:
    deps:
      - X
      - Y
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
    deps:
      - B
      - X
      - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void SimpleTestWithTabs()
        {
            string yamlA = @"
full-build:
	deps:
		- X
		- Y
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
    deps:
        - B
        - X
        - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NeedReplaceToHugeConfig()
        {
            string yamlA = @"
full-build:
  deps:
    - B
    - X
    - Y
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NoNeedReplaceToSmallerConfig()
        {
            string yamlA = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B/full-build
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "client")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void AlreadyHasSameConfig()
        {
            string yamlA = @"
full-build:
  deps:
    - B/client
    - X
    - Y
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "client")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParent()
        {
            string yamlA = @"
client *default:
  deps:
    - B
full-build > client:
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - -B
    - B/full-build
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NoNeedReplaceToSmallerConfigFromParent()
        {
            string yamlA = @"
client *default:
  deps:
    - B/full-build
full-build > client:
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void TestBranchWithSlashes()
        {
            string yamlA = @"
full-build:
  deps:
    - X
    - Y
    - B@feature\/status
";
            string yamlB = @"
client:
full-bulid > client *default:
";
            string yamlAExpected = @"
full-build:
  deps:
    - X
    - Y
    - B@feature\/status
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "client")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParentWithBranch()
        {
            string yamlA = @"
client *default:
  deps:
    - B@branch
full-build > client:
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B@branch
full-build > client:
  deps:
    - -B@branch
    - B/full-build@branch
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NeedReplaceToHugeConfigFromParentWithBranchWithSlashes()
        {
            string yamlA = @"
client *default:
  deps:
    - B@feature\/status
full-build > client:
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B@feature\/status
full-build > client:
  deps:
    - -B@feature\/status
    - B/full-build@feature\/status
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void LcaTest()
        {
            string yamlA = @"
full-build:
  deps:
    - B/client1
";
            string yamlB = @"
client1 *default:
client2:
full-build > client1, client2:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B/full-build
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "client2")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void LcaTestWithNesting()
        {
            string yamlA = @"
client:
  deps:
    - B/client1
";
            string yamlB = @"
client1:
client2:
sdk > client1, client2:
full-build > sdk:
";
            string yamlAExpected = @"
client:
  deps:
    - B/sdk
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B", null, "client2")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NoLcaThrows()
        {
            string yamlA = @"
client:
  deps:
    - B/client1
";
            string yamlB = @"
client1:
client2:
";
            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.Throws<CementException>(() => Patch(dir, "client", new Dep("B", null, "client2")));
            }
        }

        [Test]
        public void OnOffReplace()
        {
            string yamlA = @"
client *default:
  deps:
    - B/client
full-build > client:
  deps:
    - -B/client
    - B/sdk
    - C
";
            string yamlB = @"
client *default:
sdk > client:
full-build > sdk:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B/client
full-build > client:
  deps:
    - -B/client
    - B/full-build
    - C
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void ChildHasntSameDep()
        {
            string yamlA = @"
client *default:
full-build > client:
  deps:
    - X
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
  deps:
    - X
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void ChildHasSmallerConfig()
        {
            string yamlA = @"
client *default:
full-build > client:
  deps:
    - X
    - B
    - Y
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B/full-build
full-build > client:
  deps:
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void ChildHasHugeConfig()
        {
            string yamlA = @"
client *default:
full-build > client:
  deps:
    - B/full-build
    - X
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - -B
    - B/full-build
    - X
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B", null, "client")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void ChildHasSameConfig()
        {
            string yamlA = @"
client *default:
full-build > client:
  deps:
    - B/client
    - X/full-build
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
client *default:
  deps:
    - B
full-build > client:
  deps:
    - X/full-build
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void ChildOnOff()
        {
            string yamlA = @"
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
            string yamlB = @"
client *default:
sdk > client:
full-build > sdk:
";
            string yamlAExpected = @"
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

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client", new Dep("B", null, "sdk")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NestingHell()
        {
            string yamlA = @"
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
            string yamlB = @"
client1:
client2 > client1:
client3 > client2:
client4 > client3:
";
            string yamlAExpected = @"
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

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client1", new Dep("B", null, "client4")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NestingHell2()
        {
            string yamlA = @"
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
            string yamlB = @"
client1:
client2 > client1:
client3 > client2:
client4 > client3:
";
            string yamlAExpected = @"
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

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "client1", new Dep("B", null, "client4")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void NeedReplaceToHugeConfigWithSrcType()
        {
            string yamlA = @"
full-build:
  deps:
    - B:
      type: src
    - X
    - Y
";
            string yamlB = @"
client *default:
full-build > client:
";
            string yamlAExpected = @"
full-build:
  deps:
    - B/full-build:
      type: src
    - X
    - Y
";

            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B", null, "full-build")),
                    Is.EqualTo(yamlAExpected));
            }
        }

        [Test]
        public void TestWithBuildSection()
        {
            string yamlA = @"
full-build:
  build:
    target: sicklistTransport.sln
    configuration: Release

  deps:
    - kanso
";
            string yamlB = @"
client *default:
full-bulid > client:
";
            string yamlAExpected = @"
full-build:
  build:
    target: sicklistTransport.sln
    configuration: Release

  deps:
    - B
    - kanso
";
            using (var dir = MakeTestFolder(yamlA, yamlB))
            {
                Assert.That(
                    Patch(dir, "full-build", new Dep("B")),
                    Is.EqualTo(yamlAExpected));
            }
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
