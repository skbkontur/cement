using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;
using Common.Extensions;

namespace Commands
{
    public class UsagesGrep : Command
    {
        private string moduleName;
        private string cwd;
        private string workspace;
        private GitRepository currentRepository;
        private readonly ShellRunner runner;
        private string[] arguments;
        private string[] fileMasks;
        private bool skipGet;

        private static readonly string[] GrepParametersWithValue =
            {"-A", "-B", "-C", "--threads", "-f", "-e", "--parent-basename", "--max-depth"};
        private static readonly string[] NewLine = {"\r\n", "\r", "\n"};
        private static readonly Regex Whitespaces = new Regex("\\s");
        private string checkingBranch;

        public UsagesGrep()
            : base(new CommandSettings
            {
                LogPerfix = "USAGES-GREP",
                LogFileName = "usages-grep.net.log",
                MeasureElapsedTime = true,
                Location = CommandSettings.CommandLocation.RootModuleDirectory
            })
        {
            runner = new ShellRunner(Log);
        }

        protected override void ParseArgs(string[] args)
        {
            var parsed = ArgumentParser.ParseGrepParents(args);
            arguments = (string[]) parsed["gitArgs"];
            fileMasks = (string[]) parsed["fileMaskArgs"];

            checkingBranch = (string) parsed["branch"];
            skipGet = (bool) parsed["skip-get"];
        }

        protected override int Execute()
        {
            cwd = Directory.GetCurrentDirectory();
            workspace = Directory.GetParent(cwd).FullName;
            moduleName = Path.GetFileName(cwd);
            currentRepository = new GitRepository(moduleName, workspace, Log);

            if (checkingBranch == null)
                checkingBranch = currentRepository.HasLocalBranch("master") ?
                    "master" : currentRepository.CurrentLocalTreeish().Value;

            var response = Usages.GetUsagesResponse(moduleName, checkingBranch);

            var usages = response.Items
                .SelectMany(kvp => kvp.Value)
                .Where(d => d.Treeish == "master")
                .DistinctBy(d => d.Name);

            Grep(usages);
            return 0;
        }

        private void Grep(IEnumerable<Dep> toGrep)
        {
            var modules = Helper.GetModules();
            var command = BuildGitCommand(arguments, fileMasks);
            ConsoleWriter.WriteInfo(command);
            ConsoleWriter.WriteLine();

            using (new DirectoryJumper(workspace))
            {
                var clonedModules = skipGet
                    ? GetExistingDirectories(toGrep)
                    : CloneModules(toGrep, modules);

                foreach (var module in clonedModules)
                {
                    runner.RunInDirectory(module, command);
                    if (string.IsNullOrWhiteSpace(ShellRunner.LastOutput))
                        continue;
                    ConsoleWriter.WriteLine(AddModuleToOutput(ShellRunner.LastOutput, module));
                    ConsoleWriter.WriteLine();
                }
            }
        }

        private List<string> GetExistingDirectories(IEnumerable<Dep> toGrep)
        {
            return toGrep.Select(d => d.Name).Where(Directory.Exists).ToList();
        }

        private List<string> CloneModules(IEnumerable<Dep> toGrep, List<Module> modules)
        {
            var clonedModules = new List<string>();
            foreach (var depParent in toGrep)
            {
                try
                {
                    GetWithoutDependencies(depParent, modules);
                    clonedModules.Add(depParent.Name);
                }
                catch (CementException exception)
                {
                    ConsoleWriter.WriteError(exception.ToString());
                }
            }

            return clonedModules;
        }

        private string AddModuleToOutput(string output, string module)
        {
            var lines = output.Split(NewLine, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; ++i)
                lines[i] = module + "/" + lines[i];
            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildGitCommand(string[] args, string[] masks)
        {
            var sb = new StringBuilder();

            sb.Append("git --no-pager grep -n ");
            for (var i = 2; i < args.Length; ++i)
            {
                if (args[i - 1] == "-f")
                    sb.Append('"').Append(Path.GetFullPath(args[i])).Append('"');
                else
                {
                    if (IsPatternWithoutFlag(args, i))
                        sb.Append("-e ");
                    sb.Append(Escape(Whitespaces.Replace(args[i], "\\s")));
                }
                sb.Append(' ');
            }

            if (masks.Length > 0)
                sb.Append("-- ");
            foreach (var mask in masks)
                sb.Append('"').Append(mask).Append('"');

            return sb.ToString();
        }

        private static bool IsPatternWithoutFlag(string[] args, int position)
        {
            return !(args[position][0] == '-' || GrepParametersWithValue.Contains(args[position - 1]));
        }

        private static string Escape(string s)
        {
            return s.Replace("\"", "\\\"");
        }

        private void GetWithoutDependencies(Dep dep, List<Module> modules)
        {
            var getter = new ModuleGetter(
                modules,
                dep,
                LocalChangesPolicy.FailOnLocalChanges,
                null);

            getter.GetModule();
        }

        public override string HelpMessage => @"";
    }
}