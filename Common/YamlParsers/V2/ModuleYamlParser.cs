using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using SharpYaml.Serialization;

namespace Common.YamlParsers.V2
{
    public class ModuleYamlParser
    {
        private readonly ConfigLineParser configLineParser;
        private readonly ModuleYamlDefaultsParser moduleYamlDefaultsParser;
        private readonly ModuleYamlConfigurationParser moduleYamlConfigurationParser;

        public ModuleYamlParser(
            ConfigLineParser configLineParser,
            ModuleYamlDefaultsParser moduleYamlDefaultsParser,
            ModuleYamlConfigurationParser moduleYamlConfigurationParser)
        {
            this.configLineParser = configLineParser;
            this.moduleYamlDefaultsParser = moduleYamlDefaultsParser;
            this.moduleYamlConfigurationParser = moduleYamlConfigurationParser;
        }

        public ModuleDefinition Parse(string content)
        {
            var serializer = new Serializer();
            var yaml = (Dictionary<object, object>) serializer.Deserialize(content);
            var configurations = new Dictionary<string, ModuleConfiguration>();

            var rawConfigLines = yaml.Keys.Select(k => (string) k).Where(line => line != "default").ToArray();
            var configLines = new List<ConfigurationLine>(rawConfigLines.Length);
            var parsedConfigToRawLine = new Dictionary<string, string>();

            foreach (var line in rawConfigLines)
            {
                var parsed = configLineParser.Parse(line);
                parsedConfigToRawLine[parsed.ConfigName] = line;
                configLines.Add(parsed);
            }

            var hierarchy = new ConfigurationHierarchy(configLines.ToArray());
            var configs = hierarchy.GetAll();

            yaml.TryGetValue("default", out var defaultSection);
            var defaults = moduleYamlDefaultsParser.Parse(defaultSection as Dictionary<object, object>);

            foreach (var configName in configs)
            {
                var parentConfigs = hierarchy.FindClosestParents(configName);
                var configKey = parsedConfigToRawLine[configName];
                var configurationContents = (Dictionary<object, object>) yaml[configKey];
                configurations[configName] = moduleYamlConfigurationParser.Parse(defaults, configurationContents, configurations, parentConfigs);
            }

            return new ModuleDefinition(configurations);
        }
    }
}