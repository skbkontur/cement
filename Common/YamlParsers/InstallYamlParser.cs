﻿using System;
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

        public InstallData Get(string configuration = null)
        {
            configuration = configuration ?? GetDefaultConfigurationName();
            if (!ConfigurationExists(configuration))
                throw new NoSuchConfigurationException(ModuleName, configuration);
            var defaultInstallContent = ConfigurationExists("default")
                ? GetInstallContent("default")
                : new InstallData();
            var result = GetInstallContent(configuration);
            result.BuildFiles.AddRange(defaultInstallContent.BuildFiles);
            result.BuildFiles = result.BuildFiles.Distinct().ToList();
            result.Artifacts.AddRange(defaultInstallContent.Artifacts);
            result.Artifacts = result.Artifacts.Distinct().ToList();
            result.MainConfigBuildFiles.AddRange(defaultInstallContent.MainConfigBuildFiles);
            result.MainConfigBuildFiles = result.MainConfigBuildFiles.Distinct().ToList();
            result.ExternalModules.AddRange(defaultInstallContent.ExternalModules.Where(r => !result.ExternalModules.Contains(r)));
            result.ExternalModules = result.ExternalModules.Select(m => m.Substring("module ".Length)).ToList();
            return result;
        }

        private InstallData GetInstallContent(string configuration)
        {
            var result = new InstallData();
            var configQueue = new Queue<string>();
            configQueue.Enqueue(configuration);
            result.MainConfigBuildFiles.AddRange(GetAllInstallFilesFromConfig(configuration).Where(r => !r.StartsWith("module ")));
            while (configQueue.Count > 0)
            {
                var currentConfig = configQueue.Dequeue();
                var currentDeps = GetInstallSectionFromConfig(currentConfig, "install");
                result.BuildFiles.AddRange(currentDeps.Where(r => !r.StartsWith("module ")));
                result.Artifacts.AddRange(result.BuildFiles.Where(r => !result.Artifacts.Contains(r)));
                result.Artifacts.AddRange(GetInstallSectionFromConfig(currentConfig, "artifacts").Where(r => !r.StartsWith("module ")));
                result.Artifacts.AddRange(GetInstallSectionFromConfig(currentConfig, "artefacts").Where(r => !r.StartsWith("module ")));
                result.ExternalModules.AddRange(currentDeps.Where(r => r.StartsWith("module ")));
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
                .Concat(GetInstallSectionFromConfig(configName, "artefacts"))
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
    }
}
