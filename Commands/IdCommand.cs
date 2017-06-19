using System;
using System.IO;
using Common;
using log4net;

namespace Commands
{
	public class IdCommand : ICommand
	{
		private static readonly ILog Log = LogManager.GetLogger("id");

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

			ConsoleWriter.WriteError("Failed to get info in %s\nNot a module or module's parent folder");
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
						ConsoleWriter.WriteLine(moduleName + " " + hash);
					}
				}
				catch (Exception)
				{
					// ignored
				}
			}
		}

		public string HelpMessage => @"
    Prints id of current module or ids of modules

    Usage:
        cm id
";
	    public bool IsHiddenCommand => true;
	}
}