using System.Collections.Generic;
using Common;
using Common.YamlParsers;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2;
using FluentAssertions;
using NUnit.Framework;
using SharpYaml.Serialization;

namespace Tests.ParsersTests.V2
{
    [TestFixture]
    public class TestModuleYamlDefaultsParser
    {
        [TestCaseSource(nameof(Source))]
        public void TestParseDefaultSection(string input, ModuleDefaults expected)
        {
            var parser = GetParser();
            var content = GetDefaultSection(input);
            var actual = parser.Parse(content);
            actual.Should().BeEquivalentTo(expected, o => o.WithStrictOrdering());
        }

        private static TestCaseData[] Source =
        {
            new TestCaseData(@"default:
  settings:
    type: content
  hooks:
    - hooks/pre-commit
    - pre-commit.cement
  deps:
    - force: some_branch
    - module1
    - module2
  build:
    tool:
      name: sometool
    target: solution.sln
  install:
    - file1.dll
    - nuget nuget1
    - module externalModule
  artifacts:
    - file2.dll
    - file3.dll
",
                new ModuleDefaults()
                {
                    SettingsSection = new ModuleSettings()
                    {
                        IsContentModule = true
                    },
                    HooksSection = new []
                    {
                        "hooks/pre-commit",
                        "pre-commit.cement",
                    },
                    DepsSection = new DepsSection(new [] {"some_branch"}, new DepSectionItem[]
                    {
                        new DepSectionItem(new Dep("module1", null, null)),
                        new DepSectionItem(new Dep("module2", null, null)),
                    }),
                    BuildSection = new BuildData("solution.sln", null, new Tool("sometool"), new List<string>(), string.Empty),
                    InstallSection = new InstallData()
                    {
                        InstallFiles =  { "file1.dll" },
                        CurrentConfigurationInstallFiles = { "file1.dll", "file2.dll", "file3.dll" },
                        Artifacts = { "file1.dll", "file2.dll", "file3.dll" },
                        ExternalModules = { "externalModule" },
                        NuGetPackages = { "nuget1" },
                    }
                }).SetName("Complex defaults section"),
        };

        private Dictionary<object, object> GetDefaultSection(string text)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>) serializer.Deserialize(text);

            return  (Dictionary<object, object>) yaml["default"];
        }

        private ModuleDefaultsParser GetParser()
        {
            var depSectionItemParser = new DepSectionItemParser();
            var depsSectionParser = new DepsSectionParser(depSectionItemParser);
            var installSectionParser = new InstallSectionParser();
            var buildSectionParser = new BuildSectionParser();
            var hooksSectionParser = new HooksSectionParser();
            var settingsSectionParser = new SettingsSectionParser();

            var moduleDefaultsParser = new ModuleDefaultsParser(hooksSectionParser, depsSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);
            return moduleDefaultsParser;
        }
    }
}