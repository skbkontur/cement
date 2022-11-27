using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed record LsCommandOptions(
    bool IsSimpleMode,
    bool IsForLocalModules,
    bool IsForAllModules,
    bool ShowUrl,
    bool ShowPushUrl,
    string BranchName);
