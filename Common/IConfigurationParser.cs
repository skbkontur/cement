using System.Collections.Generic;

namespace Common
{
    public interface IConfigurationParser
    {
        IList<string> GetConfigurations();
        bool ConfigurationExists(string configName);
        string GetDefaultConfigurationName();
        IList<string> GetParentConfigurations(string configName);
        Dictionary<string, IList<string>> GetConfigurationsHierarchy();
    }
}