using System;

namespace Common.YamlParsers.V2.Factories
{
    public static class ModuleYamlDefaultsParserInstance
    {
        private static readonly Lazy<ModuleYamlDefaultsParser> instance = new Lazy<ModuleYamlDefaultsParser>(Create);

        public static ModuleYamlDefaultsParser Get()
        {
            return instance.Value;
        }

        private static ModuleYamlDefaultsParser Create()
        {
            var depSectionParser = new DepsSectionParser(new DepLineParser());
            var buildSectionParser = new BuildSectionParser();
            var installSectionParser = new InstallSectionParser();

            var hooksSectionParser = new HooksSectionParser();
            var settingsSectionParser = new SettingsSectionParser();
            return new ModuleYamlDefaultsParser(hooksSectionParser, depSectionParser, settingsSectionParser, buildSectionParser, installSectionParser);
        }
    }
}