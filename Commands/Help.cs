using Common;
using System.IO;
using System.Linq;

namespace Commands
{
    public class Help : ICommand
    {
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
                ConsoleWriter.WriteLine("");
                PrintHelpFooter();
                return 0;
            }

            if (args.Length > 2)
            {
                ConsoleWriter.WriteError("Wrong usage. Type 'cm help commandName'");
                return 0;
            }

            var command = args[1];
            if (CommandsList.Commands.ContainsKey(command))
            {
                var help = CommandsList.Commands[args[1]].HelpMessage;
                ConsoleWriter.WriteLine(help);
                PrintHelpFooter();
                return 0;
            }

            ConsoleWriter.WriteError("Bad command: '" + command + "'");
            return -1;
        }

        private static void GenerateReadme(string file)
        {
            var text = ReadmeGenerator.Generate();
            File.WriteAllText(file, text);
        }

        private static void PrintHelpFooter()
        {
            ConsoleWriter.WriteLine("Cement. 2018.");
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
    }
}