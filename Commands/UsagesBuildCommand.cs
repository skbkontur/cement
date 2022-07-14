using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Logging;

namespace Commands
{
    public sealed class UsagesBuildCommand : Command
    {
        private readonly IUsagesProvider usagesProvider;

        private string moduleName, branch;
        private string checkingBranch;
        private string cwd;
        private string workspace;
        private GitRepository currentRepository;
        private bool pause;

        public UsagesBuildCommand()
            : base(
                new CommandSettings
                {
                    LogPerfix = "USAGES-BUILD",
                    LogFileName = "usages-build",
                    MeasureElapsedTime = true,
                    Location = CommandSettings.CommandLocation.RootModuleDirectory
                })
        {
            var logger = LogManager.GetLogger<UsagesProvider>();
            usagesProvider = new UsagesProvider(logger, CementSettingsRepository.Get);
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

        private static void WriteStat(List<KeyValuePair<Dep, string>> badParents, List<Dep> goodParents)
        {
            if (!badParents.Any())
                ConsoleWriter.Shared.WriteOk("All usages builds is fine");
            else
            {
                ConsoleWriter.Shared.WriteOk("Ok builds:");
                if (!goodParents.Any())
                    Console.WriteLine("none");
                foreach (var dep in goodParents)
                    ConsoleWriter.Shared.WriteOk(dep.ToString());
                Console.WriteLine();
                ConsoleWriter.Shared.WriteError("There were some errors in modules:");
                foreach (var pair in badParents)
                {
                    Console.WriteLine();
                    ConsoleWriter.Shared.WriteError(pair.Key.ToString());
                    Console.WriteLine(pair.Value);
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
                        ConsoleWriter.Shared.WriteError(exception.ToString());
                        badParents.Add(
                            new KeyValuePair<Dep, string>(
                                depParent,
                                exception.Message +
                                "\nLast command output:\n" +
                                ShellRunner.LastOutput));
                        if (pause)
                            WaitKey();
                    }

                    Console.WriteLine();
                    Console.WriteLine();
                }
            }

            WriteStat(badParents, goodParents);
        }

        private void WaitKey()
        {
            Console.WriteLine("Press any key to continue checking");
            Console.ReadKey();
        }

        private void BuildParent(Dep depParent)
        {
            ConsoleWriter.Shared.WriteInfo("Checking parent " + depParent);
            if (new GetCommand().Run(new[] {"get", depParent.Name, "-c", depParent.Configuration}) != 0)
                throw new CementException("Failed get module " + depParent.Name);
            ConsoleWriter.Shared.ResetProgress();
            if (new GetCommand().Run(new[] {"get", moduleName, branch}) != 0)
                throw new CementException("Failed get current module " + moduleName);
            ConsoleWriter.Shared.ResetProgress();

            using (new DirectoryJumper(Path.Combine(workspace, depParent.Name)))
            {
                if (new BuildDepsCommand().Run(new[] {"build-deps", "-c", depParent.Configuration}) != 0)
                    throw new CementException("Failed to build deps for " + depParent.Name);
                ConsoleWriter.Shared.ResetProgress();
                if (new BuildCommand().Run(new[] {"build"}) != 0)
                    throw new CementException("Failed to build " + depParent.Name);
                ConsoleWriter.Shared.ResetProgress();
            }

            ConsoleWriter.Shared.WriteOk($"{depParent} build fine");
            Console.WriteLine();
        }
    }
}
