using System.Linq;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class SelfUpdateCommandOptionsParser : OptionsParser<SelfUpdateCommandOptions>
{
    public override SelfUpdateCommandOptions Parse(string[] args)
    {
        string branch = null;
        string server = null;

        var parser = new OptionSet
        {
            {"b|branch=", b => branch = b},
            {"s|server=", s => server = s}
        };
        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        return new SelfUpdateCommandOptions(branch, server);
    }
}
