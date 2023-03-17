using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class AnalyzerAddCommandOptionsParser : OptionsParser<AnalyzerAddCommandOptions>
{
    public override AnalyzerAddCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf}
        };

        args = parser.Parse(args).ToArray();

        if (args.Length is < 3 or > 4 || args[0] != "analyzer" || args[1] != "add")
        {
            throw new BadArgumentException(
                $"Command format error: cm {string.Join(" ", args)}\n" +
                $"Command format: cm analyzer add <analyzer-module-name>[/configuration] [<solution-file>]");
        }

        var module = args[2];
        var solution = args.Length == 4 ? args[3] : null;

        var analyzerModule = new Dep(module);

        if (configuration != null)
            analyzerModule.Configuration = configuration;

        return new AnalyzerAddCommandOptions(solution, analyzerModule);
    }
}
