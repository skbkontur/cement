using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParser_CurrentConfigurationInstallFiles
    {
        [TestCaseSource(nameof(testCases))]
        public void TestGetMainConfigBuildFiles(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.CurrentConfigurationInstallFiles;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] testCases =
        {
            new TestCaseData(@"
full-build:
",
                    new string[0])
                .SetName("Install section: CurrentConfigurationInstallFiles. No 'install' section."),

            new TestCaseData(@"
full-build:
  install:
",
                    new string[0])
                .SetName("Install section: CurrentConfigurationInstallFiles. Empty 'install' section."),

            new TestCaseData(@"
full-build:
  install:
    - file1
",
                    new [] { "file1" })
                .SetName("Install section: CurrentConfigurationInstallFiles. Single configuration, single build file."),

            new TestCaseData(@"
full-build:
  install:
    - file1
    - file2
    - file3
",
                    new [] { "file1", "file2", "file3" })
                .SetName("Install section: CurrentConfigurationInstallFiles. Single configuration, multiple install files."),

            new TestCaseData(@"
default:
  install:
    - file4
    - file5

full-build:
  install:
    - file1
    - file2
    - file3
",
                    new [] { "file1", "file2", "file3", "file4", "file5" })
                .SetName("Install section: CurrentConfigurationInstallFiles. CurrentConfigurationInstallFiles are inhereted from 'default' section."),

            new TestCaseData(@"
default:
  install:
    - file4
    - file5

config1:
  install:
    - file6

full-build > config1:
  install:
    - file1
    - file2
    - file3
",
                    new [] { "file1", "file2", "file3", "file4", "file5" })
                .SetName("Install section: CurrentConfigurationInstallFiles. CurrentConfigurationInstallFiles are not inhereted from parent configs."),

            new TestCaseData(@"
default:
  install:
    - duplicated
    - duplicated

config1:
  install:
    - duplicated

full-build > config1:
  install:
    - duplicated
    - duplicated
    - duplicated
",
                    new [] { "duplicated" })
                .SetName("Install section: CurrentConfigurationInstallFiles. CurrentConfigurationInstallFiles are not duplicated."),

            new TestCaseData(@"
default:
  install:
    - file4
    - file5
    - module SomeModule1

config1:
  install:
    - file6
    - module SomeModule2

full-build > config1:
  install:
    - file1
    - file2
    - file3
    - module SomeModule3
",
                    new [] { "file1", "file2", "file3", "file4", "file5" })
                .SetName("Install section: CurrentConfigurationInstallFiles. 'modules' are not leaked into CurrentConfigurationInstallFiles."),

            new TestCaseData(@"
default:
  install:
    - file4
    - file5
    - nuget SomeNuget2

config1:
  install:
    - file6
    - nuget SomeNuget3

full-build > config1:
  install:
    - file1
    - file2
    - file3
    - nuget SomeNuget1
",
                    new []
                    {
                        "file1",
                        "file2",
                        "file3",
                        "file4",
                        "file5",
                    })
                .SetName("Install section: CurrentConfigurationInstallFiles. 'nuget' do not leak into CurrentConfigurationInstallFiles."),

            new TestCaseData(@"
full-build:
  artifacts:
    - file1
",
                    new [] { "file1" })
                .SetName("Install section. CurrentConfigurationInstallFiles contain files from current configuration's artifacts."),

            new TestCaseData(@"
client:
  artifacts:
    - file2

full-build > client:
  artifacts:
    - file1",
                    new [] { "file1" })
                .SetName("Install section. CurrentConfigurationInstallFiles should not contain files from parent configuration's artifacts."),
        };
    }
}