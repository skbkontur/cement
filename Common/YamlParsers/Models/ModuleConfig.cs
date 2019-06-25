namespace Common.YamlParsers.Models
{
    public class ModuleConfig
    {
        public string Name {get;set;}
        public bool IsDefault { get; set; }
        public DepsData Deps {get;set; }
        public InstallData Installs { get;set; }
        public BuildData[] Builds { get;set; }
    }
}