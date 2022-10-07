using Common;
using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class AnalyzerAddCommandOptions
{
    public AnalyzerAddCommandOptions(string moduleSolutionName, Dep analyzerModule)
    {
        ModuleSolutionName = moduleSolutionName;
        AnalyzerModule = analyzerModule;
    }

    public string ModuleSolutionName { get; }

    public Dep AnalyzerModule { get; }
}