﻿using System;
using System.Linq;
using Commands;
using Common;

namespace cm
{
    static class Program
    {
        private static int Main(string[] args)
        {
            args = FixArgs(args);
            var exitCode = TryRun(args);

            ConsoleWriter.ResetProgress();

            var command = args[0];
            if (command != "complete" && command != "check-pre-commit")
                SelfUpdate.UpdateIfOld();

            return exitCode == 0 ? 0 : 13;
        }

        private static string[] FixArgs(string[] args)
        {
            if (args.Length > 0 && args[0].Equals("cm"))
                args = args.Skip(1).ToArray();
            if (args.Length == 0)
                args = new[] {"help"};
            if (args.Contains("--help") || args.Contains("/?"))
                args = new[] {"help", args[0]};
            return args;
        }

        private static int TryRun(string[] args)
        {
            try
            {
                return Run(args);
            }
            catch (CementException e)
            {
                ConsoleWriter.WriteError(e.Message);
                return -1;
            }
            catch (Exception e)
            {
                ConsoleWriter.WriteError(e.Message);
                ConsoleWriter.WriteError(e.StackTrace);
                return -1;
            }
        }

        private static int Run(string[] args)
        {
            var commands = CommandsList.Commands;
            if (commands.ContainsKey(args[0]))
            {
                return commands[args[0]].Run(args);
            }

            if (CementSettings.Get().UserCommands.ContainsKey(args[0]))
                return new UserCommand().Run(args);

            ConsoleWriter.WriteError("Bad command: '" + args[0] + "'");
            return -1;
        }
    }
}
