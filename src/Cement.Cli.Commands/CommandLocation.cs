using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public enum CommandLocation
{
    RootModuleDirectory,
    WorkspaceDirectory,
    Any,
    InsideModuleDirectory
}
