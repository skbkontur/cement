using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class ModuleYamlDefaultsParser
    {
        private readonly HooksSectionParser hooksSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly SettingsSectionParser settingsSectionParser;
        private readonly BuildSectionParser buildSectionParser;
        private readonly InstallSectionParser installSectionParser;

        public ModuleYamlDefaultsParser(
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
            var buildData = buildSectionParser.ParseBuildSections(defaultsContents.FindValue("build"));
            var depsData = depsSectionParser.Parse(defaultsContents.FindValue("deps"));

            if (buildData != null && buildData.Length > 1)
                throw new CementException("Default configuration can't contains multiple build sections.");

            return new ModuleDefaults
            {
                BuildSection = buildData?.FirstOrDefault(),
                DepsSection = depsData,
                InstallSection = installData,
                SettingsSection = settingsData,
                HooksSection = hooksData
            };
        }
    }
}