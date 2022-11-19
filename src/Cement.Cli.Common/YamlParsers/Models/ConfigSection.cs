namespace Cement.Cli.Common.YamlParsers.Models;

public sealed class ConfigSection
{
    public ConfigSectionTitle Title { get; set; }
    public DepsSection DepsSection { get; set; }
    public BuildData[] BuildSection { get; set; }
    public InstallData InstallSection { get; set; }
}
