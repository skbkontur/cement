using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;
using SharpYaml.Serialization;


namespace Common.YamlParsers
{
    public class ModuleDefinition
    {
        public ModuleDefinition([NotNull] Dictionary<string, ModuleConfiguration> configurations)
        {
            Configurations = configurations;
        }

        [NotNull]
        public Dictionary<string, ModuleConfiguration> Configurations {get; set;}

        [CanBeNull]
        public ModuleDefaults Defaults {get; set;}
    }

    public class ModuleDefaults
    {
        [CanBeNull]
        public string[] HooksSection { get; set; }

        [CanBeNull]
        public DepsContent DepsSection { get; set; }

        [CanBeNull]
        public ModuleSettings SettingsSection { get; set;}

        [CanBeNull]
        public BuildData[] BuildSection { get; set; }

        [CanBeNull]
        public InstallData InstallSection { get; set; }
    }

    public class ModuleConfiguration
    {
        public string Name {get;set;}
        public bool IsDefault { get; set; }

        public DepsContent Dependencies {get;set; }

        public InstallData InstallSection { get;set; }

        public BuildData[] BuildSection { get;set; }
    }

    public class ModuleYamlParser
    {
        private readonly ConfigLineParser configLineParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly BuildSectionParser buildSectionParser;
        private readonly InstallSectionParser installSectionParser;
        private readonly YamlModuleDefaultsParser yamlModuleDefaultsParser;

        public ModuleYamlParser(
            ConfigLineParser configLineParser,
            DepsSectionParser depsSectionParser,
            BuildSectionParser buildSectionParser,
            InstallSectionParser installSectionParser,
            YamlModuleDefaultsParser yamlModuleDefaultsParser)
        {
            this.configLineParser = configLineParser;
            this.depsSectionParser = depsSectionParser;
            this.buildSectionParser = buildSectionParser;
            this.installSectionParser = installSectionParser;
            this.yamlModuleDefaultsParser = yamlModuleDefaultsParser;
        }

        public ModuleDefinition Parse(string module, string content)
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
            var defaults = yamlModuleDefaultsParser.Parse(defaultSection as Dictionary<object, object>);

            foreach (var configName in configs)
            {
                var parentConfigs = hierarchy.FindClosestParents(configName);
                var configKey = parsedConfigToRawLine[configName];
                var configurationContents = (Dictionary<object, object>) yaml[configKey];
                var mc = new ModuleConfiguration();

                foreach (var section in configurationContents)
                {
                    var sectionName = section.Key;
                    var sectionContents = section.Value;

                    switch (sectionName)
                    {
                        case "deps":
                        {
                            var parentDeps = parentConfigs?
                                .SelectMany(c => configurations[c].Dependencies.Deps)
                                .Distinct()
                                .ToArray();

                            mc.Dependencies = depsSectionParser.Parse(sectionContents, parentDeps);
                            break;
                        }
                        case "build":
                        {
                            mc.BuildSection = buildSectionParser.ParseBuildConfigurationSections(sectionContents);
                            break;
                        }

                        case "install":
                        {
//                            var parentInstalls = parentConfigs?.Select(c => configurations[c].InstallSection).ToArray();
//                            mc.InstallSection = installSectionParser.ParseInstallSection(sectionContents, parentInstalls);
                            break;
                        }
                    }
                }

                configurations[configName] = mc;
            }

            return new ModuleDefinition(configurations);
        }



    }
}