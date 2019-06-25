using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Extensions;
using Common.YamlParsers;
using log4net;

namespace Common
{
    public class DepsPatcher
    {
        private readonly string workspace;
        private readonly string patchModule;
        private readonly Dep patchDep;
        private readonly string yamlPath;

        public DepsPatcher(string workspace, string patchModule, Dep patchDep)
        {
            this.workspace = workspace;
            this.patchModule = patchModule;
            this.patchDep = patchDep;
            yamlPath = Path.Combine(workspace, patchModule, Helper.YamlSpecFile);
            if (!File.Exists(yamlPath))
                throw new CementException("module.yaml not found in " + yamlPath);
            ModuleYamlFile.ReplaceTabs(yamlPath);
        }

        public void Patch(string patchConfiguration)
        {
            PatchConfiguration(patchConfiguration);
            PatchDfs(patchConfiguration, new HashSet<string> {patchConfiguration});
        }

        private void PatchDfs(string patchConfiguration, HashSet<string> processed)
        {
            var childConfigurations = new ConfigurationYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName))
                .GetConfigurationsHierarchy()[patchConfiguration]
                .Where(c => !processed.Contains(c)).ToList();

            foreach (var child in childConfigurations)
            {
                PatchConfiguration(child);
                processed.Add(child);
            }
            foreach (var child in childConfigurations)
                PatchDfs(child, processed);
        }

        private void PatchConfiguration(string patchConfiguration)
        {
            if (TryReplaceInSameSection(patchConfiguration))
                return;
            if (TryReplaceFromParent(patchConfiguration))
                return;

            AddDepLine(patchConfiguration, patchDep, true);
            FixChildren(patchConfiguration);
        }

        private bool TryReplaceFromParent(string patchConfiguration)
        {
            var parser = new DepsYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName));
            var had = parser.Get(patchConfiguration).Deps.Where(d => d.Name == patchDep.Name).ToList();
            if (!had.Any())
                return false;
            if (had.Count > 1)
                ThrowDuplicate();
            var shouldBe = FindLca(patchDep, had.First());
            if (GetDepLine(had.First()) == GetDepLine(shouldBe))
                return true;
            AddDepLine(patchConfiguration, shouldBe, true);
            AddDepLine(patchConfiguration, had.First(), false);
            FixChildren(patchConfiguration);
            return true;
        }

        private bool TryReplaceInSameSection(string patchConfiguration)
        {
            var parser = new DepsYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName));
            var hadInSameSection = parser.GetDepsFromConfig(patchConfiguration).Deps.Where(d => d.Name == patchDep.Name).Distinct().ToList();
            if (!hadInSameSection.Any())
                return false;
            if (hadInSameSection.Count > 1)
                ThrowDuplicate();
            var was = hadInSameSection.First();
            var shouldBe = FindLca(patchDep, was);
            ReplaceDepLine(patchConfiguration, was, shouldBe);
            FixChildren(patchConfiguration);
            return true;
        }

        private void FixChildren(string patchConfiguration)
        {
            var parser = new DepsYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName));
            var hadDepsInThisSectionAndParrents = parser.Get(patchConfiguration).Deps.Where(d => d.Name == patchDep.Name).ToList();
            if (!hadDepsInThisSectionAndParrents.Any())
                return;
            if (hadDepsInThisSectionAndParrents.Count > 1)
                ThrowDuplicate();
            var currentSectionDep = hadDepsInThisSectionAndParrents.First();
            var childConfigurations = new ConfigurationYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName))
                .GetConfigurationsHierarchy()[patchConfiguration];
            foreach (var child in childConfigurations)
                FixChild(currentSectionDep, child);
            foreach (var child in childConfigurations)
                FixChildren(child);
        }

        private void FixChild(Dep currentParrentDep, string childConfiguration)
        {
            RemoveDepLine(childConfiguration, currentParrentDep, false);
            var parser = new DepsYamlParser(new FileInfo(Directory.GetParent(yamlPath).FullName));
            var hadInSameSection = parser.GetDepsFromConfig(childConfiguration).Deps.Where(d => d.Name == patchDep.Name).ToList();
            if (!hadInSameSection.Any())
                return;
            if (hadInSameSection.Count > 1)
                ThrowDuplicate();
            var had = hadInSameSection.First();
            RemoveDepLine(childConfiguration, had, true);
            if (GetDepLine(had) == GetDepLine(currentParrentDep))
                return;
            if (FindLca(had, currentParrentDep).Configuration == currentParrentDep.Configuration)
                return;
            AddDepLine(childConfiguration, had, true);
            AddDepLine(childConfiguration, currentParrentDep, false);
        }

        private Dep FindLca(Dep dep1, Dep dep2)
        {
            if (dep1.Name != dep2.Name)
                throw new BadArgumentException();
            var result = new Dep(dep1.Name, dep1.Treeish ?? dep2.Treeish);

            var configParser = new ConfigurationYamlParser(new FileInfo(Path.Combine(workspace, dep1.Name)));
            var configs = configParser.GetConfigurations();
            var commonAncestorsList = new List<string>();
            foreach (var parent in configs)
            {
                var cm = new ConfigurationManager(new[] {new Dep(null, null, parent)}, configParser);
                if (cm.ProcessedParent(dep1) && cm.ProcessedParent(dep2))
                    commonAncestorsList.Add(parent);
            }

            var lowestAncestor = commonAncestorsList.Where(c =>
                !new ConfigurationManager(commonAncestorsList.Select(cc => new Dep(null, null, cc)), configParser).ProcessedChildrenConfigurations(new Dep(null, null, c)).Any()).ToList();

            if (!commonAncestorsList.Any() || !lowestAncestor.Any())
                throw new CementException("failed get common ancestor for configurations '" + dep1 + "' and '" + dep2 + "'");
            result.Configuration = lowestAncestor.First();
            return result;
        }

        private string GetDepLine(Dep dep)
        {
            var parser = new ConfigurationYamlParser(new FileInfo(Path.Combine(workspace, dep.Name)));
            if (parser.GetDefaultConfigurationName() == dep.Configuration)
                dep.Configuration = null;
            return dep.ToYamlString();
        }

        private void RemoveDepLine(string configuration, Dep dep, bool isOn)
        {
            var path = Path.Combine(workspace, patchModule, Helper.YamlSpecFile);
            var moduleYamlFile = new ModuleYamlFile(new FileInfo(path));
            var lines = moduleYamlFile.Lines;
            var configIndex = lines.FindIndex(l => l.StartsWith(configuration + ":")
                                                   || l.StartsWith(configuration + " "));
            var depsIndex = -1;

            for (int index = configIndex + 1; index < lines.Count && lines[index].StartsWith(" "); index++)
                if (lines[index].EndsWith("deps:"))
                {
                    depsIndex = index;
                    break;
                }
            if (depsIndex == -1)
                return;
            for (int index = depsIndex + 1; index < lines.Count && lines[index].StartsWith(GetSpacesStart(lines, depsIndex)); index++)
            {
                var str = (isOn ? " - " : " - -") + dep.Name;
                if (lines[index].Contains(str + "/") || lines[index].Contains(str + "@") || lines[index].EndsWith(str))
                {
                    lines.RemoveAt(index);
                    break;
                }
            }
            moduleYamlFile.Save(path, lines);
        }

        private void AddDepLine(string configuration, Dep dep, bool isOn)
        {
            var path = Path.Combine(workspace, patchModule, Helper.YamlSpecFile);
            var moduleYamlFile = new ModuleYamlFile(new FileInfo(path));
            var lines = moduleYamlFile.Lines;
            var configIndex = lines.FindIndex(l => l.StartsWith(configuration + ":")
                                                   || l.StartsWith(configuration + " "));
            var depsIndex = -1;

            for (int index = configIndex + 1; index < lines.Count && (lines[index].Length == 0 || lines[index].StartsWith(" ")); index++)
                if (lines[index].EndsWith("deps:"))
                {
                    depsIndex = index;
                    break;
                }
            if (depsIndex == -1)
            {
                lines.Insert(configIndex + 1, GetSpacesStart(lines, configIndex) + "deps:");
                depsIndex = configIndex + 1;
            }
            var prefix = GetSpacesStart(lines, depsIndex) + (isOn ? "- " : "- -");
            lines.Insert(depsIndex + 1, prefix + GetDepLine(dep));
            moduleYamlFile.Save(path, lines);
        }

        private void ReplaceDepLine(string patchConfiguration, Dep was, Dep shouldBe)
        {
            var path = Path.Combine(workspace, patchModule, Helper.YamlSpecFile);
            var moduleYamlFile = new ModuleYamlFile(new FileInfo(path));
            var lines = moduleYamlFile.Lines;
            var configIndex = lines.FindIndex(l => l.StartsWith(patchConfiguration + ":")
                                                   || l.StartsWith(patchConfiguration + " "));

            var depsIndex = lines.FindIndex(configIndex, line => line.EndsWith("deps:"));
            var removeIndex = lines.FindIndex(depsIndex, line =>
                line.Contains(" " + was.Name + "/") ||
                line.Contains(" " + was.Name + "@") ||
                line.Contains(" " + was.Name + ":") ||
                line.EndsWith(" " + was.Name));
            ReplaceDepLine(was, shouldBe, removeIndex, lines, depsIndex);
            moduleYamlFile.Save(path, lines);
        }

        private void ReplaceDepLine(Dep was, Dep shouldBe, int removeIndex, List<string> lines, int depsIndex)
        {
            if (removeIndex == -1)
            {
                ConsoleWriter.WriteWarning("Fail to replace " + was + ", not found in module.yaml.");
                return;
            }
            var suffix = lines[removeIndex].EndsWith(":") ? ":" : "";
            lines[removeIndex] = GetSpacesStart(lines, depsIndex) + "- " + GetDepLine(shouldBe) + suffix;
        }

        private string GetSpacesStart(List<string> lines, int depsIndex)
        {
            int count = CalcStartSpacesCount(lines[depsIndex]) + 2;
            if (depsIndex + 1 < lines.Count)
                count = Math.Max(count, CalcStartSpacesCount(lines[depsIndex + 1]));
            return new string(' ', count);
        }

        private int CalcStartSpacesCount(string str)
        {
            int res = 0;
            while (res < str.Length && str[res] == ' ')
                res++;
            return res;
        }

        private void ThrowDuplicate()
        {
            throw new BadYamlException(patchModule, "deps", "duplicate dep: " + patchDep.Name);
        }
    }

    public static class DepsPatcherProject
    {
        private static readonly ILog Log = new PrefixAppender("DEPS PATCH", LogManager.GetLogger("FixReferences"));

        public static void PatchDepsForProject(string currentModuleFullPath, Dep dep, string csprojFile)
        {
            var installData = InstallParser.Get(dep.Name, dep.Configuration);
            Log.Info("Adding deps to module.yaml");
            Log.Info("Getting cement configurations insert to");
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
            Log.Info("Adding deps to module.yaml");
            Log.Info("Getting cement configurations insert to");
            var usedConfigs = GetUsedCementConfigsForSolution(currentModuleFullPath, solutionFile);
            var toPatch = GetSmallerCementConfigs(currentModuleFullPath, usedConfigs);

            PatchDeps(
                currentModuleFullPath,
                installData.ExternalModules.Concat(new[] {dep.ToYamlString()}).ToList(),
                toPatch);
        }

        private static readonly HashSet<KeyValuePair<string, string>> patchedDeps = new HashSet<KeyValuePair<string, string>>();

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
                        ConsoleWriter.WriteError($"Fail adding {dep} to {moduleName}/{config} deps:\n\t{exception.Message}");
                        Log.Error($"Fail adding {dep} to deps: {exception.Message}");
                    }
                }
            }
        }

        public static List<string> GetSmallerCementConfigs(string modulePath, List<string> usedCementConfigs)
        {
            var configParser = new ConfigurationYamlParser(new FileInfo(modulePath));
            var configManager = new ConfigurationManager(usedCementConfigs.Select(c => new Dep("", null, c)), configParser);
            return usedCementConfigs
                .Where(c => !configManager.ProcessedChildrenConfigurations(new Dep("", null, c)).Any())
                .ToList();
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