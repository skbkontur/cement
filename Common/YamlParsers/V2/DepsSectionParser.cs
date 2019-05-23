using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class DepsSectionParser
    {
        private readonly DepLineParser depLineParser;

        public DepsSectionParser(DepLineParser depLineParser)
        {
            this.depLineParser = depLineParser;
        }

        public DepsContent Parse(object contents, DepsContent defaults = null, [CanBeNull] Dep[] parentDeps = null)
        {
            var castedContent = CastContent(contents);

            var section = ParseSection(castedContent, defaults?.Force);
            var inheritedDeps = (defaults?.Deps ?? new List<Dep>()).Concat(parentDeps ?? new Dep[0]).ToArray();
            EnsureNoDuplicates(inheritedDeps);
            var resultingDeps = inheritedDeps.ToDictionary(d => d.Name);

            foreach (var dep in section.Deps)
            {
                var isRemoved = dep.Name[0] == '-';
                var name = isRemoved ? dep.Name.Substring(1) : dep.Name;
                if (isRemoved)
                {
                    if (!resultingDeps.ContainsKey(name))
                        throw new BadYamlException("deps", $"You cannot delete dependecy '{name}'. You have to add it first.");

                    resultingDeps.Remove(name);
                }
                else
                {
                    if (resultingDeps.ContainsKey(name))
                    {
                        ConsoleWriter.WriteError(ModuleDuplicationError(name));
                        throw new BadYamlException("deps", "duplicate dep " + name);
                    }
                    resultingDeps.Add(name, dep);
                }
            }

            return new DepsContent(section.Force, resultingDeps.Values.ToList());
        }

        private DepsContent ParseSection(IEnumerable<object> contents, [CanBeNull] string[] defaultForcedBranches = null)
        {
            var deps = new List<Dep>();
            var force = defaultForcedBranches;

            foreach(var node in contents)
            {
                switch (node)
                {
                    case string scalar:
                        deps.Add(depLineParser.Parse(scalar));
                        break;

                    case Dictionary<object, object> mappingNode:
                    {
                        var mapping = Transform(mappingNode);
                        var firstKvp = mapping.FirstOrDefault();
                        var isForceKeyword = mapping.Count == 1 && firstKvp.Key == "force";

                        if (isForceKeyword && !string.IsNullOrWhiteSpace(firstKvp.Value))
                        {
                            force = firstKvp.Value.Split(new[] {',' , ' '}, StringSplitOptions.RemoveEmptyEntries);
                        }
                        else
                        {
                            var name = mapping.First(c => string.IsNullOrWhiteSpace(c.Value)).Key;
                            var treeish = FindValue(mapping, "treeish");
                            var type = FindValue(mapping, "type");
                            var configuration = FindValue(mapping, "configuration");

                            deps.Add(new Dep(name, treeish, configuration)
                            {
                                NeedSrc = type == "src"
                            });
                        }
                        break;
                    }
                }
            }
            return new DepsContent(force, deps);
        }

        private void EnsureNoDuplicates(IEnumerable<Dep> parentDeps)
        {
            var duplicatedDeps = parentDeps
                .GroupBy(d => d.Name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToArray();

            if (!duplicatedDeps.Any())
                return;

            ConsoleWriter.WriteError(ModuleDuplicationError(duplicatedDeps));
            throw new BadYamlException("deps", "duplicate dep " + string.Join(",", duplicatedDeps));
        }

        private IEnumerable<object> CastContent(object contents)
        {
            switch (contents)
            {
                case null:
                    return null;
                case string _:
                    return Enumerable.Empty<object>();
                case IEnumerable<object> t:
                    return t;
                default:
                    throw new Exception("Internal error: unexpected dep-section contents");
            }
        }

        private string FindValue(IReadOnlyDictionary<string, string> dict, string key)
        {
            return dict.ContainsKey(key) ? dict[key] : default(string);
        }

        private Dictionary<string, string> Transform(Dictionary<object, object> dict)
        {
            return dict.ToDictionary(kvp => (string) kvp.Key, kvp => (string) kvp.Value);
        }

        private string ModuleDuplicationError(params string[] modules) => $@"Module duplication found in 'module.yaml' for dep {string.Join(",", modules)}. To depend on different variations of same dep, you must turn it off.
Example:
client:
  dep:
    - {modules[0]}/client
sdk:
  dep:
    - -{modules[0]}
    - {modules[0]}/full-build";
    }
}