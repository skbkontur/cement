using JetBrains.Annotations;

namespace Commands;

[PublicAPI]
public sealed class UserCommandOptions
{
    public UserCommandOptions(string[] arguments)
    {
        Arguments = arguments;
    }

    public string[] Arguments { get; }
}