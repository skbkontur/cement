using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;

namespace Common.YamlParsers.V2
{
    public class InstallSectionParser
    {
        private const string NugetPrefix = "nuget ";
        private const string ModulePrefix = "module ";

        public InstallData Parse(object installSection, object artifactsSection, List<string> currentConfigurationInstallFilesFromDefault = null)
        {
            var sections = new InstallSection(installSection, artifactsSection);
            return Parse(sections, currentConfigurationInstallFilesFromDefault);
        }

        public InstallData Parse(InstallSection configSection, List<string> currentConfigurationInstallFilesFromDefault = null)
        {
            var externalModules = configSection.Install
                .Where(line => line.StartsWith(ModulePrefix))
                .Select(line => line.Substring(ModulePrefix.Length))
                .ToList();

            var nugets = configSection.Install
                .Where(line => line.StartsWith(NugetPrefix))
                .Select(line => line.Substring(NugetPrefix.Length))
                .ToList();

            var installFiles = configSection.Install
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            var artifacts = installFiles
                .Concat(configSection.Artifacts)
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            // currentConfigurationInstallFiles are inherited from 'default' section ¯\_(ツ)_/¯
            var currentConfigurationInstallFiles = (currentConfigurationInstallFilesFromDefault ?? Enumerable.Empty<string>())
                .Concat(artifacts)
                .ToList();

            var installData = new InstallData
            {
                InstallFiles = installFiles,
                CurrentConfigurationInstallFiles = currentConfigurationInstallFiles,
                Artifacts = artifacts,
                ExternalModules = externalModules,
                NuGetPackages = nugets
            };
            return installData;
        }

        private static bool IsBuildFileName(string line)
        {
            return !line.StartsWith(ModulePrefix) && !line.StartsWith(NugetPrefix);
        }
    }
}