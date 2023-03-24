using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.OptionsParsers;

public sealed class RefAddCommandOptionsParser : OptionsParser<RefAddCommandOptions>
{
    public override RefAddCommandOptions Parse(string[] args)
    {
        string configuration = null;
        var testReplaces = false;
        var force = false;

        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf},
            {"testReplaces", _ => testReplaces = true},
            {"force", _ => force = true}
        };

        args = parser.Parse(args).ToArray();
        if (args.Length != 4 || args[0] != "ref" || args[1] != "add")
        {
            throw new BadArgumentException(
                "Wrong usage of command.\n" +
                "Usage: cm ref add <module-name>[/configuration] <project-file>");
        }

        var module = args[2];
        var project = args[3];

        var dep = new Dep(module);
        if (configuration != null)
            dep.Configuration = configuration;

        // todo(dstarasov): кажется, это не ответственность парсера, а скорее какого-то валидатора или самой команды
        if (!project.EndsWith(".csproj"))
            throw new BadArgumentException(project + " is not csproj file");

        return new RefAddCommandOptions(project, dep, testReplaces, force);
    }
}
