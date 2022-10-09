using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common;
using Common.Exceptions;
using Common.YamlParsers;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;

namespace Commands;

[PublicAPI]
public sealed class AnalyzerAddCommand : Command<AnalyzerAddCommandOptions>
{
    private static readonly CommandSettings Settings = new()
    {
        LogFileName = "analyzer-add",
        Location = CommandLocation.InsideModuleDirectory
    };

    private readonly ILogger logger;
    private readonly ConsoleWriter consoleWriter;
    private readonly DepsPatcherProject depsPatcherProject;
    private readonly IGitRepositoryFactory gitRepositoryFactory;

    public AnalyzerAddCommand(ILogger<AnalyzerAddCommand> logger, ConsoleWriter consoleWriter, FeatureFlags featureFlags,
                              DepsPatcherProject depsPatcherProject, IGitRepositoryFactory gitRepositoryFactory)
        : base(consoleWriter, Settings, featureFlags)
    {
        this.logger = logger;
        this.consoleWriter = consoleWriter;
        this.depsPatcherProject = depsPatcherProject;
        this.gitRepositoryFactory = gitRepositoryFactory;
    }

    public override string Name => "add";
    public override string HelpMessage => @"";

    protected override AnalyzerAddCommandOptions ParseArgs(string[] args)
    {
        var parsedArgs = ArgumentParser.ParseAnalyzerAdd(args);

        var analyzerModule = new Dep((string)parsedArgs["module"]);

        if (parsedArgs["configuration"] != null)
            analyzerModule.Configuration = (string)parsedArgs["configuration"];

        var moduleSolutionName = (string)parsedArgs["solution"];

        return new AnalyzerAddCommandOptions(moduleSolutionName, analyzerModule);
    }

    protected override int Execute(AnalyzerAddCommandOptions options)
    {
        var analyzerModule = options.AnalyzerModule;
        var moduleSolutionName = options.ModuleSolutionName;

        var analyzerConfiguration = analyzerModule.Configuration;

        logger.LogInformation(
            "Module: {AnalyzerModuleName}, configuration: '{AnalyzerModuleConfiguration}', " +
            "solution name: '{ModuleSolutionName}'", analyzerModule, analyzerConfiguration, moduleSolutionName);

        var moduleDirectory = Helper.GetModuleDirectory(Directory.GetCurrentDirectory());
        logger.LogDebug("ModuleDirectory: '{ModuleDirectory}'", moduleDirectory);

        if (!File.Exists(Path.Combine(moduleDirectory, Helper.YamlSpecFile)))
            throw new CementException("No " + Helper.YamlSpecFile + " file found");

        if (moduleSolutionName == null)
        {
            var possibleModuleSolutions = Yaml.GetSolutionList(moduleDirectory);
            logger.LogDebug("Found {PossibleModuleSolutionsCount} possible module solutions", possibleModuleSolutions.Count);

            if (possibleModuleSolutions.Count != 1)
                throw new BadArgumentException("Unable to resolve sln-file, please specify path to one");

            moduleSolutionName = possibleModuleSolutions[0];
            logger.LogDebug("Solution name: '{ModuleSolutionName}'", moduleSolutionName);
        }

        var moduleSolutionPath = Path.GetFullPath(moduleSolutionName);
        logger.LogDebug("Solution path: '{ModuleSolutionPath}'", moduleSolutionPath);

        if (!moduleSolutionPath.EndsWith(".sln"))
            throw new BadArgumentException(moduleSolutionPath + " is not sln-file");

        logger.LogDebug("Module solution file is sln-file");

        if (!File.Exists(moduleSolutionPath))
            throw new BadArgumentException(moduleSolutionPath + " is not exist");

        logger.LogDebug("Module solution file exists");

        var analyzerModuleName = Helper.TryFixModuleCase(analyzerModule.Name);
        logger.LogDebug("Fixed analyzer module name: '{AnalyzerModuleName}'", analyzerModuleName);

        analyzerModule = new Dep(analyzerModuleName, analyzerModule.Treeish, analyzerConfiguration);

        if (!Directory.Exists(Path.Combine(Helper.CurrentWorkspace, analyzerModuleName)) || !Helper.HasModule(analyzerModuleName))
            throw new CementException($"Can't find module '{analyzerModuleName}'");

        logger.LogDebug("{AnalyzerModuleName} -> {ModuleSolutionName}", analyzerModule, moduleSolutionName);

        CheckBranch(analyzerModule, moduleSolutionName);
        logger.LogDebug("Branch is OK");

        logger.LogInformation("Getting install data for {AnalyzerModule}", analyzerModule);
        var installData = InstallParser.Get(analyzerModuleName, analyzerConfiguration);

        var installFiles = installData.InstallFiles;
        if (installFiles == null || !installFiles.Any())
        {
            logger.LogWarning("No install files found in '{AnalyzerModule}'", analyzerModule);
            consoleWriter.WriteWarning($"No install files found in '{analyzerModuleName}'");

            return 0;
        }

        logger.LogInformation("{InstallFilesCount} install files found", installFiles.Count);

        var csprojFiles = GetCsprojFiles(moduleSolutionPath);
        logger.LogDebug("{CSProjectFilesCount} csproj files found", installFiles.Count);

        var csprojAndRulesetPairs = csprojFiles
            .Select(
                projectFile =>
                (
                    Csproj: projectFile,
                    Ruleset: new RulesetFile(Path.ChangeExtension(projectFile.FilePath, "ruleset"))
                ))
            .ToArray();

        foreach (var (csproj, ruleset) in csprojAndRulesetPairs)
        {
            logger.LogDebug("csproj='{CSProjectFilePath}', ruleset={RulesetFilePath}", csproj, ruleset);

            foreach (var installItem in installFiles)
            {
                logger.LogDebug("installFile='{InstallFile}'", installItem);

                if (!installItem.EndsWith(".ruleset"))
                    continue;

                var analyzerModuleRulesetPath = Path.GetFullPath(Path.Combine(Helper.CurrentWorkspace, installItem));
                ruleset.Include(analyzerModuleRulesetPath);
            }

            csproj.BindRuleset(ruleset);

            logger.LogInformation("Ruleset has bound successfully");

            foreach (var installItem in installFiles)
            {
                logger.LogDebug("installFile='{InstallFile}'", installItem);

                if (!installItem.EndsWith(".dll"))
                    continue;

                var analyzerModuleDllPath = Path.GetFullPath(Path.Combine(Helper.CurrentWorkspace, installItem));
                csproj.AddAnalyzer(analyzerModuleDllPath);
            }
        }

        logger.LogDebug("Patch deps for solution");
        depsPatcherProject.PatchDepsForSolution(moduleDirectory, analyzerModule, moduleSolutionPath);

        foreach (var (csproj, ruleset) in csprojAndRulesetPairs)
        {
            logger.LogDebug("csproj='{CSProjectFilePath}', ruleset={RulesetFilePath}", csproj, ruleset);

            csproj.Save();
            logger.LogDebug("csproj saved");

            ruleset.Save();
            logger.LogDebug("ruleset saved");
        }

        logger.LogInformation("Operation has completed successfully");
        consoleWriter.WriteOk($"Add '{analyzerModuleName}' to '{Path.GetFileName(moduleSolutionPath)}' successfully completed");
        return 0;
    }

    private void CheckBranch(Dep analyzerModule, string moduleSolutionName)
    {
        if (string.IsNullOrEmpty(analyzerModule.Treeish))
        {
            logger.LogDebug("Treeish is not defined");
            return;
        }

        logger.LogDebug("Treeish: '{AnalyzerModuleTreeish}'", analyzerModule.Treeish);

        try
        {
            var gitRepository = gitRepositoryFactory.Create(analyzerModule.Name, Helper.CurrentWorkspace);
            var current = gitRepository.CurrentLocalTreeish().Value;

            if (current != analyzerModule.Treeish)
                consoleWriter.WriteWarning($"{analyzerModule.Name} on @{current} but adding @{analyzerModule.Treeish}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "FAILED-TO-CHECK-BRANCH, module: {AnalyzerModule}", analyzerModule);
        }
    }

    private static IEnumerable<ProjectFile> GetCsprojFiles(string solutionPath)
    {
        // todo: static dependency
        var modules = Helper.GetModules();
        var parser = new VisualStudioProjectParser(solutionPath, modules);

        return parser
            .GetCsprojList()
            .Select(csprojPath => new ProjectFile(csprojPath))
            .ToList();
    }
}
