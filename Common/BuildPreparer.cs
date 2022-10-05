using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.DepsValidators;
using Common.Exceptions;
using Common.Logging;
using Common.YamlParsers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Common;

[PublicAPI]
public sealed class BuildPreparer
{
    private static readonly Dictionary<string, bool> DepConfigurationExistsCache = new();

    private readonly ILogger logger;
    private readonly ConsoleWriter consoleWriter;
    private IDepsValidatorFactory depsValidatorFactory;

    public BuildPreparer(ILogger<BuildPreparer> logger, ConsoleWriter consoleWriter, IDepsValidatorFactory depsValidatorFactory)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
    }

    public static BuildPreparer Shared { get; } = new(LogManager.GetLogger<BuildPreparer>(), ConsoleWriter.Shared, DepsValidatorFactory.Shared);

    public ModulesOrder GetModulesOrder(string moduleName, string configuration)
    {
        var modulesOrder = new ModulesOrder();
        logger.LogDebug("Building configurations graph");
        consoleWriter.WriteProgress("Building configurations graph");
        modulesOrder.ConfigsGraph = BuildConfigsGraph(moduleName, configuration);
        modulesOrder.ConfigsGraph = EraseExtraChildren(modulesOrder.ConfigsGraph);
        modulesOrder.BuildOrder = GetTopologicallySortedGraph(modulesOrder.ConfigsGraph, moduleName, configuration);

        logger.LogDebug("Getting current commit hashes");
        consoleWriter.WriteProgress("Getting current commit hashes");
        modulesOrder.CurrentCommitHashes = GetCurrentCommitHashes(modulesOrder.ConfigsGraph);
        modulesOrder.UpdatedModules = BuildInfoStorage.Deserialize().GetUpdatedModules(modulesOrder.BuildOrder, modulesOrder.CurrentCommitHashes);
        consoleWriter.ResetProgress();
        return modulesOrder;
    }

    public List<Dep> GetTopologicallySortedGraph(Dictionary<Dep, List<Dep>> graph, string root, string config, bool printCycle = true)
    {
        var visitedConfigurations = new HashSet<Dep>();
        var processingConfigs = new List<Dep>();
        var result = new List<Dep>();
        var rootDep = new Dep(root, null, config);
        TopSort(rootDep, graph, visitedConfigurations, processingConfigs, result, printCycle);
        return result;
    }

    public Dictionary<Dep, List<Dep>> BuildConfigsGraph(string moduleName, string config)
    {
        var graph = new Dictionary<Dep, List<Dep>>();
        var visitedConfigurations = new HashSet<Dep>();
        Dfs(new Dep(moduleName, null, config), graph, visitedConfigurations);
        return graph;
    }

    private static Dictionary<Dep, List<Dep>> EraseChild(Dictionary<Dep, List<Dep>> configsGraph, Dep child, Dep parrent)
    {
        var result = new Dictionary<Dep, List<Dep>>();
        foreach (var kvp in configsGraph)
        {
            if (kvp.Key.Equals(child))
                continue;
            var deps = kvp.Value.Select(to => to.Equals(child) ? parrent : to).ToList();
            result.Add(kvp.Key, deps);
        }

        return result;
    }

    private void TopSort(Dep dep, Dictionary<Dep, List<Dep>> graph, ISet<Dep> visitedConfigurations, List<Dep> processingConfigs, List<Dep> result, bool printCycle)
    {
        dep.UpdateConfigurationIfNull();
        visitedConfigurations.Add(dep);
        processingConfigs.Add(dep);

        foreach (var d in graph[dep])
        {
            d.UpdateConfigurationIfNull();
            if (processingConfigs.Contains(d))
            {
                if (!printCycle)
                    throw new CementException("Unable to build! Circular dependency found!");

                while (!processingConfigs.First().Equals(d))
                    processingConfigs = processingConfigs.Skip(1).ToList();
                processingConfigs.Add(d);
                consoleWriter.WriteLine(string.Join(" ->\n", processingConfigs));
                throw new CementException("Unable to build! Circular dependency found!");
            }

            if (!visitedConfigurations.Contains(d))
            {
                TopSort(d, graph, visitedConfigurations, processingConfigs, result, printCycle);
            }
        }

        processingConfigs.Remove(dep);
        result.Add(dep);
    }

    private void CheckAndUpdateDepConfiguration(Dep dep)
    {
        dep.UpdateConfigurationIfNull();
        var key = dep.ToString();
        if (!DepConfigurationExistsCache.ContainsKey(key))
        {
            if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, dep.Name)))
            {
                throw new CementBuildException("Failed to find module '" + dep.Name + "'");
            }

            DepConfigurationExistsCache[key] = !Yaml.Exists(dep.Name) ||
                                               Yaml.ConfigurationParser(dep.Name).ConfigurationExists(dep.Configuration);
        }

        if (!DepConfigurationExistsCache[key])
        {
            consoleWriter.WriteWarning(
                $"Configuration '{dep.Configuration}' was not found in {dep.Name}. Will take full-build config");
            dep.Configuration = "full-build";
        }
    }

    private void Dfs(Dep dep, Dictionary<Dep, List<Dep>> graph, HashSet<Dep> visitedConfigurations)
    {
        CheckAndUpdateDepConfiguration(dep);
        visitedConfigurations.Add(dep);
        graph[dep] = new List<Dep>();
        var currentDeps = new DepsParser(consoleWriter, depsValidatorFactory, Path.Combine(Helper.CurrentWorkspace, dep.Name))
            .Get(dep.Configuration).Deps ?? new List<Dep>();

        currentDeps = currentDeps.Select(d => new Dep(d.Name, null, d.Configuration)).ToList();
        foreach (var d in currentDeps)
        {
            d.UpdateConfigurationIfNull();
            graph[dep].Add(d);
            if (!visitedConfigurations.Contains(d))
            {
                Dfs(d, graph, visitedConfigurations);
            }
        }
    }

    private Dictionary<Dep, List<Dep>> EraseExtraChildren(Dictionary<Dep, List<Dep>> configsGraph)
    {
        var vertices = configsGraph.Select(e => e.Key).ToList();
        var deletedChildren = new List<Dep>();
        foreach (var parrent in vertices)
        {
            if (deletedChildren.Contains(parrent))
                continue;
            var hierarchyManager = new ConfigurationManager(parrent.Name, vertices.Where(v => v.Name == parrent.Name).ToArray());
            var childrenConfigurations = hierarchyManager.ProcessedChildrenConfigurations(parrent);
            foreach (var childConfig in childrenConfigurations)
            {
                var child = new Dep(parrent.Name, null, childConfig);
                if (deletedChildren.Contains(child))
                    continue;
                var configsGraph2 = EraseChild(configsGraph, child, parrent);
                try
                {
                    GetTopologicallySortedGraph(configsGraph2, parrent.Name, parrent.Configuration, false);
                    configsGraph = configsGraph2;
                    deletedChildren.Add(child);
                }
                catch (Exception)
                {
                    // cycle
                }
            }
        }

        return configsGraph;
    }

    private Dictionary<string, string> GetCurrentCommitHashes(Dictionary<Dep, List<Dep>> configsGraph)
    {
        var deps = configsGraph.Keys.Select(d => d.Name).Distinct().ToList();

        var result = deps.AsParallel()
            .Select(d => new KeyValuePair<string, string>(d, GetCurrentCommitHash(d)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        return result;
    }

    private string GetCurrentCommitHash(string moduleName)
    {
        try
        {
            var repo = new GitRepository(moduleName, Helper.CurrentWorkspace, logger);
            return repo.CurrentLocalCommitHash();
        }
        catch (Exception e)
        {
            consoleWriter.WriteWarning($"Failed to retrieve local commit hash for '{moduleName}': {e}");
            return null;
        }
    }
}
