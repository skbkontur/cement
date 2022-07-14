using System.Diagnostics;
using System.Linq;
using Common;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class UserCommand : Command
    {
        private string[] arguments;

        public UserCommand()
            : base(
                new CommandSettings
                {
                    LogPerfix = "USER-COMMAND",
                    LogFileName = null,
                    MeasureElapsedTime = false,
                    Location = CommandSettings.CommandLocation.Any
                })
        {
        }

        public override string HelpMessage => @"";

        protected override int Execute()
        {
            var cmd = CementSettingsRepository.Get().UserCommands[arguments[0]];
            Log.LogDebug("Run command " + arguments[0] + ": '" + cmd + "'");
            if (arguments.Length > 1)
            {
                arguments = arguments.Skip(1).ToArray();
                cmd = string.Format(cmd, arguments);
            }

            return Run(cmd);
        }

        protected override void ParseArgs(string[] args)
        {
            arguments = args;
        }

        private static int Run(string cmd)
        {
            ConsoleWriter.Shared.WriteInfo("Running command '" + cmd + "'");

            var startInfo = new ProcessStartInfo
            {
                FileName = Platform.IsUnix() ? "/bin/bash" : "cmd",
                Arguments = Platform.IsUnix() ? " -lc " : " /c ",
                UseShellExecute = false
            };
            startInfo.Arguments = startInfo.Arguments + "\"" + cmd + "\"";

            var process = Process.Start(startInfo);
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}
