using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class ShowDepsCommandOptions
{
    public ShowDepsCommandOptions(string configuration)
    {
        Configuration = configuration;
    }

    public string Configuration { get; }
}