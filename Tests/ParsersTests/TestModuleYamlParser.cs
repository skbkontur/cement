using Common.YamlParsers;
using Common.YamlParsers.V2;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    public class TestModuleYamlParser
    {
        [Test, Ignore]
        public void Test()
        {
            var depSectionParser = new DepsSectionParser(new DepLineParser());
            var configLineParser = new ConfigLineParser();
            var buildSectionParser = new BuildSectionParser();
            var installSectionParser = new InstallSectionParser();

            var hooksSectionParser = new HooksSectionParser();
            var settingsSectionParser = new SettingsSectionParser();
            var yamlModuleDefaultsParser = new ModuleYamlDefaultsParser(hooksSectionParser, depSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);

            var yamlConfigurationParser = new ModuleYamlConfigurationParser(installSectionParser, depSectionParser, buildSectionParser);

            var parser = new ModuleYamlParser(configLineParser, yamlModuleDefaultsParser, yamlConfigurationParser);
            var text = Properties.ParsersTestData.ResourceManager.GetString("module.yaml.full");

            var result = parser.Parse(text);

            Assert.NotNull(result);
        }
    }
}