using System.Collections.Generic;
using System.IO;
using Cement.Cli.Common.YamlParsers;

namespace Cement.Cli.Common;

public sealed class ConfigurationParser : IConfigurationParser
{
    private readonly IConfigurationParser parser;

    public ConfigurationParser(FileInfo modulePath)
    {
        if (File.Exists(Path.Combine(modulePath.FullName, Helper.YamlSpecFile)))
            parser = new ConfigurationYamlParser(modulePath);
    }

    public IList<string> GetConfigurations()
    {
        return parser == null ? new[] {"full-build"} : parser.GetConfigurations();
    }

    public bool ConfigurationExists(string configName)
    {
        return parser == null ? configName.Equals("full-build") : parser.ConfigurationExists(configName);
    }

    public string GetDefaultConfigurationName()
    {
        return parser == null ? null : parser.GetDefaultConfigurationName();
    }

    public IList<string> GetParentConfigurations(string configName)
    {
        return parser == null ? new List<string>() : parser.GetParentConfigurations(configName);
    }

    public Dictionary<string, IList<string>> GetConfigurationsHierarchy()
    {
        return parser == null ? new Dictionary<string, IList<string>> {{"full-build", new List<string>()}} : parser.GetConfigurationsHierarchy();
    }
}
