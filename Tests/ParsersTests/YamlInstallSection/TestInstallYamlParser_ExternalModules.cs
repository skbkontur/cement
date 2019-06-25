using FluentAssertions;
using NUnit.Framework;
using Tests.Helpers;

namespace Tests.ParsersTests.YamlInstallSection
{
    [TestFixture]
    public class TestInstallYamlParser_ExternalModules
    {
        [TestCaseSource(nameof(testCases))]
        public void TestGetExternalModules(string moduleYamlText, string[] expected)
        {
            var parser = YamlFromText.InstallParser(moduleYamlText);
            var parsed = parser.Get();

            var actual = parsed.ExternalModules;
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        private static TestCaseData[] testCases =
        {
            new TestCaseData(@"
full-build:
  install:
    - module Module1
",
                new[]
                {
                    "Module1"
                })
                .SetName("Install section: external modules. Single configuration, single module."),

            new TestCaseData(@"
full-build:
  artifacts:
    - module Module1
",
                    new string[0])
                .SetName("Install section. External modules cannot be placed in 'artifacts' section."),

            new TestCaseData(@"
full-build:
  install:
    - module Module1
    - module Module2
    - module Module3
",
                    new[]
                    {
                        "Module1",
                        "Module2",
                        "Module3",
                    })
                .SetName("Install section: external modules. Single configuration, multiple modules"),

            new TestCaseData(@"
config1:
  install:
    - module Module4
    - module Module5

full-build > config1:
  install:
    - module Module1
    - module Module2
    - module Module3
",
                    new[]
                    {
                        "Module1",
                        "Module2",
                        "Module3",
                        "Module4",
                        "Module5",
                    })
                .SetName("Install section: external modules. Two-leveled configuration, multiple modules"),

            new TestCaseData(@"
config1:
  install:
    - module Module4
    - module Module5

config2:
  install:
    - module Module6
    - module Module7

full-build > config1,config2:
  install:
    - module Module1
    - module Module2
    - module Module3
",
                    new[]
                    {
                        "Module1",
                        "Module2",
                        "Module3",
                        "Module4",
                        "Module5",
                        "Module6",
                        "Module7",
                    })
                .SetName("Install section: external modules. Two-leveled multiple-ancestors configuration, multiple modules"),

            new TestCaseData(@"
config0:
  install:
    - module Module8

config1 > config0:
  install:
    - module Module4
    - module Module5

config2:
  install:
    - module Module6
    - module Module7

full-build > config1,config2:
  install:
    - module Module1
    - module Module2
    - module Module3
",
                    new[]
                    {
                        "Module1",
                        "Module2",
                        "Module3",
                        "Module4",
                        "Module5",
                        "Module6",
                        "Module7",
                        "Module8",
                    })
                .SetName("Install section: external modules. Three-leveled multiple-ancestors configuration, multiple modules"),


            new TestCaseData(@"
default:
  install:
    - module Module9

config0:
  install:
    - module Module8

config1 > config0:
  install:
    - module Module4
    - module Module5

config2:
  install:
    - module Module6
    - module Module7

full-build > config1,config2:
  install:
    - module Module1
    - module Module2
    - module Module3
",
                    new[]
                    {
                        "Module1",
                        "Module2",
                        "Module3",
                        "Module4",
                        "Module5",
                        "Module6",
                        "Module7",
                        "Module8",
                        "Module9",
                    })
                .SetName("Install section: external modules. Three-leveled multiple-ancestors configuration with 'default' section, multiple external modules"),

            new TestCaseData(@"
default:
  install:
    - module DuplicatedModule

config0:
  install:
    - module DuplicatedModule

config1 > config0:
  install:
    - module DuplicatedModule

config2:
  install:
    - module DuplicatedModule

full-build > config1,config2:
  install:
    - module DuplicatedModule
",
                    new[]
                    {
                        "DuplicatedModule",
                        "DuplicatedModule",
                        "DuplicatedModule",
                        "DuplicatedModule",
                    })
                .SetName("Install section: external modules. Three-leveled multiple-ancestors configuration with 'default' section. External modules do duplicate."),
        };
    }
}