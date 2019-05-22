using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class InstallSectionParser
    {
        private const string NugetPrefix = "nuget ";
        private const string ModulePrefix = "module ";

        [NotNull]
        public InstallData Parse(YamlInstallSections configSections, InstallData defaults = null, InstallData[] parentInstalls = null)
        {
            var externalModules = configSections.Install
                .Where(line => line.StartsWith(ModulePrefix))
                .Select(line => line.Substring(ModulePrefix.Length))
                .ToList();

            var nugets = configSections.Install
                .Where(line => line.StartsWith(NugetPrefix))
                .Select(line => line.Substring(NugetPrefix.Length))
                .ToList();

            var installFiles = configSections.Install
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            var artifacts = installFiles
                .Concat(configSections.Artifacts)
                .Concat(configSections.Artefacts)
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            var currentConfigurationInstallFiles = new List<string>(artifacts);
            var currentConfigInstallData = new InstallData
            {
                InstallFiles = installFiles,
                CurrentConfigurationInstallFiles = currentConfigurationInstallFiles,
                Artifacts = artifacts,
                ExternalModules = externalModules,
                NuGetPackages = nugets
            };

            if (defaults == null && parentInstalls == null)
                return currentConfigInstallData;

            var accumulate = defaults ?? new InstallData();
            if (parentInstalls != null)
                accumulate = parentInstalls.Aggregate(accumulate, (current, parentInstall) => current.JoinWith(parentInstall));

            accumulate = accumulate.JoinWith(currentConfigInstallData);
            return accumulate;
        }

        private static bool IsBuildFileName(string line)
        {
            return !line.StartsWith(ModulePrefix) && !line.StartsWith(NugetPrefix);
        }
    }
}