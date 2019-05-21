using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParserMainConfigBuildFiles
    {
        [TestCaseSource(nameof(MainConfigBuildFilesTestCase))]
        public void TestGetMainConfigBuildFiles(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.MainConfigBuildFiles;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] MainConfigBuildFilesTestCase =
        {
            new TestCaseData(@"
full-build:
",
                    new string[0])
                .SetName("Install section: MainConfigBuildFiles. No 'install' section."),

            new TestCaseData(@"
full-build:
  install:
",
                    new string[0])
                .SetName("Install section: MainConfigBuildFiles. Empty 'install' section."),

            new TestCaseData(@"
full-build:
  install:
    - file1
",
                    new [] { "file1" })
                .SetName("Install section: MainConfigBuildFiles. Single configuration, single build file."),

            new TestCaseData(@"
full-build:
  install:
    - file1
    - file2
    - file3
",
                    new [] { "file1", "file2", "file3" })
                .SetName("Install section: MainConfigBuildFiles. Single configuration, multiple build files."),

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
                .SetName("Install section: MainConfigBuildFiles. MainConfigBuildFiles are inhereted from 'default' section."),

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
                .SetName("Install section: MainConfigBuildFiles. MainConfigBuildFiles are not inhereted from parent configs."),

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
                .SetName("Install section: MainConfigBuildFiles. MainConfigBuildFiles are not duplicated."),

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
                .SetName("Install section: MainConfigBuildFiles. 'modules' are not leaked into MainConfigBuildFiles."),

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
                .SetName("Install section: MainConfigBuildFiles. 'nuget' do not leak into MainConfigBuildFiles."),

            new TestCaseData(@"
full-build:
  artifacts:
    - file1
  artefacts:
    - file2
",
                    new [] { "file1", "file2" })
                .SetName("Install section. MainConfigBuildFiles contain files from current configuration's artifacts."),

            new TestCaseData(@"
client:
  artifacts:
    - file3
  artefacts:
    - file4

full-build > client:
  artifacts:
    - file1
  artefacts:
    - file2",
                    new [] { "file1", "file2" })
                .SetName("Install section. MainConfigBuildFiles should not contain files from parent configuration's artifacts."),
        };
    }
}