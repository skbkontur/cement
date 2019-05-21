using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers
{
    public class YamlConfigurationParser
    {
        public ModuleConfiguration Parse(
            [CanBeNull] ModuleDefaults defaults,
            Dictionary<object, object> configurationContents,
            Dictionary<string, ModuleConfiguration> knownConfigurations,
            [CanBeNull] string[] parentConfigs
        )
        {
            var result = new ModuleConfiguration();

            var parentInstalls = parentConfigs?.Select(c => knownConfigurations[c].InstallSection).ToArray();
            configurationContents.TryGetValue("install", out var installSection);
            configurationContents.TryGetValue("artifacts", out var artifactsSection);
            configurationContents.TryGetValue("artefacts", out var artefactsSection);
            result.InstallSection = Merge(defaults?.InstallSection, parentInstalls, installSection, artifactsSection, artefactsSection);


            var parentDeps = parentConfigs?
                .SelectMany(c => knownConfigurations[c].Dependencies.Deps)
                .Distinct()
                .ToArray();
            configurationContents.TryGetValue("deps", out var depsSection);
            result.Dependencies = Merge(parentDeps, depsSection);


            configurationContents.TryGetValue("build", out var buildSection);
            result.BuildSection = Merge(defaults?.BuildSection, buildSection);

            return result;
        }


        private InstallData Merge(
            [CanBeNull] InstallData defaultsInstallSection,
            [CanBeNull] InstallData[] parentInstalls,
            [CanBeNull] object installSection,
            [CanBeNull] object artifactsSection,
            [CanBeNull] object artefactsSection)
        {
            throw new System.NotImplementedException();
        }

        private DepsContent Merge(
            [CanBeNull] Dep[] defaultsInstallSection,
            [CanBeNull] object parentInstalls)
        {
            throw new System.NotImplementedException();
        }

        private BuildData[] Merge(
            [CanBeNull] BuildData[] defaultsInstallSection,
            [CanBeNull] object parentInstalls)
        {
            throw new System.NotImplementedException();
        }
    }
}