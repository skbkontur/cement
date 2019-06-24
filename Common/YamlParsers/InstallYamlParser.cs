using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common.YamlParsers
{
    public class InstallYamlParser : ConfigurationYamlParser
    {
        public InstallYamlParser(FileInfo moduleName) : base(moduleName)
        {
        }

        public InstallYamlParser(string moduleName, string configFileContents) : base(moduleName, configFileContents)
        {
        }

        public InstallData Get(string configuration = null)
        {
            configuration = configuration ?? GetDefaultConfigurationName();
            if (!ConfigurationExists(configuration))
                throw new NoSuchConfigurationException(ModuleName, configuration);
            var defaultInstallContent = ConfigurationExists("default")
                ? GetInstallContent("default")
                : new InstallData();
            var result = GetInstallContent(configuration);
            result.InstallFiles.AddRange(defaultInstallContent.InstallFiles);
            result.InstallFiles = result.InstallFiles.Distinct().ToList();
            result.Artifacts.AddRange(defaultInstallContent.Artifacts);
            result.Artifacts = result.Artifacts.Distinct().ToList();
            result.CurrentConfigurationInstallFiles.AddRange(defaultInstallContent.CurrentConfigurationInstallFiles);
            result.CurrentConfigurationInstallFiles = result.CurrentConfigurationInstallFiles.Distinct().ToList();
            result.ExternalModules.AddRange(defaultInstallContent.ExternalModules.Where(r => !result.ExternalModules.Contains(r)));
            result.ExternalModules = result.ExternalModules.Select(m => m.Substring("module ".Length)).ToList();
            result.NuGetPackages.AddRange(defaultInstallContent.NuGetPackages.Where(r => !result.NuGetPackages.Contains(r)));
            result.NuGetPackages = result.NuGetPackages.Select(m => m.Substring("nuget ".Length)).ToList();
            return result;
        }

        private InstallData GetInstallContent(string configuration)
        {
            var result = new InstallData();
            var configQueue = new Queue<string>();
            configQueue.Enqueue(configuration);
            result.CurrentConfigurationInstallFiles.AddRange(GetAllInstallFilesFromConfig(configuration).Where(IsBuildFileName));
            while (configQueue.Count > 0)
            {
                var currentConfig = configQueue.Dequeue();
                var currentDeps = GetInstallSectionFromConfig(currentConfig, "install");
                result.InstallFiles.AddRange(currentDeps.Where(IsBuildFileName));
                result.Artifacts.AddRange(result.InstallFiles.Where(r => !result.Artifacts.Contains(r)));
                result.Artifacts.AddRange(GetInstallSectionFromConfig(currentConfig, "artifacts").Where(IsBuildFileName));
                result.ExternalModules.AddRange(currentDeps.Where(r => r.StartsWith("module ")));
                result.NuGetPackages.AddRange(currentDeps.Where(r => r.StartsWith("nuget ")));
                if (!IsInherited(currentConfig))
                    continue;
                var childConfigurations = GetParentConfigurations(currentConfig);
                if (childConfigurations == null || childConfigurations.Count == 0)
                    continue;
                foreach (var childConfig in childConfigurations)
                {
                    configQueue.Enqueue(childConfig);
                }
            }
            return result;
        }

        public List<string> GetAllInstallFilesFromConfig(string configName)
        {
            return GetInstallSectionFromConfig(configName, "install")
                .Concat(GetInstallSectionFromConfig(configName, "artifacts"))
                .ToList();
        }

        private List<string> GetInstallSectionFromConfig(string configName, string keyWord)
        {
            try
            {
                var configSection = GetConfigurationSection(configName);
                return GetInstallFromSection(keyWord, configSection);
            }
            catch (BadYamlException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new BadYamlException(ModuleName, "install", exception.Message);
            }
        }

        private static List<string> GetInstallFromSection(string keyWord, Dictionary<string, object> configSection)
        {
            if (configSection == null)
                return new List<string>();

            if (!configSection.ContainsKey(keyWord))
                return new List<string>();

            var section = configSection[keyWord];
            if (section == null || section is string)
                return new List<string>();

            var list = (List<object>) configSection[keyWord];
            var strList = list.Select(e => (string) e).ToList();
            return strList;
        }

        private bool IsBuildFileName(string line)
        {
            return !line.StartsWith("module ") && !line.StartsWith("nuget ");
        }
    }
}