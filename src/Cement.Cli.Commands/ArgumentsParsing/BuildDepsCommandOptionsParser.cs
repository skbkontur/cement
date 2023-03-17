using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class BuildDepsCommandOptionsParser : ArgumentParser<BuildDepsCommandOptions>
{
    public override BuildDepsCommandOptions Parse(string[] args)
    {
        var rebuild = false;
        string configuration = null;
        var warnings = false;
        var verbose = false;
        var progress = false;
        var quickly = false;
        var cleanBeforeBuild = false;

        var parser = new OptionSet
        {
            {"r|rebuild", _ => rebuild = true},
            {"c|configuration=", conf => configuration = conf},
            {"w|warnings", _ => warnings = true},
            {"v|verbose", _ => verbose = true},
            {"p|progress", _ => progress = true},
            {"q|quickly", _ => quickly = true},
            {"cleanBeforeBuild", _ => cleanBeforeBuild = true}
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
            ShowOutput = verbose,
            ShowProgress = progress,
            CleanBeforeBuild = cleanBeforeBuild
        };
        return new BuildDepsCommandOptions(configuration, rebuild, quickly, buildSettings);
    }
}
