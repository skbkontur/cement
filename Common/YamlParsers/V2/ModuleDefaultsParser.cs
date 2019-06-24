using System;
using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class ModuleDefaultsParser
    {
        private readonly HooksSectionParser hooksSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly SettingsSectionParser settingsSectionParser;
        private readonly BuildSectionParser buildSectionParser;
        private readonly InstallSectionParser installSectionParser;

        public ModuleDefaultsParser(
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

            try
            {
                var installData = installSectionParser.Parse(installSection, artifactsSection);
                var hooksData = hooksSectionParser.Parse(defaultsContents.FindValue("hooks"));
                var settingsData = settingsSectionParser.Parse(defaultsContents.FindValue("settings"));
                var buildData = buildSectionParser.ParseDefaults(defaultsContents.FindValue("build"));
                var depsSection = depsSectionParser.Parse(defaultsContents.FindValue("deps"));

                return new ModuleDefaults
                {
                    BuildSection = buildData,
                    DepsSection = depsSection,
                    InstallSection = installData,
                    SettingsSection = settingsData,
                    HooksSection = hooksData
                };
            }
            catch (BadYamlException ex)
            {
                throw new BadYamlException("default." + ex.SectionName, ex.Message);
            }
        }
    }
}