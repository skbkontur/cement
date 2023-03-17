using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class BuildCommandOptionsParser : OptionsParser<BuildCommandOptions>
{
    public override BuildCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var warnings = false;
        var obsolete = false;
        var verbose = false;
        var progress = false;
        var cleanBeforeBuild = false;

        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf},
            {"w|warnings", _ => warnings = true},
            {"v|verbose", _ => verbose = true},
            {"p|progress", _ => progress = true},
            {"cleanBeforeBuild", _ => cleanBeforeBuild = true},
            {"W", _ => obsolete = true},
        };
        var extraArgs = parser.Parse(args.Skip(1));
        ThrowIfHasExtraArgs(extraArgs);

        if (verbose && (warnings || progress))
        {
            throw new BadArgumentException();
        }

        var buildSettings = new BuildSettings
        {
            ShowAllWarnings = warnings,
            ShowObsoleteWarnings = obsolete,
            ShowOutput = verbose,
            ShowProgress = progress,
            ShowWarningsSummary = true,
            CleanBeforeBuild = cleanBeforeBuild
        };

        return new BuildCommandOptions(configuration, buildSettings);
    }
}
