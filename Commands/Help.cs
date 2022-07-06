using System;
using Common;
using System.IO;
using System.Linq;

namespace Commands
{
    public sealed class Help : ICommand
    {
        private readonly ReadmeGenerator readmeGenerator;

        public Help()
        {
            readmeGenerator = new ReadmeGenerator();
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

            if (args.Length == 1)
            {
                CommandsList.Print();
                ConsoleWriter.Shared.WriteLine("");
                PrintHelpFooter();
                return 0;
            }

            if (args.Length > 2)
            {
                ConsoleWriter.Shared.WriteError("Wrong usage. Type 'cm help commandName'");
                return 0;
            }

            var command = args[1];
            if (CommandsList.Commands.ContainsKey(command))
            {
                var help = CommandsList.Commands[args[1]].HelpMessage;
                ConsoleWriter.Shared.WriteLine(help);
                PrintHelpFooter();
                return 0;
            }

            ConsoleWriter.Shared.WriteError("Bad command: '" + command + "'");
            return -1;
        }

        private static void PrintHelpFooter()
        {
            ConsoleWriter.Shared.WriteLine($"Cement. {DateTime.Now.Year}.");
        }

        private void GenerateReadme(string file)
        {
            var text = readmeGenerator.Generate();
            File.WriteAllText(file, text);
        }
    }
}
