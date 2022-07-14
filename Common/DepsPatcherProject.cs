﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Extensions;
using Common.Logging;
using Common.YamlParsers;
using Microsoft.Extensions.Logging;

namespace Common
{
    //TODO: переименовать и порефакторить - класс перегружено выглядит
    public static class DepsPatcherProject
    {
        private static readonly ILogger Log = LogManager.GetLogger<DepsPatcher>();

        private static readonly HashSet<KeyValuePair<string, string>> patchedDeps = new();

        public static void PatchDepsForProject(string currentModuleFullPath, Dep dep, string csprojFile)
        {
            var installData = InstallParser.Get(dep.Name, dep.Configuration);
            Log.LogInformation("Adding deps to module.yaml");
            Log.LogInformation("Getting cement configurations insert to");
            var usedConfigs = GetUsedCementConfigsForProject(currentModuleFullPath, csprojFile);
            var toPatch = GetSmallerCementConfigs(currentModuleFullPath, usedConfigs);

            PatchDeps(
                currentModuleFullPath,
                installData.ExternalModules.Concat(new[] {dep.ToYamlString()}).ToList(),
                toPatch);
        }

        public static void PatchDepsForSolution(string currentModuleFullPath, Dep dep, string solutionFile)
        {
            var installData = InstallParser.Get(dep.Name, dep.Configuration);
            Log.LogInformation("Adding deps to module.yaml");
            Log.LogInformation("Getting cement configurations insert to");
            var usedConfigs = GetUsedCementConfigsForSolution(currentModuleFullPath, solutionFile);
            var toPatch = GetSmallerCementConfigs(currentModuleFullPath, usedConfigs);

            PatchDeps(
                currentModuleFullPath,
                installData.ExternalModules.Concat(new[] {dep.ToYamlString()}).ToList(),
                toPatch);
        }

        public static List<string> GetSmallerCementConfigs(string modulePath, List<string> usedCementConfigs)
        {
            var configParser = new ConfigurationYamlParser(new FileInfo(modulePath));
            var configManager = new ConfigurationManager(usedCementConfigs.Select(c => new Dep("", null, c)), configParser);
            return usedCementConfigs
                .Where(c => !configManager.ProcessedChildrenConfigurations(new Dep("", null, c)).Any())
                .ToList();
        }

        private static void PatchDeps(string currentModuleFullPath, List<string> modulesToInsert, List<string> cementConfigs)
        {
            foreach (var config in cementConfigs)
            {
                foreach (var module in modulesToInsert)
                {
                    var dep = new Dep(module);
                    var workspace = Directory.GetParent(currentModuleFullPath).FullName;
                    var moduleName = Path.GetFileName(currentModuleFullPath);

                    var kvp = new KeyValuePair<string, string>(Path.Combine(workspace, moduleName, config), module);
                    if (patchedDeps.Contains(kvp))
                        continue;
                    patchedDeps.Add(kvp);

                    try
                    {
                        new DepsPatcher(workspace, moduleName, dep).Patch(config);
                    }
                    catch (Exception exception)
                    {
                        ConsoleWriter.Shared.WriteError($"Fail adding {dep} to {moduleName}/{config} deps:\n\t{exception.Message}");
                        Log.LogError($"Fail adding {dep} to deps: {exception.Message}");
                    }
                }
            }
        }

        private static List<string> GetUsedCementConfigsForSolution(string modulePath, string solutionFile)
        {
            var moduleYaml = Path.Combine(modulePath, Helper.YamlSpecFile);
            if (!File.Exists(moduleYaml))
                return new List<string>();

            var configurations = new ConfigurationParser(new FileInfo(modulePath)).GetConfigurations();
            var result = new HashSet<string>();
            foreach (var config in configurations)
            {
                var buildData = new BuildYamlParser(new FileInfo(modulePath)).Get(config);
                foreach (var data in buildData)
                {
                    if (data.Target.IsFakeTarget())
                        continue;
                    var dataTargetPath = Path.Combine(modulePath, data.Target);
                    if (dataTargetPath == solutionFile)
                        result.Add(config);
                }
            }

            return result.ToList();
        }

        private static List<string> GetUsedCementConfigsForProject(string modulePath, string csprojFile)
        {
            var projectPath = Path.GetFullPath(csprojFile);
            var moduleYaml = Path.Combine(modulePath, Helper.YamlSpecFile);
            if (!File.Exists(moduleYaml))
                return new List<string>();

            var configurations = new ConfigurationParser(new FileInfo(modulePath)).GetConfigurations();
            var result = new HashSet<string>();

            foreach (var config in configurations)
            {
                var buildData = new BuildYamlParser(new FileInfo(modulePath)).Get(config);
                foreach (var data in buildData)
                {
                    if (data.Target.IsFakeTarget())
                        continue;
                    var slnPath = Path.Combine(modulePath, data.Target);
                    var solutionParser = new VisualStudioProjectParser(slnPath, new List<string>());
                    var currentConfigs =
                        solutionParser.GetSolutionConfigsByCsproj(projectPath);
                    if (currentConfigs.Contains(data.Configuration))
                        result.Add(config);
                }
            }

            return result.ToList();
        }
    }
}