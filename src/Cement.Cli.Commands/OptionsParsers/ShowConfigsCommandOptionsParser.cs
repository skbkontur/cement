using System.Linq;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class ShowConfigsCommandOptionsParser : OptionsParser<ShowConfigsCommandOptions>
{
    public override ShowConfigsCommandOptions Parse(string[] args)
    {
        string module = null;

        var extraArgs = args.Skip(1).ToList();
        if (extraArgs.Count > 0)
        {
            module = extraArgs[0];
            ThrowIfHasExtraArgs(extraArgs.Skip(1).ToList());
        }

        return new ShowConfigsCommandOptions(module);
    }
}
