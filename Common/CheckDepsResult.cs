using System.Collections.Generic;

namespace Common
{
    public sealed class CheckDepsResult
    {
        public readonly SortedSet<string> NotUsedDeps;
        public readonly List<ReferenceWithCsproj> NotInDeps;
        public readonly SortedSet<string> NoYamlInstallSection;
        public readonly SortedSet<string> ConfigOverhead;

        public CheckDepsResult(SortedSet<string> notUsedDeps, List<ReferenceWithCsproj> notInDeps,
                               SortedSet<string> noYamlInstall, SortedSet<string> configOverhead)
        {
            NotUsedDeps = notUsedDeps;
            NotInDeps = notInDeps;
            NoYamlInstallSection = noYamlInstall;
            ConfigOverhead = configOverhead;
        }
    }
}
