using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public enum CommandLocation
{
    RootModuleDirectory,
    WorkspaceDirectory,
    Any,
    InsideModuleDirectory
}
