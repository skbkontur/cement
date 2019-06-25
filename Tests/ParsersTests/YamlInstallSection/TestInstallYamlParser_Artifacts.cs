using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParser_Artifacts
    {
        [TestCaseSource(nameof(testCases))]
        public void TestGetArtifacts(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.Artifacts;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] testCases =
        {
            new TestCaseData(@"
full-build:
  artifacts:
    - a1
    - a2",
                new[] { "a1", "a2"})
                .SetName("Install section: artifacts. Single configuration, multiple artifacts"),

            new TestCaseData(@"
config1:
  artifacts:
    - a3
    - a4

full-build > config1:
  artifacts:
    - a1
    - a2",
                    new[] { "a1", "a2", "a3", "a4"})
                .SetName("Install section: artifacts. Two-leveled configuration, multiple artifacts"),

            new TestCaseData(@"
config1:
  artifacts:
    - a3
    - a4

config2:
  artifacts:
    - a5
    - a6

full-build > config1,config2:
  artifacts:
    - a1
    - a2",
                    new[] { "a1", "a2", "a3", "a4", "a5", "a6"})
                .SetName("Install section: artifacts. Two-leveled multiple-ancestors configuration, multiple artifacts"),

            new TestCaseData(@"
config0:
  artifacts:
    - a7
    - a8

config1 > config0:
  artifacts:
    - a3
    - a4

config2:
  artifacts:
    - a5
    - a6

full-build > config1,config2:
  artifacts:
    - a1
    - a2",
                    new[] { "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8"})
                .SetName("Install section: artifacts. three-leveled multiple-ancestors configuration, multiple artifacts"),

            new TestCaseData(@"
default:
  artifacts:
    - a9
    - a10

config0:
  artifacts:
    - a7
    - a8

config1 > config0:
  artifacts:
    - a3
    - a4

config2:
  artifacts:
    - a5
    - a6

full-build > config1,config2:
  artifacts:
    - a1
    - a2",
                    new[] { "a1", "a2", "a3", "a4", "a5", "a6", "a7", "a8", "a9", "a10"})
                .SetName("Install section: artifacts. three-leveled multiple-ancestors configuration with default section, multiple artifacts"),


            new TestCaseData(@"
default:
  artifacts:
    - DuplicateArtifact

config0:
  artifacts:
    - DuplicateArtifact

config1 > config0:
  artifacts:
    - DuplicateArtifact

config2:
  artifacts:
    - DuplicateArtifact

full-build > config1,config2:
  artifacts:
    - DuplicateArtifact",
                    new[] { "DuplicateArtifact"})
                .SetName("Install section: artifacts. three-leveled multiple-ancestors configuration with default section. Artifacts do not duplicate."),

            new TestCaseData(@"
full-build:
  install:
    - file1
    - module SomeModule
    - nuget SomeNuget

  artifacts:
    - a1",
                    new[] { "file1", "a1" })
                .SetName("Install section: artifacts. Install files from install section are added to artifacts (ignoring 'module' and 'nuget' directive)."),

            new TestCaseData(@"
full-build:
  artifacts:
    - a1
    - nuget SomeNuget1
",
                    new[] { "a1" })
                .SetName("Install section: artifacts. Nuget packages do not leak into artifact section"),

            new TestCaseData(@"
full-build:
  artifacts:
    - a1
    - module SomeModule1
",
                    new[] { "a1" })
                .SetName("Install section: artifacts. External modules do not leak into artifact section"),
        };
    }
}