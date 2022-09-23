using System;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public sealed class HelpCommand : ICommand
    {
        private readonly ConsoleWriter consoleWriter;
        private readonly FeatureFlags featureFlags;
        private readonly ReadmeGenerator readmeGenerator;

        public HelpCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, ReadmeGenerator readmeGenerator)
        {
            this.consoleWriter = consoleWriter;
            this.featureFlags = featureFlags;
            this.readmeGenerator = readmeGenerator;
        }

        public string HelpMessage => @"
    Prints help for command

    Usage:
        cm help <command-name>
        cm <command-name> /?
        cm <command-name> --help

    Example:
        cm help init
";

        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            if (args.Contains("--gen"))
            {
                GenerateReadme(args[2]);
                return 0;
            }

            var commands = new CommandsList(consoleWriter, featureFlags);
            if (args.Length == 1)
            {
                commands.Print();
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
}
