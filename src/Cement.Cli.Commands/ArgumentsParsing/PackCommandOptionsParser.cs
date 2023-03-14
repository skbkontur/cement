using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class PackCommandOptionsParser : ArgumentParser<PackCommandOptions>
{
    public override PackCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var warnings = false;
        var obsolete = false;
        var verbose = false;
        var progress = false;
        var preRelease = false;

        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf},
            {"w|warnings", _ => warnings = true},
            {"W", _ => obsolete = true},
            {"v|verbose", _ => verbose = true},
            {"p|progress", _ => progress = true},
            {"prerelease", _ => preRelease = true}
        };

        args = parser.Parse(args).ToArray();

        if (args.Length != 2 || args[0] != "pack")
            throw new BadArgumentException("Wrong usage of command.\nUsage: cm pack [-c|--configuration <config-name>] <project-file>");

        var project = args[1];

        // todo(dstarasov): кажется, это не ответственность парсера, а скорее какого-то валидатора или самой команды
        if (!project.EndsWith(".csproj"))
            throw new BadArgumentException(project + " is not csproj file");

        var buildSettings = new BuildSettings
        {
            ShowAllWarnings = warnings,
            ShowObsoleteWarnings = obsolete,
            ShowOutput = verbose,
            ShowProgress = progress,
            ShowWarningsSummary = true
        };

        return new PackCommandOptions(project, configuration, buildSettings, preRelease);
    }
}
