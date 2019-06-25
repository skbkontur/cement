using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class DepsSectionParser
    {
        private readonly DepSectionItemParser depSectionItemParser;

        public DepsSectionParser(DepSectionItemParser depSectionItemParser)
        {
            this.depSectionItemParser = depSectionItemParser;
        }
        public DepsSection Parse([CanBeNull] object depLines, [CanBeNull] ModuleDefaults yamlDefaults = null)
        {
            var depLinesAsList = CastContent(depLines);
            var defaultForceBranches = yamlDefaults?.DepsSection?.Force;
            return Parse(depLinesAsList, defaultForceBranches);
        }

        public DepsSection Parse([CanBeNull] IEnumerable<object> depLines, [CanBeNull] string[] defaultForceBranches = null)
        {
            var forceBranches = defaultForceBranches;

            if (depLines == null)
                return new DepsSection(forceBranches);

            var deps = new List<DepSectionItem>();
            foreach (var node in depLines)
            {
                switch (node)
                {
                    case string scalar:
                        var parsed = depSectionItemParser.Parse(scalar);
                        deps.Add(parsed);
                        break;

                    case Dictionary<object, object> mappingNode:
                    {
                        var mapping = Transform(mappingNode);
                        var firstKvp = mapping.FirstOrDefault();
                        var isForceKeyword = mapping.Count == 1 && firstKvp.Key == "force";

                        if (isForceKeyword && !string.IsNullOrWhiteSpace(firstKvp.Value))
                        {
                            forceBranches = firstKvp.Value
                                .Split(',')
                                .Select(branch => branch.TrimStart())
                                .ToArray();
                        }
                        else
                        {

                            var rawName = mapping.First(c => string.IsNullOrWhiteSpace(c.Value)).Key;
                            var parsedName = depSectionItemParser.Parse(rawName);
                            var treeish = FindValue(mapping, "treeish", parsedName.Dependency.Treeish);
                            var type = FindValue(mapping, "type", null);
                            var configuration = FindValue(mapping, "configuration", parsedName.Dependency.Configuration);

                            var dep = new Dep(parsedName.Dependency.Name, treeish, configuration)
                            {
                                NeedSrc = type == "src"
                            };
                            deps.Add(new DepSectionItem(parsedName.IsRemoved, dep));
                        }

                        break;
                    }
                }
            }

            return new DepsSection(forceBranches, deps.ToArray());
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
            return dict.ToDictionary(kvp => (string)kvp.Key, kvp => (string)kvp.Value);
        }
    }
}