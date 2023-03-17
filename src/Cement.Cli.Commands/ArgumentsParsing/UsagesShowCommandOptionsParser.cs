using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.Exceptions;
using NDesk.Options;

namespace Cement.Cli.Commands.ArgumentsParsing;

public sealed class UsagesShowCommandOptionsParser : OptionsParser<UsagesShowCommandOptions>
{
    public override UsagesShowCommandOptions Parse(string[] args)
    {
        //todo(dstarasov): этот код выглядит скорее как ответственность уже самой команды, а не парсера
        var currentDir = Directory.GetCurrentDirectory();
        while (currentDir != Directory.GetDirectoryRoot(currentDir) && !Helper.IsCurrentDirectoryModule(currentDir))
            currentDir = Directory.GetParent(currentDir).FullName;

        var configuration = "*";
        var branch = "*";
        var all = false;
        var edges = false;

        string module = null;
        if (Helper.IsCurrentDirectoryModule(currentDir))
            module = Path.GetFileName(currentDir);

        var parser = new OptionSet
        {
            {"c|configuration=", conf => configuration = conf},
            {"m|module=", m => module = m},
            {"b|branch=", b => branch = b},
            {"a|all", _ => all = true},
            {"e|edges", _ => edges = true}
        };
        var extraArgs = parser.Parse(args.Skip(2));
        if (module == null)
        {
            throw new BadArgumentException("Current directory is not cement module directory, use -m to specify module name");
        }

        if (module.Contains('/'))
        {
            module = module.Split('/').First();
            configuration = module.Split('/').Last();
        }

        ThrowIfHasExtraArgs(extraArgs);
        return new UsagesShowCommandOptions(module, branch, configuration, all, edges);
    }
}
