using System.Collections.Generic;
using JetBrains.Annotations;

namespace Common
{
    public class InstallData
    {
        public string ModuleName { get; set; }

        /// <summary>
        /// <para>Install files of current configuration.</para>
        /// <para>1. Contains results of build. Those who want to reference this module/configuration should reference these results.</para>
        /// <para>2. Contains artifacts (files that can be used by other modules, but are ignored while running `cm ref add`).</para>
        /// <para>3. Does not contain nuget and external modules.</para>
        /// </summary>
        [CanBeNull]
        public List<string> CurrentConfigurationInstallFiles { get; set; }

        /// <summary>
        /// <para>Install files of current and parent configuration.</para>
        /// <para>1. Contains results of build. Those who want to reference this module/configuration may reference these results.</para>
        /// <para>2. Does not contain artifacts.</para>
        /// <para>3. Does not contain nuget and external modules.</para>
        /// </summary>
        [CanBeNull]
        public List<string> InstallFiles { get; set; }

        /// <summary>
        /// Install modules of current and parent configurations.
        /// </summary>
        [CanBeNull]
        public List<string> ExternalModules { get; set; }

        /// <summary>
        /// Install nuget of current and parent configurations.
        /// </summary>
        [CanBeNull]
        public List<string> NuGetPackages { get; set; }

        /// <summary>
        /// Install files and artifacts of current and parent configurations.
        /// </summary>
        [CanBeNull]
        public List<string> Artifacts { get; set; }

        public InstallData(List<string> installFiles, List<string> externalModules, List<string> nuGetPackages = null, List < string> artifacts = null)
        {
            CurrentConfigurationInstallFiles = installFiles;
            InstallFiles = installFiles;
            ExternalModules = externalModules;
            NuGetPackages = nuGetPackages;
            Artifacts = artifacts;
        }

        public InstallData()
        {
            CurrentConfigurationInstallFiles = new List<string>();
            InstallFiles = new List<string>();
            ExternalModules = new List<string>();
            NuGetPackages = new List<string>();
            Artifacts = new List<string>();
        }
    }
}