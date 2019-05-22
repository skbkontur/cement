using System;

namespace Common.YamlParsers.V2.Factories
{
    public static class ModuleYamlParserInstance
    {
        private static readonly Lazy<ModuleYamlParser> instance = new Lazy<ModuleYamlParser>(Create);

        public static ModuleYamlParser Get()
        {
            return instance.Value;
        }

        private static ModuleYamlParser Create()
        {
            var configLineParser = new ConfigLineParser();
            var defaultsParser = ModuleYamlDefaultsParserInstance.Get();
            var configParser = ModuleYamlConfigurationParserInstance.Get();

            return new ModuleYamlParser(configLineParser, defaultsParser, configParser);
        }
    }
}