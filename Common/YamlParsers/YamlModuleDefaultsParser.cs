using System.Collections.Generic;
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

            var result = new ModuleDefaults();

            defaultsContents.TryGetValue("hooks", out var hooksSection);
            result.HooksSection = hooksSectionParser.Parse(hooksSection);

            defaultsContents.TryGetValue("settings", out var settingsSection);
            result.SettingsSection = settingsSectionParser.Parse(settingsSection);

            defaultsContents.TryGetValue("build", out var buildSection);
            result.BuildSection = buildSectionParser.ParseBuildDefaultsSections(buildSection);

            defaultsContents.TryGetValue("deps", out var depsSection);
            result.DepsSection = depsSectionParser.Parse(depsSection);

            defaultsContents.TryGetValue("install", out var installSection);
            defaultsContents.TryGetValue("artifacts", out var artifactsSection);
            defaultsContents.TryGetValue("artefacts", out var artefactsSection);
            result.InstallSection = installSectionParser.Parse(installSection, artifactsSection, artefactsSection);

            return result;
        }
    }
}