using System.Linq;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class UsagesBuildCommandOptionsParser : OptionsParser<UsagesBuildCommandOptions>
{
    public override UsagesBuildCommandOptions Parse(string[] args)
    {
        string branch = null;
        var pause = false;

        var parser = new OptionSet
        {
            {"b|branch=", b => branch = b},
            {"p|pause", _ => pause = true}
        };
        var extraArgs = parser.Parse(args.Skip(2));
        ThrowIfHasExtraArgs(extraArgs);

        return new UsagesBuildCommandOptions(pause, branch);
    }
}
