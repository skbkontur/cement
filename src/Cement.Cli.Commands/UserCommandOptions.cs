using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UserCommandOptions
{
    public UserCommandOptions(string[] arguments)
    {
        Arguments = arguments;
    }

    public string[] Arguments { get; }
}