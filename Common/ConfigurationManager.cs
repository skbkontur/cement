using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Common
{
    public class ConfigurationManager
    {
        private Dictionary<string, IList<string>> configHierarchy;
        public readonly List<string> ProcessedDeps;
        private readonly IConfigurationParser parser;


        public ConfigurationManager(string moduleName, IEnumerable<Dep> processedConfigurations)
        {
            ProcessedDeps = processedConfigurations.Select(dep => dep.Configuration).ToList();
            parser = new ConfigurationParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
        }

        public ConfigurationManager(string moduleName, IEnumerable<string> processedConfigurations)
        {
            ProcessedDeps = processedConfigurations.ToList();
            parser = new ConfigurationParser(new FileInfo(Path.Combine(Helper.CurrentWorkspace, moduleName)));
        }

        public ConfigurationManager(IEnumerable<Dep> processedDeps, IConfigurationParser parser)
        {
            ProcessedDeps = processedDeps.Select(dep => dep.Configuration).ToList();
            this.parser = parser;
        }

        public bool ProcessedParent(Dep dep)
        {
            configHierarchy = parser.GetConfigurationsHierarchy();
            var config = dep.Configuration ?? parser.GetDefaultConfigurationName();
            return ProcessedParentDfs(config);
        }

        public List<string> ProcessedChildrenConfigurations(Dep dep)
        {
            configHierarchy = ReverseHierarchy(parser.GetConfigurationsHierarchy());
            var config = dep.Configuration ?? parser.GetDefaultConfigurationName();
            if (config == null || !configHierarchy.ContainsKey(config))
                return new List<string>();

            var children = new HashSet<string>();
            GetChildrenDfs(dep.Configuration, children);
            return children.Where(c => ProcessedDeps.Contains(c) && dep.Configuration != c).ToList();
        }

        private void GetChildrenDfs(string configuration, HashSet<string> usedVertexes)
        {
            usedVertexes.Add(configuration);
            foreach (var child in configHierarchy[configuration])
            {
                if (usedVertexes.Contains(child))
                    continue;
                usedVertexes.Add(child);
                GetChildrenDfs(child, usedVertexes);
            }
        }

        private Dictionary<string, IList<string>> ReverseHierarchy(Dictionary<string, IList<string>> configurationsHierarchy)
        {
            var reversedHierarchy = new Dictionary<string, IList<string>>();
            foreach (var from in configurationsHierarchy.Keys)
            {
                if (!reversedHierarchy.ContainsKey(from))
                {
                    reversedHierarchy[from] = new List<string>();
                }
                foreach (var to in configurationsHierarchy[from])
                {
                    if (!reversedHierarchy.ContainsKey(to))
                        reversedHierarchy[to] = new List<string>();
                    reversedHierarchy[to].Add(from);
                }
            }
            return reversedHierarchy;
        }

        private bool ProcessedParentDfs(string config)
        {
            return ProcessedDeps.Contains(config) || config != null && configHierarchy.ContainsKey(config) && configHierarchy[config].Any(ProcessedParentDfs);
        }
    }
}