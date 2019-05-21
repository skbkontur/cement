using System.Collections.Generic;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers
{
    public class YamlModuleDefaultsParser
    {
        private readonly HooksSectionParser hooksSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly SettingsSectionParser settingsSectionParser;
        private readonly BuildSectionParser buildSectionParser;
        private readonly InstallSectionParser installSectionParser;

        public YamlModuleDefaultsParser(
            HooksSectionParser hooksSectionParser,
            DepsSectionParser depsSectionParser,
            SettingsSectionParser settingsSectionParser,
            BuildSectionParser buildSectionParser,
            InstallSectionParser installSectionParser
            )
        {
            this.hooksSectionParser = hooksSectionParser;
            this.depsSectionParser = depsSectionParser;
            this.settingsSectionParser = settingsSectionParser;
            this.buildSectionParser = buildSectionParser;
            this.installSectionParser = installSectionParser;
        }

        [CanBeNull]
        public ModuleDefaults Parse([CanBeNull] Dictionary<object, object> defaultsContents)
        {
            if (defaultsContents == null)
                return null;

            var installSection = defaultsContents.FindValue("install");
            var artifactsSection = defaultsContents.FindValue("artifacts");
            var artefactsSection = defaultsContents.FindValue("artefacts");
            var installData = installSectionParser.Parse(installSection, artifactsSection, artefactsSection);
            var hooksData = hooksSectionParser.Parse(defaultsContents.FindValue("hooks"));
            var settingsData = settingsSectionParser.Parse(defaultsContents.FindValue("settings"));
            var buildData = buildSectionParser.ParseBuildDefaultsSections(defaultsContents.FindValue("build"));
            var depsData = depsSectionParser.Parse(defaultsContents.FindValue("deps"));

            return new ModuleDefaults
            {
                BuildSection = buildData,
                DepsSection = depsData,
                InstallSection = installData,
                SettingsSection = settingsData,
                HooksSection = hooksData
            };
        }
    }
}