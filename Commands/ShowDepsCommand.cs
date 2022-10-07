using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.DepsValidators;
using Common.YamlParsers;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class ShowDepsCommand : Command<ShowDepsCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "show-deps",
        Location = CommandLocation.RootModuleDirectory
    };
    private readonly IDepsValidatorFactory depsValidatorFactory;
    private readonly ConsoleWriter consoleWriter;
    private readonly Dictionary<Dep, List<string>> overheadCache = new();
    private readonly ArborJs arborJs;

    public ShowDepsCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IDepsValidatorFactory depsValidatorFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.depsValidatorFactory = depsValidatorFactory;
        arborJs = new ArborJs();
    }

    public override string Name => "show-deps";
    public override string HelpMessage => @"
    Shows module deps in arbor.js

    Usage:
        cm show-deps [-c <config-name>]
";

    public List<string> GetDepsGraph(Dep dep)
    {
        var tree = new List<string>();
        dep.Configuration = dep.Configuration ?? "full-build";
        Bfs(dep, tree);

        tree.Insert(0, $"{dep} {{color:red}}");
        tree.Insert(0, "{color:DimGrey}");
        tree.Insert(0, ";Copy paste this text to http://arborjs.org/halfviz/#");
        return tree;
    }

    protected override ShowDepsCommandOptions ParseArgs(string[] args)
    {
        var parsed = ArgumentParser.ParseDepsGraph(args);
        var configuration = (string)parsed["configuration"];
        return new ShowDepsCommandOptions(configuration);
    }

    protected override int Execute(ShowDepsCommandOptions options)
    {
        var cwd = Directory.GetCurrentDirectory();
        var moduleName = Path.GetFileName(cwd);

        var configuration = options.Configuration;
        var result = GetDepsGraph(new Dep(moduleName, null, configuration));

        var full = moduleName;
        if (!string.IsNullOrEmpty(configuration))
            full += "/" + configuration;

        arborJs.Show(full, result);
        return 0;
    }

    private static void ColorDep(List<string> tree, Dep dep, string color)
    {
        tree.Add($"{dep} {{color:{color}}}");
    }

    private static bool WasSmallerConfig(IEnumerable<Dep> deps, Dep dep)
    {
        var withSameName = deps.Where(d => d.Name == dep.Name);
        var configurationManager = new ConfigurationManager(dep.Name, withSameName);
        if (configurationManager.ProcessedParent(dep))
            return true;
        return false;
    }

    private List<Dep> GetDeps(Dep root)
    {
        var deps = new DepsParser(
                consoleWriter, depsValidatorFactory, Path.Combine(Helper.CurrentWorkspace, root.Name))
            .Get(root.Configuration).Deps ?? new List<Dep>();
        foreach (var dep in deps)
        {
            dep.UpdateConfigurationIfNull();
            dep.Treeish = null;
        }

        return deps;
    }

    private void Bfs(Dep root, List<string> tree)
    {
        var used = new HashSet<Dep>();
        var queue = new Queue<Dep>();
        queue.Enqueue(root);
        used.Add(root);

        var rootDeps = GetDeps(root);
        foreach (var dep in rootDeps)
        {
            ColorDep(tree, dep, "green");
        }

        while (queue.Any())
        {
            var dep = queue.Dequeue();
            var currentDeps = GetDeps(dep);

            foreach (var to in currentDeps)
            {
                if (used.Contains(to))
                    continue;
                if (WasSmallerConfig(used, to))
                    continue;

                var rootDep = rootDeps.FirstOrDefault(d => d.Name == to.Name && !d.Equals(to));
                if (rootDep != null && WasSmallerConfig(new[] {to}, rootDep))
                    ColorDep(tree, to, "GoldenRod");

                AddEdge(dep, to, tree);
                used.Add(to);
                queue.Enqueue(to);
            }
        }
    }

    private void AddEdge(Dep from, Dep to, List<string> result)
    {
        var edge = $"{from} -> {to}";

        if (Yaml.Exists(from.Name))
        {
            var names = GetOverhead(from);
            if (names.Contains(to.Name))
                edge += " {color:red, weight:2}";
        }

        result.Add(edge);
    }

    private List<string> GetOverhead(Dep dep)
    {
        if (overheadCache.ContainsKey(dep))
            return overheadCache[dep];

        var checker = new DepsChecker(
            ConsoleWriter, depsValidatorFactory,
            Path.Combine(Helper.CurrentWorkspace, dep.Name),
            dep.Configuration,
            Helper.GetModules());

        var overhead = checker.GetCheckDepsResult(false).ConfigOverhead;
        var names = overhead.Select(path => path.Split('/', '\\').FirstOrDefault()).Distinct().ToList();
        return overheadCache[dep] = names;
    }
}
