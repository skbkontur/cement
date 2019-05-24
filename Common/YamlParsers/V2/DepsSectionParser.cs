using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
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

        public ParseDepsSectionResult Parse(object contents, ParsedDepsSection defaults = null, [CanBeNull] ParsedDepsSection[] parentDeps = null)
        {
            var castedContent = CastContent(contents);
            var currentSection = ParseSection(castedContent, defaults?.Force);
            defaults = defaults ?? new ParsedDepsSection();
            parentDeps = parentDeps ?? new ParsedDepsSection[0];

            var sections = new List<ParsedDepsSection> {defaults}
                .Concat(parentDeps)
                .Concat(new [] {currentSection})
                .ToArray();

            var resultingDeps = new List<Dep>();

            foreach (var section in sections)
            {
                foreach (var line in section.Lines)
                {
                    if (line.IsRemoved)
                    {
                        var removedCount = resultingDeps.RemoveAll(testedDep => DepMatch(line.Dependency, testedDep));
                        if (removedCount == 0)
                            throw new BadYamlException("deps", $"You cannot delete dependecy '{line.Dependency}'. You have to add it first.");
                    }
                    else
                    {
                        // Two duplicate deps is normal
                        // Two deps with same name and different configuration/branch - is not
                        if (!resultingDeps.Contains(line.Dependency))
                            resultingDeps.Add(line.Dependency);
                    }
                }
            }

            EnsureNoDuplicates(resultingDeps);

            var resultingDepsContent = new DepsContent(currentSection.Force, resultingDeps.ToList());
            return new ParseDepsSectionResult()
            {
                RawSection = currentSection,
                ResultingDeps = resultingDepsContent
            };
        }

        [NotNull]
        private ParsedDepsSection ParseSection([CanBeNull] IEnumerable<object> contents, [CanBeNull] string[] defaultForcedBranches = null)
        {
            var deps = new List<DepLine>();
            var force = defaultForcedBranches;

            if (contents == null)
                return new ParsedDepsSection(force);

            foreach(var node in contents)
            {
                switch (node)
                {
                    case string scalar:
                        var parsed = depLineParser.Parse(scalar);
                        deps.Add(parsed);
                        break;

                    case Dictionary<object, object> mappingNode:
                    {
                        var mapping = Transform(mappingNode);
                        var firstKvp = mapping.FirstOrDefault();
                        var isForceKeyword = mapping.Count == 1 && firstKvp.Key == "force";

                        if (isForceKeyword && !string.IsNullOrWhiteSpace(firstKvp.Value))
                        {
                            force = firstKvp.Value
                                .Split(',')
                                .Select(branch => branch.TrimStart())
                                .ToArray();
                        }
                        else
                        {

                            var rawName = mapping.First(c => string.IsNullOrWhiteSpace(c.Value)).Key;
                            var parsedName = depLineParser.Parse(rawName);
                            var treeish = FindValue(mapping, "treeish", parsedName.Dependency.Treeish);
                            var type = FindValue(mapping, "type", null);
                            var configuration = FindValue(mapping, "configuration", parsedName.Dependency.Configuration);

                            var dep = new Dep(parsedName.Dependency.Name, treeish, configuration)
                            {
                                NeedSrc = type == "src"
                            };
                            deps.Add(new DepLine(parsedName.IsRemoved, dep));
                        }
                        break;
                    }
                }
            }
            return new ParsedDepsSection(force, deps.ToArray());
        }

        private bool DepMatch(Dep depToRemove, Dep testedDep)
        {
            if (depToRemove.Name != testedDep.Name)
                return false;

            var treeishMatch = false;
            var configMatch = false;

            if (depToRemove.Treeish == null || depToRemove.Treeish.Equals("*") || depToRemove.Treeish.Equals(testedDep.Treeish))
                treeishMatch = true;
            if (depToRemove.Configuration == null || depToRemove.Configuration.Equals("*") || depToRemove.Configuration.Equals(testedDep.Configuration))
                configMatch = true;
            return treeishMatch && configMatch;
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

        [CanBeNull]
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

        private string FindValue(IReadOnlyDictionary<string, string> dict, string key, string defaultValue)
        {
            return dict.ContainsKey(key) ? dict[key] : defaultValue;
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