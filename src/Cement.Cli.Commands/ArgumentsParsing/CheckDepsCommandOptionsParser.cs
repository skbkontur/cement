using System.Linq;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class CheckDepsCommandOptionsParser : OptionsParser<CheckDepsCommandOptions>
{
    public override CheckDepsCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var showAll = false;
        var showShort = false;
        var external = false;

        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf},
            {"a|all", _ => showAll = true},
            {"s|short", _ => showShort = true},
            {"e|external", _ => external = true}
        };
        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        return new CheckDepsCommandOptions(configuration, showAll, external, showShort);
    }
}
