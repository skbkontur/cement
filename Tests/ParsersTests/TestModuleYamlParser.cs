using Common.YamlParsers;
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
            var yamlModuleDefaultsParser = new YamlModuleDefaultsParser(hooksSectionParser, depSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);

            var parser = new ModuleYamlParser(configLineParser, depSectionParser, buildSectionParser, installSectionParser, yamlModuleDefaultsParser);
            var text = Properties.ParsersTestData.ResourceManager.GetString("module.yaml.full");

            var result = parser.Parse(text);
            Assert.NotNull(result);
        }
    }
}