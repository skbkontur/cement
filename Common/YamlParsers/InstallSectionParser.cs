using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers
{
    public class InstallSectionParser
    {
        private const string NugetPrefix = "nuget ";
        private const string ModulePrefix = "module ";

        [NotNull]
        public InstallData Parse(
            [CanBeNull] object installSection,
            [CanBeNull] object artifactsSection,
            [CanBeNull] object artefactsSection)
        {
            var rawInstall = Transform(installSection);
            var rawArtifacts = Transform(artifactsSection);
            var rawArtefacts = Transform(artefactsSection);

            return Parse(rawInstall, rawArtifacts, rawArtefacts);
        }

        [NotNull]
        private InstallData Parse(
            [NotNull] string[] installSection,
            [NotNull] string[] artifactsSection,
            [NotNull] string[] artefactsSection)
        {
            var externalModules = installSection
                .Where(line => line.StartsWith(ModulePrefix))
                .Select(line => line.Substring(ModulePrefix.Length))
                .ToList();

            var nugets = installSection
                .Where(line => line.StartsWith(NugetPrefix))
                .Select(line => line.Substring(NugetPrefix.Length))
                .ToList();

            var installFiles = installSection
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            var artifacts = installFiles
                .Concat(artifactsSection)
                .Concat(artefactsSection)
                .Where(IsBuildFileName)
                .Distinct()
                .ToList();

            var currentConfigurationInstallFiles = new List<string>(artifacts);

            return new InstallData
            {
                InstallFiles = installFiles,
                CurrentConfigurationInstallFiles = currentConfigurationInstallFiles,
                Artifacts = artifacts,
                ExternalModules = externalModules,
                NuGetPackages = nugets
            };
        }

        [NotNull]
        private string[] Transform(object sectionContent)
        {
            var list = sectionContent as IEnumerable<object>;
            if (list == null)
                return new string[0];

            return list.Cast<string>().ToArray();
        }

        private static bool IsBuildFileName(string line)
        {
            return !line.StartsWith(ModulePrefix) && !line.StartsWith(NugetPrefix);
        }
    }
}