using System.Collections.Generic;

namespace Common
{
    public class InstallData
    {
        public string ModuleName { get; set; }
        public List<string> MainConfigBuildFiles { get; set; }
        public List<string> BuildFiles { get; set; }
        public List<string> ExternalModules { get; set; }
        public List<string> Artifacts { get; set; }

        public InstallData(List<string> buildFiles, List<string> externalModules, List<string> artifacts = null)
        {
            MainConfigBuildFiles = buildFiles;
            BuildFiles = buildFiles;
            ExternalModules = externalModules;
            Artifacts = artifacts;
        }

        public InstallData()
        {
            MainConfigBuildFiles = new List<string>();
            BuildFiles = new List<string>();
            ExternalModules = new List<string>();
            Artifacts = new List<string>();
        }
    }
}
