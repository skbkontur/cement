using Common.YamlParsers.V2.Factories;
using NUnit.Framework;

namespace Tests.ParsersTests
{
    public class TestModuleYamlParser
    {
        [Test, Ignore]
        public void Test()
        {
            var parser = ModuleYamlParserInstance.Get();
            var text = Properties.ParsersTestData.ResourceManager.GetString("module.yaml.full");
            var result = parser.Parse(text);
            Assert.NotNull(result);
        }
    }
}