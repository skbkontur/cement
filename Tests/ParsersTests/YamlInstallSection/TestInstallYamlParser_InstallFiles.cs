using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParser_InstallFiles
    {
        [TestCaseSource(nameof(testCases))]
        public void TestGetBuildFiles(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.InstallFiles;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] testCases =
        {
            new TestCaseData(@"
full-build:
",
                    new string[0])
                .SetName("Install section: install files. Single configuration, no install section"),

            new TestCaseData(@"
full-build:
  install:
",
                    new string[0])
                .SetName("Install section: install files. Single configuration, empty install section"),

            new TestCaseData(@"
full-build:
  install:
    - file1
",
                    new[]
                    {
                        "file1"
                    })
                .SetName("Install section: install files. Single configuration, single build file"),

            new TestCaseData(@"
full-build:
  install:
    - file1
    - file2
    - file3
",
                    new[]
                    {
                        "file1",
                        "file2",
                        "file3",
                    })
                .SetName("Install section: install files. Single configuration, multiple install files"),

            new TestCaseData(@"
config1:
  install:
    - file4
    - file5

full-build > config1:
  install:
    - file1
    - file2
    - file3
",
                    new[]
                    {
                        "file1",
                        "file2",
                        "file3",
                        "file4",
                        "file5",
                    })
                .SetName("Install section: install files. Two-leveled configuration configuration, multiple install files"),

            new TestCaseData(@"
config1:
  install:
    - file4
    - file5

config2:
  install:
    - file6
    - file7

full-build > config1,config2:
  install:
    - file1
    - file2
    - file3
",
                    new[]
                    {
                        "file1",
                        "file2",
                        "file3",
                        "file4",
                        "file5",
                        "file6",
                        "file7",
                    })
                .SetName("Install section: install files. Two-leveled multiple-ancestors configuration configuration, multiple install files"),


            new TestCaseData(@"
config0:
  install:
    - file8
    - file9

config1 > config0:
  install:
    - file4
    - file5

config2:
  install:
    - file6
    - file7

full-build > config1,config2:
  install:
    - file1
    - file2
    - file3
",
                    new[]
                    {
                        "file1",
                        "file2",
                        "file3",
                        "file4",
                        "file5",
                        "file6",
                        "file7",
                        "file8",
                        "file9",
                    })
                .SetName("Install section: install files. Three-leveled multiple-ancestors configuration configuration, multiple install files"),

            new TestCaseData(@"
default:
  install:
    - file10

config0:
  install:
    - file8
    - file9

config1 > config0:
  install:
    - file4
    - file5

config2:
  install:
    - file6
    - file7

full-build > config1,config2:
  install:
    - file1
    - file2
    - file3
",
                    new[]
                    {
                        "file1",
                        "file2",
                        "file3",
                        "file4",
                        "file5",
                        "file6",
                        "file7",
                        "file8",
                        "file9",
                        "file10",
                    })
                .SetName("Install section: install files. Three-leveled multiple-ancestors configuration configuration with 'default' section, multiple install files"),

            new TestCaseData(@"
default:
  install:
    - DuplicatedFile

config0:
  install:
    - DuplicatedFile

config1 > config0:
  install:
    - DuplicatedFile

config2:
  install:
    - DuplicatedFile

full-build > config1,config2:
  install:
    - DuplicatedFile
",
                    new[] { "DuplicatedFile" })
                .SetName("Install section: install files. Three-leveled multiple-ancestors configuration configuration with 'default' section, multiple install files. InstallFiles are not duplicated."),


            new TestCaseData(@"
full-build:
  install:
    - nuget SomeNuget
    - module SomeModule
",
                    new string[0] )
                .SetName("Install section: install files. Nuget and external modules are not considered install files."),

            new TestCaseData(@"
full-build:
  install:
    - file1

  artifacts:
    - file1
",
                    new[] { "file1" })
                .SetName("Install section: install files. InstallFiles collection are not affected by duplicate artifacts (single configuration)."),

            new TestCaseData(@"
full-build:
  install:
    - file1

  artifacts:
    - file2
",
                    new[] { "file1" })
                .SetName("Install section: install files. InstallFiles does not contain artifacts from current configuration."),

            new TestCaseData(@"
client:
  artifacts:
    - file2

full-build > client:
  install:
    - file1
",
                    new[] { "file1" })
                .SetName("Install section: install files. InstallFiles does not contain artifacts from parent configuration."),
        };
    }
}