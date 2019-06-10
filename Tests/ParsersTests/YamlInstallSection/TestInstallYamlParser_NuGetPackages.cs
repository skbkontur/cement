using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParser_NuGetPackages
    {
        [TestCaseSource(nameof(testCases))]
        public void TestGetNuGet(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.NuGetPackages;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] testCases =
        {
            new TestCaseData(@"
full-build:
  install:
    - nuget NugetDep1
",
                new[]
                {
                    "NugetDep1"
                })
                .SetName("Install section: nuget, single configuration, single nuget"),

            new TestCaseData(@"
full-build:
  install:
    - current.dll
    - nuget NugetDep1
    - nuget NugetDep2
    - nuget NugetDep3
",
                    new[]
                    {
                        "NugetDep1",
                        "NugetDep2",
                        "NugetDep3",
                    })
                .SetName("Install section: nuget, single configuration, multiple nugets"),

            new TestCaseData(@"
config1:
  install:
    - nuget NugetDep4
    - nuget NugetDep5

full-build > config1:
  install:
    - current.dll
    - nuget NugetDep1
    - nuget NugetDep2
    - nuget NugetDep3
",
                    new[]
                    {
                        "NugetDep1",
                        "NugetDep2",
                        "NugetDep3",
                        "NugetDep4",
                        "NugetDep5",
                    })
                .SetName("Install section: nuget, two-leveled configuration, multiple nugets"),

            new TestCaseData(@"
config0:
  install:
    - nuget NugetDep8

config1 > config0:
  install:
    - nuget NugetDep4
    - nuget NugetDep5

config2:
  install:
    - nuget NugetDep6
    - nuget NugetDep7

full-build > config1,config2:
  install:
    - current.dll
    - nuget NugetDep1
    - nuget NugetDep2
    - nuget NugetDep3
",
                    new[]
                    {
                        "NugetDep1",
                        "NugetDep2",
                        "NugetDep3",
                        "NugetDep4",
                        "NugetDep5",
                        "NugetDep6",
                        "NugetDep7",
                        "NugetDep8",
                    })
                .SetName("Install section: nuget, three-leveled multiple-ancestors configuration, multiple nugets"),

            new TestCaseData(@"
default:
  install:
    - nuget NugetDep9

config0:
  install:
    - nuget NugetDep8

config1 > config0:
  install:
    - nuget NugetDep4
    - nuget NugetDep5

config2:
  install:
    - nuget NugetDep6
    - nuget NugetDep7

full-build > config1,config2:
  install:
    - current.dll
    - nuget NugetDep1
    - nuget NugetDep2
    - nuget NugetDep3
",
                    new[]
                    {
                        "NugetDep1",
                        "NugetDep2",
                        "NugetDep3",
                        "NugetDep4",
                        "NugetDep5",
                        "NugetDep6",
                        "NugetDep7",
                        "NugetDep8",
                        "NugetDep9",
                    })
                .SetName("Install section: nuget, three-leveled multiple-ancestors configuration with 'default' section, multiple nugets"),

            new TestCaseData(@"
default:
  install:
    - nuget DuplicateNuget

config0:
  install:
    - nuget DuplicateNuget

config1 > config0:
  install:
    - nuget DuplicateNuget

config2:
  install:
    - nuget DuplicateNuget

full-build > config1,config2:
  install:
    - nuget DuplicateNuget
",
                    new[]
                    {
                        "DuplicateNuget",
                        "DuplicateNuget",
                        "DuplicateNuget",
                        "DuplicateNuget",
                    })
                .SetName("Install section: nuget, two-leveled multiple-ancestors configuration with 'default' section. Nugets do duplicate."),
        };
    }
}