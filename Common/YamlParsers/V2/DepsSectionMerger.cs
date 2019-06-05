using System;
using System.Collections.Generic;
using System.Linq;
using Common.YamlParsers.Models;
using JetBrains.Annotations;

namespace Common.YamlParsers.V2
{
    public class DepsSectionMerger
    {
        public DepsData Merge(DepsSection current, [CanBeNull]  DepsSection defaults = null, [CanBeNull] DepsSection[] parents = null)
        {
            var sections = new List<DepsSection> {defaults ?? new DepsSection() }
                .Concat(parents ?? new DepsSection[0])
                .Concat(new [] {current})
                .ToArray();

            var resultingDeps = new List<Dep>();

            foreach (var section in sections)
            {
                foreach (var line in section.SectionItems)
                {
                    if (line.IsRemoved)
                    {
                        var removedCount = resultingDeps.RemoveAll(testedDep => DepMatch(line.Dependency, testedDep));
                        if (removedCount == 0)
                            throw new BadYamlException("deps", $"You cannot delete dependency '{line.Dependency}'. You have to add it first.");
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

            var result = new DepsData(current.Force, resultingDeps.ToList());
            return result;
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