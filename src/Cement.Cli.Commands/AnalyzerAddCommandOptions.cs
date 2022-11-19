using Cement.Cli.Common;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

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
