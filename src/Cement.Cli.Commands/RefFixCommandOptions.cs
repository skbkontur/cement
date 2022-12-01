using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class RefFixCommandOptions
{
    public RefFixCommandOptions(bool fixExternal)
    {
        FixExternal = fixExternal;
    }

    public bool FixExternal { get; }
}