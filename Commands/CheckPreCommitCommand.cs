using System.IO;
using System.Linq;
using System.Text;
using Common;

namespace Commands
{
    public sealed class CheckPreCommitCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "pre-commit-check",
            MeasureElapsedTime = false,
            RequireModuleYaml = false,
            Location = CommandLocation.RootModuleDirectory,
            IsHiddenCommand = true
        };
        private readonly ConsoleWriter consoleWriter;

        public CheckPreCommitCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags)
            : base(consoleWriter, Settings, featureFlags)
        {
            this.consoleWriter = consoleWriter;
        }

        public override string Name => "check-pre-commit";
        public override string HelpMessage => @"
    Checks that commit is good

    Usage:
        cm check-pre-commit
";

        protected override int Execute()
        {
            var cwd = Directory.GetCurrentDirectory();
            var moduleName = Path.GetFileName(cwd);
            var repo = new GitRepository(moduleName, Helper.CurrentWorkspace, Log);

            var changedFiles = repo.GetFilesForCommit().Where(file => file.EndsWith(".cs") && File.Exists(file)).Distinct().ToList();
            var exitCode = 0;

            foreach (var file in changedFiles)
            {
                if (!CheckFile(file))
                {
                    exitCode = -1;
                    consoleWriter.WriteLine("Bad encoding in file: " + file);
                }
            }

            return exitCode;
        }

        protected override void ParseArgs(string[] args)
        {
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
    }
}
