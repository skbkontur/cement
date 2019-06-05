using System;
using System.Collections.Generic;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers.Models;
using Common.YamlParsers.V2.Factories;
using SharpYaml.Serialization;

namespace Common.YamlParsers.V2
{
    public class ModuleYamlParser
    {
        private readonly ModuleDefaultsParser moduleDefaultsParser;
        private readonly ConfigSectionParser configSectionParser;
        private readonly DepsSectionMerger depsSectionMerger;
        private readonly InstallSectionMerger installSectionMerger;

        public ModuleYamlParser(
            ModuleDefaultsParser moduleDefaultsParser,
            ConfigSectionParser configSectionParser,
            InstallSectionMerger installSectionMerger,
            DepsSectionMerger depsSectionMerger)
        {
            this.moduleDefaultsParser = moduleDefaultsParser;
            this.configSectionParser = configSectionParser;
            this.depsSectionMerger = depsSectionMerger;
            this.installSectionMerger = installSectionMerger;
        }

        public ModuleDefinition Parse(string content, string moduleInfo = "")
        {
            content = PatchTabs(content, moduleInfo);
            var serializer = new Serializer();

            var yaml = (Dictionary<object, object>) serializer.Deserialize(content);

            var defaultSection = yaml.FindValue("default") as Dictionary<object, object>;
            var moduleDefaults = moduleDefaultsParser.Parse(defaultSection) ?? new ModuleDefaults();

            var configSectionMap = yaml
                .Where(section => (string)section.Key != "default")
                .Select(section => configSectionParser.Parse(section, moduleDefaults))
                .ToDictionary(configSection => configSection.Title.Name);

            var configSectionTitles = configSectionMap.Values
                .Select(section => section.Title)
                .ToArray();

            var hierarchy = ConfigurationHierarchyFactory.Get(configSectionTitles);
            var orderedConfigNames = hierarchy.GetAll();
            var defaultConfigName = hierarchy.FindDefault();

            var configurations = new Dictionary<string, ModuleConfig>();
            foreach (var configName in orderedConfigNames)
            {
                var configSection = configSectionMap[configName];

                var allParentNames = hierarchy.GetAllParents(configName);
                var parentInstalls = allParentNames?.Select(name => configurations[name].Installs).ToArray();
                var parentDeps = allParentNames?.Select(name => configSectionMap[name].DepsSection).ToArray();

                try
                {
                    var installContent = installSectionMerger.Merge(configSection.InstallSection, moduleDefaults?.InstallSection, parentInstalls);
                    var depsContent = depsSectionMerger.Merge(configSection.DepsSection, moduleDefaults?.DepsSection, parentDeps);

                    var result = new ModuleConfig
                    {
                        Name = configName,
                        IsDefault = configName == defaultConfigName,
                        Installs = installContent,
                        Deps = depsContent,
                        Builds = configSection.BuildSection
                    };
                    configurations[configName] = result;
                }
                catch (BadYamlException ex)
                {
                    throw new BadYamlException(configName + "." + ex.SectionName, ex.Message);
                }
            }

            return new ModuleDefinition(configurations, moduleDefaults);
        }

        private string PatchTabs(string input, string moduleInfo = null)
        {
            if (!input.Contains('\t'))
                return input;

            var msg = "These should not be any tab characters in yaml file. Replace tab characters with spaces.";
            if (!string.IsNullOrEmpty(moduleInfo))
                msg += " " + moduleInfo;

            ConsoleWriter.WriteWarning(msg);

            var lines = input.Split(new[] {Environment.NewLine}, StringSplitOptions.None);
            for (var i = 1; i < lines.Length; i++)
            {
                if (!lines[i].Contains('\t'))
                    continue;

                var properWhitespaceCount = lines[i - 1].TakeWhile(char.IsWhiteSpace).Count();
                lines[i] = new string(' ', properWhitespaceCount) + lines[i].TrimStart();
            }

            return string.Join(Environment.NewLine, lines);
        }
    }
}