using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class CompleteCommandOptions
{
    public CompleteCommandOptions(string[] otherArgs)
    {
        OtherArgs = otherArgs;
    }

    public string[] OtherArgs { get; }
}