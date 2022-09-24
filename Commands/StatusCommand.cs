using System;
using System.IO;
using Common;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class StatusCommand : ICommand
    {
        private static readonly ILogger Log = LogManager.GetLogger<StatusCommand>();
        private readonly ConsoleWriter consoleWriter;

        public StatusCommand(ConsoleWriter consoleWriter)
        {
            this.consoleWriter = consoleWriter;
        }

        public string HelpMessage => @"
    Prints status of modifed git repos in the cement tracked dir
    It checks repo for push/pull and local state

    Usage:
        cm status

    Runs anywhere in the cement tracked tree
";

        public string Name => "status";
        public bool IsHiddenCommand => false;

        public int Run(string[] args)
        {
            if (args.Length != 1)
            {
                consoleWriter.WriteError("Invalid command usage. User 'cm help init' for details");
                return -1;
            }

            var cwd = Directory.GetCurrentDirectory();
            cwd = Helper.GetWorkspaceDirectory(cwd);

            if (cwd == null)
            {
                consoleWriter.WriteError("Cement root was not found");
                return -1;
            }

            PrintStatus(cwd);
            return 0;
        }

        private void PrintStatus(string cwd)
        {
            var listDir = Directory.GetDirectories(cwd);
            var count = 0;
            foreach (var dir in listDir)
            {
                var repo = new GitRepository(dir, Log);
                PrintStatus(repo);
                consoleWriter.WriteProgress(++count + "/" + listDir.Length + " " + repo.ModuleName);
            }

            consoleWriter.ResetProgress();
        }

        private void PrintStatus(GitRepository repo)
        {
            try
            {
                if (!repo.HasLocalChanges() && repo.ShowUnpushedCommits().Length == 0)
                    return;

                consoleWriter.WriteInfo(repo.ModuleName);
                if (repo.HasLocalChanges())
                    consoleWriter.WriteLine(repo.ShowLocalChanges());
                if (repo.ShowUnpushedCommits().Length > 0)
                    consoleWriter.WriteLine(repo.ShowUnpushedCommits());
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}
