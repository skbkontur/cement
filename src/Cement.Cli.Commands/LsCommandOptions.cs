using JetBrains.Annotations;

namespace Cement.Cli.Commands;

public enum ModuleProcessType
{
    Local,
    All,
}

[PublicAPI]
public sealed record LsCommandOptions(
    bool IsSimpleMode,
    ModuleProcessType ModuleProcessType,
    bool ShowUrl,
    bool ShowPushUrl,
    string BranchName);
