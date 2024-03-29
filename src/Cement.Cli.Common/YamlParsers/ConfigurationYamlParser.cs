using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.Exceptions;
using SharpYaml.Serialization;

namespace Cement.Cli.Common.YamlParsers;

public class ConfigurationYamlParser : IConfigurationParser
{
    protected readonly string ModuleName;
    private readonly Dictionary<string, object> configurationsDescription = new();

    public ConfigurationYamlParser(FileSystemInfo moduleName)
    {
        var specFilePath = Path.Combine(moduleName.FullName, Helper.YamlSpecFile);
        var text = File.ReadAllText(specFilePath).Replace("\t", "    ");
        ModuleName = moduleName.Name;

        TryParseYaml(text);
    }

    public ConfigurationYamlParser(string moduleName, string configFileContents)
    {
        ModuleName = moduleName;
        TryParseYaml(configFileContents);
    }

    public IList<string> GetConfigurations()
    {
        return configurationsDescription.Keys
            .Where(config => !"default".Equals(config))
            .Select(GetRealConfigurationName)
            .ToList();
    }

    public Dictionary<string, object> GetConfigurationDescription(string configName)
    {
        return configurationsDescription.TryGetValue(configName, out var result)
            ? (result as Dictionary<object, object>)?.ToDictionary(k =>(string)k.Key, v => v.Value)
            : null;
    }

    public bool ConfigurationExists(string configName)
    {
        return configurationsDescription.Keys.Select(GetRealConfigurationName).Contains(configName);
    }

    public string GetDefaultConfigurationName()
    {
        var defaultConfigurations = configurationsDescription.Keys
            .Where(section => section.EndsWith("*default"))
            .ToList();

        return defaultConfigurations.Count switch
        {
            > 1 => throw new BadYamlException(ModuleName, "configurations", "Multiple default configurations exists"),
            0 => "full-build",
            _ => GetRealConfigurationName(defaultConfigurations.First())
        };
    }

    public IList<string> GetParentConfigurations(string configName)
    {
        foreach (var configNode in configurationsDescription.Keys)
        {
            if (configName.Equals(GetRealConfigurationName(configNode)))
                return IsInherited(configName)
                    ? configNode.Split('>').Last()
                        .Split('*')[0]
                        .Split(',')
                        .Select(parentConf => parentConf.Trim())
                        .Where(ConfigurationExists)
                        .ToList()
                    : null;
        }

        return null;
    }

    public Dictionary<string, IList<string>> GetConfigurationsHierarchy()
    {
        var result = new Dictionary<string, IList<string>>();
        var configurationsList = GetConfigurations();
        foreach (var config in configurationsList)
        {
            if (!result.ContainsKey(config))
                result[config] = new List<string>();
            var childrenConfigs = GetParentConfigurations(config);
            if (childrenConfigs == null)
                continue;
            foreach (var child in childrenConfigs)
            {
                if (!result.ContainsKey(child))
                    result[child] = new List<string>();
                result[child].Add(config);
            }
        }

        return result;
    }

    protected Dictionary<string, object> GetConfigurationSection(string configName)
    {
        try
        {
            var withSameName = configurationsDescription.Keys
                .Where(config => configName.Equals(GetRealConfigurationName(config)))
                .ToList();
            switch (withSameName.Count)
            {
                case 0:
                    return new Dictionary<string, object>();
                case > 1:
                    throw new BadYamlException(ModuleName, "configurations", "duplicate configuration name " + configName);
            }

            var section = configurationsDescription[withSameName.First()];
            if (section is null or string)
                return new Dictionary<string, object>();

            var dict = (Dictionary<object, object>)section;
            return dict.Keys.ToDictionary(key => (string)key, key => dict[key]);
        }
        catch (BadYamlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BadYamlException(ModuleName, "configurations", exception.Message);
        }
    }

    protected bool IsInherited(string configName)
    {
        return
            configurationsDescription.Keys.Any(
                config =>
                    configName.Equals(GetRealConfigurationName(config)) && config.Contains('>'));
    }

    private static string GetRealConfigurationName(string configNameNode)
    {
        return configNameNode.Split('>', '*').First().Trim();
    }

    private void TryParseYaml(string text)
    {
        try
        {
            var serializer = new Serializer();
            var content = serializer.Deserialize(text);
            var dict = (Dictionary<object, object>)content;
            foreach (var key in dict.Keys)
                configurationsDescription.Add((string)key, dict[key]);
        }
        catch (Exception)
        {
            throw new CementException("Fail to parse module.yaml file in " + ModuleName + ". Check that yaml is correct dictionary <string, object>");
        }
    }
}
