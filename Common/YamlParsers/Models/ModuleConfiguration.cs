namespace Common.YamlParsers.Models
{
    public class ModuleConfiguration
    {
        public string Name {get;set;}
        public bool IsDefault { get; set; }

        public DepsContent Dependencies {get;set; }

        public InstallData InstallSection { get;set; }

        public BuildData[] BuildSection { get;set; }
        public string[] ParentConfigs { get; set; }
    }
}