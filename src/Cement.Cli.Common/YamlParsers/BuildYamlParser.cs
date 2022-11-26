using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common.Exceptions;

namespace Cement.Cli.Common.YamlParsers;

public sealed class BuildYamlParser : ConfigurationYamlParser
{
    public BuildYamlParser(FileInfo modulePath)
        : base(modulePath)
    {
    }

    public BuildYamlParser(string moduleName, string content)
        : base(moduleName, content)
    {
    }

    public List<BuildData> Get(string configName = null)
    {
        try
        {
            return GetBuildData(configName);
        }
        catch (BadYamlException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new BadYamlException(ModuleName, "build", exception.Message);
        }
    }

    private static List<string> GetBuildParams(object section)
    {
        return section switch
        {
            List<object> list => list.Cast<string>().ToList(),
            string str => new[] {str}.ToList(),
            _ => new List<string>()
        };
    }

    private static Tool GetToolFromDict(Dictionary<object, object> dict)
    {
        var tool = new Tool {Name = ToolNames.MSBuild};
        foreach (var key in dict.Keys)
        {
            if ((string)key == "name")
                tool.Name = (string)dict[key];
            if ((string)key == "version")
                tool.Version = (string)dict[key];
        }

        tool.Version ??= CementSettingsRepository.Get().DefaultMsBuildVersion;
        return tool;
    }

    private List<BuildData> GetBuildData(string configName)
    {
        configName ??= GetDefaultConfigurationName();

        var buildSections = GetBuildSectionsFromConfig(configName);
        if (!buildSections.Any())
            buildSections.Add(
                new Dictionary<string, object>
                {
                    {"tool", null},
                    {"target", null},
                    {"configuration", null},
                    {"parameters", null}
                });

        foreach (var buildSection in buildSections)
            ProcessBuildSection(configName, buildSection, buildSections);

        return buildSections.Select(
            parameters =>
                new BuildData(
                    Helper.FixPath((string)parameters["target"]), (string)parameters["configuration"],
                    GetToolFromSection(parameters["tool"]),
                    GetBuildParams(parameters["parameters"]), (string)parameters["name"])).ToList();
    }

    private void ProcessBuildSection(string configName, Dictionary<string, object> buildSection, List<Dictionary<string, object>> buildSections)
    {
        TryUpdateWithDefaultSection(buildSection);
        if (!buildSection.ContainsKey("target") || string.IsNullOrEmpty((string)buildSection["target"]))
            buildSection["target"] = "";
        if ((!buildSection.ContainsKey("configuration") || string.IsNullOrEmpty((string)buildSection["configuration"]))
            && ((string)buildSection["target"]).EndsWith(".sln"))
            throw new BadYamlException(ModuleName, "build", "Build configuration not found in " + configName);
        if (!buildSection.ContainsKey("tool") || buildSection["tool"] == null)
            buildSection["tool"] = ToolNames.MSBuild;
        if (!buildSection.ContainsKey("parameters"))
            buildSection["parameters"] = null;
        if (buildSections.Count > 1 && (!buildSection.ContainsKey("name") || buildSection["name"] == null))
            throw new CementException("Build section hasn't name property.");
        if (!buildSection.ContainsKey("name") || buildSection["name"] == null)
            buildSection["name"] = "";
        if (!buildSection.ContainsKey("configuration"))
            buildSection.Add("configuration", null);
    }

    private Tool GetToolFromSection(object section)
    {
        return section switch
        {
            string {Length: 0} => throw new BadYamlException(ModuleName, "tool", "empty tool"),
            string tool => new Tool {Name = tool},
            IDictionary<object, object> => GetToolFromDict((Dictionary<object, object>)section),
            _ => throw new BadYamlException(ModuleName, "tool", "not dict format")
        };
    }

    private void TryUpdateWithDefaultSection(Dictionary<string, object> parameters)
    {
        var defaultSections = GetBuildSectionsFromConfig("default");
        if (!defaultSections.Any())
            return;
        if (defaultSections.Count > 1)
            throw new CementException("Default configuration can't contains multiple build sections.");
        var defaultSection = defaultSections.First();

        foreach (var key in defaultSection.Keys)
        {
            if (!parameters.ContainsKey(key) || parameters[key] == null)
                parameters[key] = defaultSection[key];
        }
    }

    private List<Dictionary<string, object>> GetBuildSectionsFromConfig(string configuration)
    {
        if (configuration == null)
            return new List<Dictionary<string, object>>();

        var configSection = GetConfigurationSection(configuration);
        const string buildKey = "build";
        if (configSection == null || !configSection.ContainsKey(buildKey) || configSection[buildKey] == null)
            return new List<Dictionary<string, object>>();

        if (configSection[buildKey] is Dictionary<object, object> dict)
        {
            var stringDict = dict.ToDictionary(kvp => (string)kvp.Key, kvp => kvp.Value);
            return new[] {stringDict}.ToList();
        }

        if (configSection[buildKey] is not List<object> list)
            return new List<Dictionary<string, object>>();

        return list
            .Cast<Dictionary<object, object>>()
            .Select(d => d.ToDictionary(
                kvp => (string)kvp.Key,
                kvp => kvp.Value))
            .ToList();
    }
}
