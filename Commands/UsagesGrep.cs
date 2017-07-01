using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Common;

namespace Commands
{
    public class UsagesGrep : Command
    {
        private string moduleName;
        private string cwd;
        private string workspace;
        private GitRepository currentRepository;
        private readonly ShellRunner runner;
        private List<string> arguments;

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
            arguments = (List<string>)parsed["gitArgs"];
            checkingBranch = (string)parsed["branch"];
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
            var command = BuildGitCommand(arguments);

            using (new DirectoryJumper(workspace))
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
                foreach (var module in clonedModules)
                {
                    ConsoleWriter.WriteLine();
                    runner.RunInDirectory(module, command);
                    ConsoleWriter.WriteLine(AddModuleToOutput(ShellRunner.LastOutput, module));
                }
                ConsoleWriter.WriteLine();
            }
        }

        private string AddModuleToOutput(string output, string module)
        {
            var lines = output.Split(NewLine, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < lines.Length; ++i)
                lines[i] = module + "/" + lines[i];
            return string.Join(Environment.NewLine, lines);
        }

        private static string BuildGitCommand(IList<string> args)
        {
            var sb = new StringBuilder();
            sb.Append("git --no-pager grep -n ");
            for (var i = 2; i < args.Count; ++i)
            {
                if (args[i][0] != '-' && args[i - 1] != "-e")
                    sb.Append("-e ");
                sb.Append(Whitespaces.Replace(args[i], "\\s")).Append(' ');
            }
            return sb.ToString();
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