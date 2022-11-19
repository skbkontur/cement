using System.Collections.Generic;

namespace Cement.Cli.Common;

public sealed class CheckDepsResult
{
    public CheckDepsResult(SortedSet<string> notUsedDeps, IReadOnlyList<ReferenceWithCsproj> notInDeps,
                           SortedSet<string> noYamlInstall, SortedSet<string> configOverhead)
    {
        NotUsedDeps = notUsedDeps;
        NotInDeps = notInDeps;
        NoYamlInstallSection = noYamlInstall;
        ConfigOverhead = configOverhead;
    }

    public SortedSet<string> NotUsedDeps { get; }
    public IReadOnlyList<ReferenceWithCsproj> NotInDeps { get; }
    public SortedSet<string> NoYamlInstallSection { get; }
    public SortedSet<string> ConfigOverhead { get; }
}
