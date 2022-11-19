using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class CompleteCommandOptions
{
    public CompleteCommandOptions(string[] otherArgs)
    {
        OtherArgs = otherArgs;
    }

    public string[] OtherArgs { get; }
}