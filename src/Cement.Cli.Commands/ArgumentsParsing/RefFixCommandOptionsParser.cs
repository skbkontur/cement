using System.Linq;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class RefFixCommandOptionsParser : OptionsParser<RefFixCommandOptions>
{
    public override RefFixCommandOptions Parse(string[] args)
    {
        if (args.Length < 2 || args[0] != "ref" || args[1] != "fix")
            throw new BadArgumentException("Wrong usage of command.\nUsage: cm ref fix [-e]");

        var external = false;
        var parser = new OptionSet
        {
            {"e|external", _ => external = true}
        };
        var extraArgs = parser.Parse(args.Skip(2));
        ThrowIfHasExtraArgs(extraArgs);

        return new RefFixCommandOptions(external);
    }
}
