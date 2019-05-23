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
            var depSectionParser = new DepsSectionParser(new DepLineParser());
            var buildSectionParser = new BuildSectionParser();
            var installSectionParser = new InstallSectionParser();

            var hooksSectionParser = new HooksSectionParser();
            var settingsSectionParser = new SettingsSectionParser();
            var defaultsParser = new ModuleYamlDefaultsParser(hooksSectionParser, depSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);

            return new ModuleYamlParser(
                configLineParser,
                defaultsParser,
                installSectionParser,
                depSectionParser,
                buildSectionParser);
        }
    }
}