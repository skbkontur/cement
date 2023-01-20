using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Cement.Cli.Commands.Attributes;
using Cement.Cli.Commands.Common;
using Cement.Cli.Common;
using JetBrains.Annotations;
using Microsoft.Extensions.DependencyInjection;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class HelpCommand : ICommand
{
    private readonly ConsoleWriter consoleWriter;
    private readonly IServiceProvider serviceProvider;
    private readonly ReadmeGenerator readmeGenerator;

    public HelpCommand(ConsoleWriter consoleWriter, IServiceProvider serviceProvider, ReadmeGenerator readmeGenerator)
    {
        this.consoleWriter = consoleWriter;
        this.serviceProvider = serviceProvider;
        this.readmeGenerator = readmeGenerator;
    }

    public bool MeasureElapsedTime { get; }

    public bool RequireModuleYaml { get; }

    public CommandLocation Location { get; } = CommandLocation.Any;

    public string Name => "help";

    public string HelpMessage => @"
    Prints help for command

    Usage:
        cm help <command-name>
        cm <command-name> /?
        cm <command-name> --help

    Example:
        cm help init
";

    public int Run(string[] args)
    {
        if (args.Contains("--gen"))
        {
            GenerateReadme(args[2]);
            return 0;
        }

        var commands = serviceProvider.GetServices<ICommand>()
            .ToDictionary(c => c.Name);

        if (args.Length == 1)
        {
            Print(commands);
            consoleWriter.WriteLine("");
            PrintHelpFooter();
            return 0;
        }

        if (args.Length > 2)
        {
            consoleWriter.WriteError("Wrong usage. Type 'cm help commandName'");
            return 0;
        }

        var command = args[1];
        if (commands.ContainsKey(command))
        {
            var help = commands[args[1]].HelpMessage;
            consoleWriter.WriteLine(help);
            PrintHelpFooter();
            return 0;
        }

        consoleWriter.WriteError("Bad command: '" + command + "'");
        return -1;
    }

    public void Print(IDictionary<string, ICommand> commands)
    {
        var commandNames = commands.Keys.OrderBy(x => x);
        foreach (var commandName in commandNames)
        {
            var command = commands[commandName];
            var commandType = command.GetType();

            if (commandType.GetCustomAttribute<HiddenCommandAttribute>() != null)
                continue;

            var smallHelp = GetSmallHelp(command);
            consoleWriter.WriteLine($"{commandName,-25}{smallHelp}");
        }
    }

    private static string GetSmallHelp(ICommand command)
    {
        var help = command.HelpMessage;
        var lines = help.Split('\n');
        return lines.Length < 2 ? "???" : lines[1].Trim();
    }

    private void PrintHelpFooter()
    {
        consoleWriter.WriteLine($"Cement. {DateTime.Now.Year}.");
    }

    private void GenerateReadme(string file)
    {
        var text = readmeGenerator.Generate();
        File.WriteAllText(file, text);
    }
}
