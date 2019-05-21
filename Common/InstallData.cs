using System.Collections.Generic;

namespace Common
{
    public class InstallData
    {
        public string ModuleName { get; set; }

        /// <summary>
        /// <para>Installs of current configuration.</para>
        /// <para>1. Contains results of build. Those who want to reference this module/configuration should reference these results.</para>
        /// <para>2. Contains artifacts (files that can be used by other modules, but are ignored while running `cm ref add`).</para>
        /// <para>3. Does not contain nuget and external modules.</para>
        /// </summary>
        public List<string> MainConfigBuildFiles { get; set; }

        /// <summary>
        /// <para>Install files of current and parent configuration.</para>
        /// <para>1. Contains results of build. Those who want to reference this module/configuration may reference these results.</para>
        /// <para>2. Does not contain artifacts.</para>
        /// <para>3. Does not contain nuget and external modules.</para>
        /// </summary>
        public List<string> BuildFiles { get; set; }

        /// <summary>
        /// Install modules of current and parent configurations.
        /// </summary>
        public List<string> ExternalModules { get; set; }

        /// <summary>
        /// Install nuget of current and parent configurations.
        /// </summary>
        public List<string> NuGetPackages { get; set; }

        /// <summary>
        /// Install and artifacts of current and parent configurations.
        /// </summary>
        public List<string> Artifacts { get; set; }

        public InstallData(List<string> buildFiles, List<string> externalModules, List<string> nuGetPackages = null, List < string> artifacts = null)
        {
            MainConfigBuildFiles = buildFiles;
            BuildFiles = buildFiles;
            ExternalModules = externalModules;
            NuGetPackages = nuGetPackages;
            Artifacts = artifacts;
        }

        public InstallData()
        {
            MainConfigBuildFiles = new List<string>();
            BuildFiles = new List<string>();
            ExternalModules = new List<string>();
            NuGetPackages = new List<string>();
            Artifacts = new List<string>();
        }
    }
}