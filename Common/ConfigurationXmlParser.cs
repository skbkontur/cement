using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace Common;

public sealed class ConfigurationXmlParser : IConfigurationParser
{
    private readonly XmlDocument document;

    public ConfigurationXmlParser(string content)
    {
        document = new XmlDocument();
        document.LoadXml(content);
    }

    public ConfigurationXmlParser(FileInfo modulePath)
    {
        document = new XmlDocument();
        document.Load(Path.Combine(modulePath.FullName, ".cm", "spec.xml"));
    }

    public IList<string> GetConfigurations()
    {
        var configurations = new List<string>();
        var configurationsTags = document.GetElementsByTagName("conf");
        foreach (XmlNode node in configurationsTags)
        {
            if (node.Attributes != null)
                configurations.Add(node.Attributes["name"].Value);
        }

        return configurations;
    }

    public bool ConfigurationExists(string configName)
    {
        return GetConfigurations().Contains(configName);
    }

    public string GetDefaultConfigurationName()
    {
        var defaultConfiguration = "full-build";
        var configurationsTags = document.GetElementsByTagName("default-config");
        foreach (XmlNode node in configurationsTags)
        {
            defaultConfiguration = node.Attributes["name"].Value;
        }

        return defaultConfiguration;
    }

    public IList<string> GetParentConfigurations(string configName)
    {
        var parents = new List<string>();
        var configurationsTags = document.GetElementsByTagName("conf");
        foreach (XmlNode node in configurationsTags)
        {
            if (node.Attributes != null && configName.Equals(node.Attributes["name"].Value))
                if (node.Attributes["parents"] != null)
                    return node.Attributes["parents"].Value.Split(',').Select(par => par.Trim()).ToList();
        }

        return parents;
    }

    public Dictionary<string, IList<string>> GetConfigurationsHierarchy()
    {
        var result = new Dictionary<string, IList<string>>();
        result["full-build"] = new List<string>();
        var configurationsList = GetConfigurations();
        foreach (var config in configurationsList)
        {
            if (!result.ContainsKey(config))
                result[config] = new List<string>();
            var parentConfigurations = GetParentConfigurations(config);
            if (parentConfigurations == null)
                continue;
            foreach (var parent in parentConfigurations)
            {
                result[config].Add(parent);
            }
        }

        foreach (var config in configurationsList.Where(c => !c.Equals("full-build")))
        {
            if (!result[config].Contains("full-build"))
                result[config].Add("full-build");
        }

        return result;
    }
}
