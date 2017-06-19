using System;
using System.IO;
using System.Linq;
using System.Text;
using Common;

namespace Commands
{
	public class CheckPreCommit : Command
	{
		public CheckPreCommit() 
			: base(new CommandSettings
			{
				LogPerfix = "CHECK-PRE-COMMIT",
				LogFileName = null,
				MeasureElapsedTime = false,
				RequireModuleYaml = false,
				Location = CommandSettings.CommandLocation.RootModuleDirectory
			}){ }

		protected override int Execute()
		{
			var cwd = Directory.GetCurrentDirectory();
			var moduleName = Path.GetFileName(cwd);
			var repo = new GitRepository(moduleName, Helper.CurrentWorkspace, Log);

			var changedFiles = repo.GetFilesForCommit().Where(file => file.EndsWith(".cs") && File.Exists(file)).Distinct().ToList();
			int exitCode = 0;

			foreach (var file in changedFiles)
			{
				if (!CheckFile(file))
				{
					exitCode = -1;
					Console.WriteLine("Bad encoding in file: " + file);
				}
			}

			return exitCode;
		}

		private static bool CheckFile(string file)
		{
			var bytes = File.ReadAllBytes(file);
			var hasBom = FileHasUtf8Bom(bytes);

			if (hasBom)
				return true;

			return !FileHasNonAsciiSymbols(bytes);
		}

		private static bool FileHasNonAsciiSymbols(byte[] fileBytes)
		{
			return fileBytes.Any(b => b > 127);
		}

		private static bool FileHasUtf8Bom(byte[] fileBytes)
		{
			var preamble = new UTF8Encoding(true).GetPreamble();
			if (fileBytes.Length < preamble.Length)
				return false;
			for (var i = 0; i < preamble.Length; i++)
				if (fileBytes[i] != preamble[i])
					return false;
			return true;
		}

		protected override void ParseArgs(string[] args)
		{
			
		}

		public override string HelpMessage => @"
    Checks that commit is good

    Usage:
        cm check-pre-commit
";
	}
}