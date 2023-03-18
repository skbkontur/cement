using System.Linq;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class ShowDepsCommandOptionsParser : OptionsParser<ShowDepsCommandOptions>
{
    public override ShowDepsCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf}
        };
        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        return new ShowDepsCommandOptions(configuration);
    }
}
