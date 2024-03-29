using System;
using System.Collections.Generic;
using System.Linq;
using Cement.Cli.Commands.OptionsParsers;
using Cement.Cli.Common;
using Cement.Cli.Common.Logging;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UsagesShowCommand : Command<UsagesShowCommandOptions>
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IUsagesProvider usagesProvider;

    public UsagesShowCommand(ConsoleWriter consoleWriter)
    {
        this.consoleWriter = consoleWriter;
        usagesProvider = new UsagesProvider(LogManager.GetLogger<UsagesProvider>(), CementSettingsRepository.Get);
    }

    public override string Name => "show";
    public override string HelpMessage => @"";

    protected override UsagesShowCommandOptions ParseArgs(string[] args)
    {
        return new UsagesShowCommandOptionsParser().Parse(args);
    }

    protected override int Execute(UsagesShowCommandOptions options)
    {
        var module = options.Module;

        if (Helper.GetModules().All(m => !string.Equals(m.Name, module, StringComparison.OrdinalIgnoreCase)))
            consoleWriter.WriteWarning("Module " + module + " not found");

        var response = usagesProvider.GetUsages(module, options.Branch, options.Configuration);

        if (options.PrintEdges)
        {
            consoleWriter.WriteLine(";Copy paste this text to http://arborjs.org/halfviz/#");
            foreach (var item in response.Items)
                PrintArborjsInfo(options.ShowAll, item);
        }
        else
        {
            foreach (var item in response.Items)
                PrintInfo(options.ShowAll, item);
            PrintFooter(response.UpdatedTime);
        }

        return 0;
    }

    private void PrintArborjsInfo(bool showAll, KeyValuePair<Dep, List<Dep>> item)
    {
        consoleWriter.WriteLine("{color:black}");
        consoleWriter.WriteLine(item.Key + " {color:red}");
        var answer = item.Value;
        if (!showAll)
            answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

        foreach (var parent in answer)
            consoleWriter.WriteLine(parent + " -> " + item.Key);
    }

    private void PrintInfo(bool showAll, KeyValuePair<Dep, List<Dep>> item)
    {
        var answer = item.Value;
        if (!showAll)
            answer = answer.Select(d => new Dep(d.Name, null, d.Configuration)).Distinct().ToList();

        consoleWriter.WriteLine("{0} usages:", item.Key);

        var modules = answer.GroupBy(dep => dep.Name, dep => dep).OrderBy(kvp => kvp.Key);
        foreach (var kvp in modules)
        {
            var configs = kvp.GroupBy(dep => dep.Configuration).OrderBy(kvp2 => kvp2.Key);
            consoleWriter.WriteLine("  " + kvp.Key);
            foreach (var config in configs)
            {
                consoleWriter.WriteLine("    " + config.Key);
                if (config.Any() && showAll)
                    consoleWriter.WriteLine(string.Join("\n", config.Select(c => "      " + c.Treeish).OrderBy(x => x)));
            }
        }

        consoleWriter.WriteLine();
    }

    private void PrintFooter(DateTime updTime)
    {
        consoleWriter.WriteInfo("Data from cache relevant to the " + updTime);
    }
}
