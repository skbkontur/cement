using System.Collections.Generic;
using Common.Extensions;
using Common.YamlParsers.Models;

namespace Common.YamlParsers.V2
{
    public class ConfigSectionParser
    {
        private readonly ConfigSectionTitleParser configSectionTitleParser;
        private readonly InstallSectionParser installSectionParser;
        private readonly DepsSectionParser depsSectionParser;
        private readonly BuildSectionParser buildSectionParser;

        public ConfigSectionParser(
            ConfigSectionTitleParser configSectionTitleParser,
            InstallSectionParser installSectionParser,
            DepsSectionParser depsSectionParser,
            BuildSectionParser buildSectionParser)
        {
            this.configSectionTitleParser = configSectionTitleParser;
            this.installSectionParser = installSectionParser;
            this.depsSectionParser = depsSectionParser;
            this.buildSectionParser = buildSectionParser;
        }

        public ConfigSection Parse(KeyValuePair<object, object> configSection, ModuleDefaults yamlDefaults)
        {
            return Parse(configSection.Key, configSection.Value, yamlDefaults);
        }

        public ConfigSection Parse(object title, object data, ModuleDefaults yamlDefaults)
        {
            var titleAsString = (string) title;
            var dataAsDict = data as Dictionary<object, object>;
            return Parse(titleAsString, dataAsDict, yamlDefaults);
        }

        public ConfigSection Parse(string title, Dictionary<object, object> data, ModuleDefaults yamlDefaults)
        {
            var configSectionTitle = configSectionTitleParser.Parse(title);

            var installSection = data?.FindValue("install");
            var artifactsSection = data?.FindValue("artifacts");
            var installData = installSectionParser.Parse(installSection, artifactsSection, yamlDefaults?.InstallSection?.CurrentConfigurationInstallFiles);

            var depsData = data?.FindValue("deps");
            var depsSection = depsSectionParser.Parse(depsData, yamlDefaults);

            var buildSection = data?.FindValue("build");
            var buildData = buildSectionParser.ParseConfiguration(buildSection, yamlDefaults?.BuildSection);

            return new ConfigSection()
            {
                Title = configSectionTitle,
                DepsSection = depsSection,
                InstallSection = installData,
                BuildSection = buildData
            };
        }
    }
}