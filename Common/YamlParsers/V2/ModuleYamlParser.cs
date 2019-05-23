using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using SharpYaml.Serialization;

namespace Common.YamlParsers.V2
{
    public class ModuleYamlParser
    {
        private readonly ConfigLineParser configLineParser;
        private readonly ModuleYamlDefaultsParser moduleYamlDefaultsParser;
        private readonly InstallSectionParser installSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly BuildSectionParser buildSectionParser;

        public ModuleYamlParser(
            ConfigLineParser configLineParser,
            ModuleYamlDefaultsParser moduleYamlDefaultsParser,
            InstallSectionParser installSectionParser,
            DepsSectionParser depsSectionParser,
            BuildSectionParser buildSectionParser)
        {
            this.configLineParser = configLineParser;
            this.moduleYamlDefaultsParser = moduleYamlDefaultsParser;
            this.installSectionParser = installSectionParser;
            this.depsSectionParser = depsSectionParser;
            this.buildSectionParser = buildSectionParser;
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

            var defaultSection = yaml.FindValue("default") as Dictionary<object, object>;
            var defaults = moduleYamlDefaultsParser.Parse(defaultSection);
            var defaultConfigName = hierarchy.GetDefault();

            foreach (var configName in configs)
            {
                var parentConfigs = hierarchy.FindClosestParents(configName);
                var configKey = parsedConfigToRawLine[configName];
                var configurationContents = (Dictionary<object, object>) yaml[configKey];

                var parentInstalls = parentConfigs?.Select(c => configurations[c].InstallSection).ToArray();
                var parentDeps = parentConfigs?
                    .SelectMany(c => configurations[c].Dependencies.Deps)
                    .Distinct()
                    .ToArray();

                var installSection = configurationContents.FindValue("install");
                var artifactsSection = configurationContents.FindValue("artifacts");
                var artefactsSection = configurationContents.FindValue("artefacts");
                var depsSection = configurationContents.FindValue("deps");
                var buildSection = configurationContents.FindValue("build");

                var sections = new YamlInstallSections(installSection, artifactsSection, artefactsSection);

                var result = new ModuleConfiguration
                {
                    InstallSection = installSectionParser.Parse(sections, defaults?.InstallSection, parentInstalls),
                    Dependencies = depsSectionParser.Parse(depsSection, defaults?.DepsSection, parentDeps),
                    BuildSection = buildSectionParser.ParseConfiguration(buildSection, defaults?.BuildSection),
                    Name = configName,
                    IsDefault = configName == defaultConfigName,
                    ParentConfigs = parentConfigs
                };
                configurations[configName] = result;
            }

            return new ModuleDefinition(configurations);
        }
    }
}