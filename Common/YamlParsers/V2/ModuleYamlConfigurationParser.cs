using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class ModuleYamlConfigurationParser
    {
        private readonly InstallSectionParser installSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly BuildSectionParser buildSectionParser;

        public ModuleYamlConfigurationParser(
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

            var defaultForcedBranches = defaults?.DepsSection?.HasForcedBranches() == true ? defaults.DepsSection.Force : null;
            result.Dependencies = depsSectionParser.Parse(configurationContents.FindValue("deps"), defaultForcedBranches, parentDeps);

            var buildSectionFromYaml = configurationContents.FindValue("build");
            result.BuildSection = buildSectionParser.ParseBuildSections(buildSectionFromYaml, defaults?.BuildSection);

            EnsureIsValid(result);

            return result;
        }

        private void EnsureIsValid(ModuleConfiguration result)
        {
            var invalidTarget = result.BuildSection?.FirstOrDefault(s => s.Target.EndsWith(".sln") && string.IsNullOrEmpty(s.Configuration));
            if (invalidTarget != null)
                throw new BadYamlException("build", "Build configuration not found. You have to explicitly specify 'configuration' for *.sln targets.");
        }

        private InstallData Merge(
            [CanBeNull] InstallData defaultsInstallSection,
            [CanBeNull] InstallData currentInstallSection,
            [CanBeNull] InstallData[] parentInstalls)
        {
            var accumulate = defaultsInstallSection ?? new InstallData();

            if (currentInstallSection != null)
                accumulate = accumulate.JoinWith(currentInstallSection);

            if (parentInstalls != null)
                accumulate = parentInstalls.Aggregate(accumulate, (current, parentInstall) => current.JoinWith(parentInstall));

            return accumulate;
        }
    }
}