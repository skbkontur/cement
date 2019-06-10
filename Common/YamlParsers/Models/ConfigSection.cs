using Common.YamlParsers.Models;

namespace Common.YamlParsers.V2
{
    public class ConfigSection
    {
        public ConfigSectionTitle Title { get; set; }
        public DepsSection DepsSection { get; set; }
        public BuildData[] BuildSection { get; set; }
        public InstallData InstallSection { get; set; }
    }
}