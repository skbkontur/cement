using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class ShowConfigsCommandOptions
{
    public ShowConfigsCommandOptions(string moduleName)
    {
        ModuleName = moduleName;
    }

    public string ModuleName { get; }
}