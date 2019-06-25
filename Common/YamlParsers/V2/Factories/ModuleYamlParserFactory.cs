using System;

namespace Common.YamlParsers.V2.Factories
{
    public static class ModuleYamlParserFactory
    {
        private static readonly Lazy<ModuleYamlParser> instance = new Lazy<ModuleYamlParser>(Create);

        public static ModuleYamlParser Get()
        {
            return instance.Value;
        }

        private static ModuleYamlParser Create()
        {
            var configSectionTitleParser = new ConfigSectionTitleParser();
            var depLineParser = new DepSectionItemParser();
            var depsSectionParser = new DepsSectionParser(depLineParser);
            var installSectionParser = new InstallSectionParser();
            var buildSectionParser = new BuildSectionParser();
            var configSectionParser = new ConfigSectionParser(configSectionTitleParser, installSectionParser, depsSectionParser, buildSectionParser);

            var hooksSectionParser = new HooksSectionParser();
            var settingsSectionParser = new SettingsSectionParser();
            var moduleDefaultsParser = new ModuleDefaultsParser(hooksSectionParser, depsSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);

            var depsSectionMerger = new DepsSectionMerger();
            var installSectionMerger = new InstallSectionMerger();

            return new ModuleYamlParser(
                moduleDefaultsParser,
                configSectionParser,
                installSectionMerger,
                depsSectionMerger
                );
        }
    }
}