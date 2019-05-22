using System;

namespace Common.YamlParsers.V2.Factories
{
    public static class ModuleYamlConfigurationParserInstance
    {
        private static readonly Lazy<ModuleYamlConfigurationParser> instance = new Lazy<ModuleYamlConfigurationParser>(Create);

        public static ModuleYamlConfigurationParser Get()
        {
            return instance.Value;
        }

        private static ModuleYamlConfigurationParser Create()
        {
            var depLineParser = new DepLineParser();
            var depSectionParser = new DepsSectionParser(depLineParser);
            var buildSectionParser = new BuildSectionParser();
            var installSectionParser = new InstallSectionParser();

            return new ModuleYamlConfigurationParser(installSectionParser, depSectionParser, buildSectionParser);
        }
    }
}