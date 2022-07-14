using System;
using System.IO;
using Common;
using Common.Logging;
using Microsoft.Extensions.Logging;

namespace Commands
{
    public sealed class IdCommand : ICommand
    {
        private static readonly ILogger Log = LogManager.GetLogger<IdCommand>();

        public string HelpMessage => @"
    Prints id of current module or ids of modules

    Usage:
        cm id
";

        public bool IsHiddenCommand => true;

        public int Run(string[] args)
        {
            var cwd = Directory.GetCurrentDirectory();
            if (Helper.IsCementTrackedDirectory(cwd))
            {
                PrintHashes(Directory.GetDirectories(cwd));
                return 0;
            }

            if (Helper.IsCurrentDirectoryModule(cwd))
            {
                PrintHashes(new[] {cwd});
                return 0;
            }

            ConsoleWriter.Shared.WriteError("Failed to get info in %s\nNot a module or module's parent folder");
            return -1;
        }

        private void PrintHashes(string[] modules)
        {
            foreach (var module in modules)
            {
                try
                {
                    var moduleName = Path.GetFileName(module);
                    var workspace = Directory.GetParent(module).FullName;
                    var repo = new GitRepository(moduleName, workspace, Log);
                    if (repo.IsGitRepo)
                    {
                        var hash = repo.CurrentLocalCommitHash();
                        ConsoleWriter.Shared.WriteLine(moduleName + " " + hash);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }
}
