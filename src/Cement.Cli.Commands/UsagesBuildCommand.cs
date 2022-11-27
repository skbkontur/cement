using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Cement.Cli.Common;
using Cement.Cli.Common.ArgumentsParsing;
using Cement.Cli.Common.Exceptions;
using JetBrains.Annotations;

namespace Cement.Cli.Commands;

[PublicAPI]
public sealed class UsagesBuildCommand : Command<UsagesBuildCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        MeasureElapsedTime = true,
        Location = CommandLocation.RootModuleDirectory
    };
    private readonly ConsoleWriter consoleWriter;
    private readonly IGitRepositoryFactory gitRepositoryFactory;
    private readonly IUsagesProvider usagesProvider;
    private readonly GetCommand getCommand;
    private readonly BuildDepsCommand buildDepsCommand;
    private readonly BuildCommand buildCommand;

    private string moduleName;
    private string branch;
    private string cwd;
    private string workspace;
    private GitRepository currentRepository;

    public UsagesBuildCommand(ConsoleWriter consoleWriter, FeatureFlags featureFlags, IUsagesProvider usagesProvider,
                              GetCommand getCommand, BuildDepsCommand buildDepsCommand, BuildCommand buildCommand,
                              IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.consoleWriter = consoleWriter;
        this.usagesProvider = usagesProvider;
        this.getCommand = getCommand;
        this.buildDepsCommand = buildDepsCommand;
        this.buildCommand = buildCommand;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public override string Name => "build";
    public override string HelpMessage => @"";

    protected override UsagesBuildCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseBuildParents(args);
        var checkingBranch = (string)parsedArgs["branch"];
        var pause = (bool)parsedArgs["pause"];

        return new UsagesBuildCommandOptions(pause, checkingBranch);
    }

    protected override int Execute(UsagesBuildCommandOptions options)
    {
        cwd = Directory.GetCurrentDirectory();
        workspace = Directory.GetParent(cwd).FullName;
        moduleName = Path.GetFileName(cwd);
        currentRepository = gitRepositoryFactory.Create(moduleName, workspace);

        if (currentRepository.HasLocalChanges())
            throw new CementException("You have uncommited changes");
        branch = currentRepository.CurrentLocalTreeish().Value;

        var checkingBranch = options.CheckingBranch;
        if (checkingBranch == null)
            checkingBranch = branch;

        var response = usagesProvider.GetUsages(moduleName, checkingBranch);

        var toBuild = response.Items.SelectMany(kvp => kvp.Value).Where(d => d.Treeish == "master").ToList();
        BuildDeps(options.Pause, toBuild);
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

    private void BuildDeps(bool pause, List<Dep> toBuilt)
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

                    // todo(dstarasov): очень странный код, который неявно зависит от последнего вызова ShellRunner
                    var reason = exception.Message + "\nLast command output:\n" + ShellRunner.LastOutput;

                    var badParent = new KeyValuePair<Dep, string>(depParent, reason);
                    badParents.Add(badParent);

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
            if (buildDepsCommand.Run(new[] {"build-deps", "-c", depParent.Configuration}) != 0)
                throw new CementException("Failed to build deps for " + depParent.Name);
            consoleWriter.ResetProgress();
            if (buildCommand.Run(new[] {"build"}) != 0)
                throw new CementException("Failed to build " + depParent.Name);
            consoleWriter.ResetProgress();
        }

        consoleWriter.WriteOk($"{depParent} build fine");
        consoleWriter.WriteLine();
    }
}
