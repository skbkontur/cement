using Common.YamlParsers;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    public class TestModuleYamlParser
    {
        [Test, Ignore]
        public void lalala()
        {
            var depSectionParser = new DepsSectionParser(new DepLineParser());
            var configLineParser = new ConfigLineParser();
            var buildSectionParser = new BuildSectionParser();
            var installSectionParser = new InstallSectionParser();

            var parser = new ModuleYamlParser(configLineParser, depSectionParser, buildSectionParser, installSectionParser);
            var text = Properties.ParsersTestData.ResourceManager.GetString("module.yaml.full");

            var result = parser.Parse("module", text);

            Assert.NotNull(result);
        }
    }
}