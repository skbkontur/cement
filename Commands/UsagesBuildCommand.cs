using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;

namespace Commands
{
    public sealed class UsagesBuildCommand : Command
    {
        private static readonly CommandSettings Settings = new()
        {
            LogFileName = "usages-build",
            MeasureElapsedTime = true,
            Location = CommandSettings.CommandLocation.RootModuleDirectory
        };
        private readonly ConsoleWriter consoleWriter;

        private readonly IUsagesProvider usagesProvider;
        private readonly GetCommand getCommand;

        private string moduleName, branch;
        private string checkingBranch;
        private string cwd;
        private string workspace;
        private GitRepository currentRepository;
        private bool pause;

        public UsagesBuildCommand(ConsoleWriter consoleWriter, IUsagesProvider usagesProvider, GetCommand getCommand)
            : base(Settings)
        {
            this.consoleWriter = consoleWriter;
            this.usagesProvider = usagesProvider;
            this.getCommand = getCommand;
        }

        public override string HelpMessage => @"";

        protected override void ParseArgs(string[] args)
        {
            var parsedArgs = ArgumentParser.ParseBuildParents(args);
            checkingBranch = (string)parsedArgs["branch"];
            pause = (bool)parsedArgs["pause"];
        }

        protected override int Execute()
        {
            cwd = Directory.GetCurrentDirectory();
            workspace = Directory.GetParent(cwd).FullName;
            moduleName = Path.GetFileName(cwd);
            currentRepository = new GitRepository(moduleName, workspace, Log);

            if (currentRepository.HasLocalChanges())
                throw new CementException("You have uncommited changes");
            branch = currentRepository.CurrentLocalTreeish().Value;
            if (checkingBranch == null)
                checkingBranch = branch;

            var response = usagesProvider.GetUsages(moduleName, checkingBranch);

            var toBuild = response.Items.SelectMany(kvp => kvp.Value).Where(d => d.Treeish == "master").ToList();
            BuildDeps(toBuild);
            return 0;
        }

        private void WriteStat(List<KeyValuePair<Dep, string>> badParents, List<Dep> goodParents)
        {
            if (!badParents.Any())
                consoleWriter.WriteOk("All usages builds is fine");
            else
            {
                consoleWriter.WriteOk("Ok builds:");
                if (!goodParents.Any())
                    consoleWriter.WriteLine("none");
                foreach (var dep in goodParents)
                    consoleWriter.WriteOk(dep.ToString());
                consoleWriter.WriteLine();
                consoleWriter.WriteError("There were some errors in modules:");
                foreach (var pair in badParents)
                {
                    consoleWriter.WriteLine();
                    consoleWriter.WriteError(pair.Key.ToString());
                    consoleWriter.WriteLine(pair.Value);
                }
            }
        }

        private void BuildDeps(List<Dep> toBuilt)
        {
            var badParents = new List<KeyValuePair<Dep, string>>();
            var goodParents = new List<Dep>();

            using (new DirectoryJumper(workspace))
            {
                foreach (var depParent in toBuilt)
                {
                    try
                    {
                        BuildParent(depParent);
                        goodParents.Add(depParent);
                    }
                    catch (CementException exception)
                    {
                        consoleWriter.WriteError(exception.ToString());
                        badParents.Add(
                            new KeyValuePair<Dep, string>(
                                depParent,
                                exception.Message +
                                "\nLast command output:\n" +
                                ShellRunner.LastOutput));
                        if (pause)
                            WaitKey();
                    }

                    consoleWriter.WriteLine();
                    consoleWriter.WriteLine();
                }
            }

            WriteStat(badParents, goodParents);
        }

        private void WaitKey()
        {
            consoleWriter.WriteLine("Press any key to continue checking");
            Console.ReadKey();
        }

        private void BuildParent(Dep depParent)
        {
            consoleWriter.WriteInfo("Checking parent " + depParent);
            if (getCommand.Run(new[] {"get", depParent.Name, "-c", depParent.Configuration}) != 0)
                throw new CementException("Failed get module " + depParent.Name);
            consoleWriter.ResetProgress();
            if (getCommand.Run(new[] {"get", moduleName, branch}) != 0)
                throw new CementException("Failed get current module " + moduleName);
            consoleWriter.ResetProgress();

            using (new DirectoryJumper(Path.Combine(workspace, depParent.Name)))
            {
                if (new BuildDepsCommand(consoleWriter).Run(new[] {"build-deps", "-c", depParent.Configuration}) != 0)
                    throw new CementException("Failed to build deps for " + depParent.Name);
                consoleWriter.ResetProgress();
                if (new BuildCommand(consoleWriter).Run(new[] {"build"}) != 0)
                    throw new CementException("Failed to build " + depParent.Name);
                consoleWriter.ResetProgress();
            }

            consoleWriter.WriteOk($"{depParent} build fine");
            consoleWriter.WriteLine();
        }
    }
}
