using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers
{
    public class YamlConfigurationParser
    {
        private readonly InstallSectionParser installSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly BuildSectionParser buildSectionParser;

        public YamlConfigurationParser(
            InstallSectionParser installSectionParser,
            DepsSectionParser depsSectionParser,
            BuildSectionParser buildSectionParser
            )
        {
            this.installSectionParser = installSectionParser;
            this.depsSectionParser = depsSectionParser;
            this.buildSectionParser = buildSectionParser;
        }

        public ModuleConfiguration Parse(
            [CanBeNull] ModuleDefaults defaults,
            Dictionary<object, object> configurationContents,
            Dictionary<string, ModuleConfiguration> knownConfigurations,
            [CanBeNull] string[] parentConfigs
        )
        {
            var result = new ModuleConfiguration();

            var parentInstalls = parentConfigs?.Select(c => knownConfigurations[c].InstallSection).ToArray();
            var installSection = configurationContents.FindValue("install");
            var artifactsSection = configurationContents.FindValue("artifacts");
            var artefactsSection = configurationContents.FindValue("artefacts");
            var currentInstallSection = installSectionParser.Parse(installSection, artifactsSection, artefactsSection);
            result.InstallSection = Merge(defaults?.InstallSection, currentInstallSection, parentInstalls);

            var parentDeps = parentConfigs?
                .SelectMany(c => knownConfigurations[c].Dependencies.Deps)
                .Distinct()
                .ToArray();

            var currentDepsSection = depsSectionParser.Parse(configurationContents.FindValue("deps"));
            result.Dependencies = Merge(defaults?.BuildSection, parentDeps, currentDepsSection);

            var currentBuildSection = buildSectionParser.ParseBuildConfigurationSections(configurationContents.FindValue("build"));
            result.BuildSection = Merge(defaults?.BuildSection, currentBuildSection);

            return result;
        }

        private BuildData[] Merge(
            [CanBeNull] BuildData[] defaultsBuildSection,
            [CanBeNull] BuildData[] currentBuildSectionSection)
        {
            throw new System.NotImplementedException();
        }

        private DepsContent Merge(
            [CanBeNull] BuildData[] defaultsInstallSection,
            [CanBeNull] Dep[] currentInstallSection,
            [CanBeNull] DepsContent currentDepsSection)
        {
            throw new System.NotImplementedException();
        }

        private InstallData Merge(
            [CanBeNull] InstallData defaultsInstallSection,
            [CanBeNull] InstallData currentInstallSection,
            [CanBeNull] InstallData[] parentInstalls)
        {
            throw new System.NotImplementedException();
        }
    }
}