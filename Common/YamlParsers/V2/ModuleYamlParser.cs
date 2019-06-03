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

        public ModuleDefinition Parse(string content, string moduleInfo = "")
        {
            content = PatchTabs(content, moduleInfo);
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

            var hierarchy = ConfigurationHierarchyFactory.Get(configLines.ToArray());
            var configs = hierarchy.GetAll();

            var defaultSection = yaml.FindValue("default") as Dictionary<object, object>;
            var defaults = moduleYamlDefaultsParser.Parse(defaultSection);
            var defaultConfigName = hierarchy.FindDefault();

            var cache = new Dictionary<string, ParsedDepsSection>();

            foreach (var configName in configs)
            {
                var allParents = hierarchy.GetAllParents(configName);
                var configKey = parsedConfigToRawLine[configName];

                var configurationContents = yaml[configKey] as Dictionary<object, object>;

                var parentInstalls = allParents?.Select(c => configurations[c].InstallSection).ToArray();
                var parentDeps = allParents?
                    .Select(c => cache[c])
                    .ToArray();

                var installSection = configurationContents?.FindValue("install");
                var artifactsSection = configurationContents?.FindValue("artifacts");
                var artefactsSection = configurationContents?.FindValue("artefacts");
                var depsSection = configurationContents?.FindValue("deps");
                var buildSection = configurationContents?.FindValue("build");
                var sections = new YamlInstallSections(installSection, artifactsSection, artefactsSection);

                try
                {
                    var depsParseResult = depsSectionParser.Parse(depsSection, defaults?.DepsSection, parentDeps);
                    cache[configName] = depsParseResult.RawSection;

                    var result = new ModuleConfiguration
                    {
                        InstallSection = installSectionParser.Parse(sections, defaults?.InstallSection, parentInstalls),
                        Dependencies = depsParseResult.ResultingDeps,
                        BuildSection = buildSectionParser.ParseConfiguration(buildSection, defaults?.BuildSection),
                        Name = configName,
                        IsDefault = configName == defaultConfigName,
                    };
                    configurations[configName] = result;
                }
                catch (BadYamlException ex)
                {
                    throw new BadYamlException(configName + "." + ex.SectionName, ex.Message);
                }
            }

            return new ModuleDefinition(configurations, defaults ?? new ModuleDefaults());
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