using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.YamlParsers
{
    public class BuildYamlParser : ConfigurationYamlParser
    {
        public BuildYamlParser(FileInfo modulePath) : base(modulePath)
        {
        }

        public BuildYamlParser(string moduleName, string content) : base(moduleName, content)
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

        private List<BuildData> GetBuildData(string configName)
        {
            configName = configName ?? GetDefaultConfigurationName();

            var buildSections = GetBuildSectionsFromConfig(configName);
            if (!buildSections.Any())
                buildSections.Add(new Dictionary<string, object>
                {
                    {"tool", null},
                    {"target", null},
                    {"configuration", null},
                    {"parameters", null}
                });

            foreach (var buildSection in buildSections)
                ProcessBuildSection(configName, buildSection, buildSections);

            return buildSections.Select(parameters =>
                new BuildData(Helper.FixPath((string) parameters["target"]), (string) parameters["configuration"],
                    GetToolFromSection(parameters["tool"]),
                    GetBuildParams(parameters["parameters"]), (string) parameters["name"])).ToList();
        }

        private void ProcessBuildSection(string configName, Dictionary<string, object> buildSection, List<Dictionary<string, object>> buildSections)
        {
            TryUpdateWithDefaultSection(buildSection);
            if (!buildSection.ContainsKey("target") || string.IsNullOrEmpty((string) buildSection["target"]))
                buildSection["target"] = "";
            if ((!buildSection.ContainsKey("configuration") || string.IsNullOrEmpty((string) buildSection["configuration"]))
                && ((string) buildSection["target"]).EndsWith(".sln"))
                throw new BadYamlException(ModuleName, "build", "Build configuration not found in " + configName);
            if (!buildSection.ContainsKey("tool") || buildSection["tool"] == null)
                buildSection["tool"] = "msbuild";
            if (!buildSection.ContainsKey("parameters"))
                buildSection["parameters"] = null;
            if (buildSections.Count > 1 && (!buildSection.ContainsKey("name") || buildSection["name"] == null))
                throw new CementException("Build section hasn't name property.");
            if (!buildSection.ContainsKey("name") || buildSection["name"] == null)
                buildSection["name"] = "";
            if (!buildSection.ContainsKey("configuration"))
                buildSection.Add("configuration", null);
        }

        private static List<string> GetBuildParams(object section)
        {
            var list = section as List<object>;
            if (list != null)
                return list.Cast<string>().ToList();
            var str = section as string;
            if (str != null)
                return new[] {str}.ToList();
            return new List<string>();
        }

        private Tool GetToolFromSection(object section)
        {
            if (section is string)
            {
                var tool = (string) section;
                if (tool.Length == 0)
                    throw new BadYamlException(ModuleName, "tool", "empty tool");
                return new Tool {Name = tool};
            }
            if (section is IDictionary<object, object>)
                return GetToolFromDict((Dictionary<object, object>) section);
            throw new BadYamlException(ModuleName, "tool", "not dict format");
        }

        private static Tool GetToolFromDict(Dictionary<object, object> dict)
        {
            var tool = new Tool {Name = "msbuild"};
            foreach (var key in dict.Keys)
            {
                if ((string) key == "name")
                    tool.Name = (string) dict[key];
                if ((string) key == "version")
                    tool.Version = (string) dict[key];
            }

            tool.Version = tool.Version ?? CementSettings.Get().DefaultMsBuildVersion;
            return tool;
        }

        private void TryUpdateWithDefaultSection(Dictionary<string, object> parameters)
        {
            var defaultSections = GetBuildSectionsFromConfig("default");
            if (!defaultSections.Any())
                return;
            if (defaultSections.Count() > 1)
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
            if (configSection == null || !configSection.ContainsKey("build") || configSection["build"] == null)
                return new List<Dictionary<string, object>>();

            if (configSection["build"] is Dictionary<object, object>)
            {
                var dict = configSection["build"] as Dictionary<object, object>;
                var stringDict = dict.ToDictionary(kvp => (string) kvp.Key, kvp => kvp.Value);
                return new[] {stringDict}.ToList();
            }
            var list = configSection["build"] as List<object>;
            if (list == null)
                return new List<Dictionary<string, object>>();
            return list.Cast<Dictionary<object, object>>().Select(d => d.ToDictionary(kvp => (string) kvp.Key, kvp => kvp.Value)).ToList();
        }
    }
}