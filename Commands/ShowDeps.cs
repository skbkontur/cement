using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.YamlParsers;

namespace Commands
{
    public class ShowDeps : Command
    {
        private static readonly Dictionary<Dep, List<string>> overheadCache = new Dictionary<Dep, List<string>>();
        private readonly ArborJs arborJs;
        private string configuration;

        public ShowDeps()
            : base(
                new CommandSettings
                {
                    LogPerfix = "SHOW-DEPS",
                    LogFileName = null,
                    MeasureElapsedTime = false,
                    Location = CommandSettings.CommandLocation.RootModuleDirectory
                })
        {
            arborJs = new ArborJs();
        }

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

        protected override void ParseArgs(string[] args)
        {
            var parsed = ArgumentParser.ParseDepsGraph(args);
            configuration = (string)parsed["configuration"];
        }

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);

            var result = GetDepsGraph(new Dep(moduleName, null, configuration));

            var full = moduleName;
            if (!string.IsNullOrEmpty(configuration))
                full += "/" + configuration;

            arborJs.Show(full, result);
            return 0;
        }

        private static void Bfs(Dep root, List<string> tree)
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

        private static void AddEdge(Dep from, Dep to, List<string> result)
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

        private static List<string> GetOverhead(Dep dep)
        {
            if (overheadCache.ContainsKey(dep))
                return overheadCache[dep];

            var checker = new DepsChecker(
                Path.Combine(Helper.CurrentWorkspace, dep.Name),
                dep.Configuration,
                Helper.GetModules());

            var overhead = checker.GetCheckDepsResult(false).ConfigOverhead;
            var names = overhead.Select(path => path.Split('/', '\\').FirstOrDefault()).Distinct().ToList();
            return overheadCache[dep] = names;
        }

        private static List<Dep> GetDeps(Dep root)
        {
            var deps = new DepsParser(
                    Path.Combine(Helper.CurrentWorkspace, root.Name))
                .Get(root.Configuration).Deps ?? new List<Dep>();
            foreach (var dep in deps)
            {
                dep.UpdateConfigurationIfNull();
                dep.Treeish = null;
            }

            return deps;
        }
    }
}
