using System;
using System.IO;
using Common;
using log4net;

namespace Commands
{
	public class Status : ICommand
	{
		private static readonly ILog Log = LogManager.GetLogger("status");

		public int Run(string[] args)
		{
			if (args.Length != 1)
			{
				ConsoleWriter.WriteError("Invalid command usage. User 'cm help init' for details");
				return -1;
			}

			var cwd = Directory.GetCurrentDirectory();
			cwd = Helper.GetWorkspaceDirectory(cwd);

			if (cwd == null)
			{
				ConsoleWriter.WriteError("Cement root was not found");
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
				ConsoleWriter.WriteProgress(++count + "/" + listDir.Length + " " + repo.ModuleName);
			}
			ConsoleWriter.ResetProgress();
		}

		private void PrintStatus(GitRepository repo)
		{
			try
			{
				if (!repo.HasLocalChanges() && repo.ShowUnpushedCommits().Length == 0)
					return;

				ConsoleWriter.WriteInfo(repo.ModuleName);
				if (repo.HasLocalChanges())
					ConsoleWriter.WriteLine(repo.ShowLocalChanges());
				if (repo.ShowUnpushedCommits().Length > 0)
					ConsoleWriter.WriteLine(repo.ShowUnpushedCommits());
			}
			catch (Exception)
			{
				// ignored
			}
		}
		
		public string HelpMessage => @"
    Prints status of modifed git repos in the cement tracked dir. 
    It checks repo for push/pull and local state.

    Usage:
        cm status

    Runs anywhere in the cement tracked tree.
";
        public bool IsHiddenCommand => false;
    }
}